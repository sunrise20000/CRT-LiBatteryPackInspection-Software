using Aitex.Common.Util;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.Cognex
{
    public class CognexWaferIDReader:OCRReaderBaseDevice, IConnection
    {
        private CognexOCRConnection _connection;
        private static readonly object _lockerHandlerList = new object();
        private string _scRoot;
        private PeriodicJob _thread;
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigWarningMessage = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();
        private bool _enableLog;
        private string _imageStorePath;
        private DateTime _dtActionStart;

        public override string CurrentImageFileName
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.TempImageFilePath"))
                {
                    return SC.GetStringValue($"OCRReader.{Name}.TempImageFilePath");
                }
                return "";
            }
        }
        public bool IsLogined { get; set; }
        public CognexWaferIDReader(string module,string name,string scRoot):base(module,name)
        {
            _scRoot = scRoot;
            InitializeCognex();
            CheckToPostMessage((int)OCRReaderMsg.StartInit, null);
        }
        public void InitializeCognex()
        {
            Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            TimeLimitForRead = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitForWID");
            _lstHandler.Clear();
            _connection = new CognexOCRConnection(Address,this);
            if (CheckAddressPort(Address))
            {
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");                    
                }
            }
            else EV.PostWarningLog(Module, $"{Module}.{Name} Adress is illegal");
            ConnectionManager.Instance.Subscribe(Name, this);
            _thread = new PeriodicJob(50, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            var fileInfo = new FileInfo(PathManager.GetDirectory($"Logs\\{Name}"));

            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                fileInfo.Directory.Create();

            if (fileInfo.Directory != null)
                _imageStorePath = fileInfo.FullName;
            ConnectionManager.Instance.Subscribe($"{Name}", this);
        }


        static bool CheckAddressPort(string s)
        {
            bool isLegal;
            Regex regex = new Regex(@"^((2[0-4]\d|25[0-5]|[1]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[1]?\d\d?)\:([1-9]|[1-9][0-9]|[1-9][0-9][0-9]|[1-9][0-9][0-9][0-9]|[1-6][0-5][0-5][0-3][0-5])$");//CheckAddressPort
            Match match = regex.Match(s);
            if (match.Success)
            {
                isLegal = true;
            }
            else
            {
                isLegal = false;
            }
            return isLegal;
        }

        private bool OnTimer()
        {
            try
            {
                
                _connection.MonitorTimeout();

                if (_connection.IsCommunicationError)
                {
                    _lstHandler.Clear();
                    return true;
                }
                if (_connection.IsBusy)
                    return true;

                HandlerBase handler = null;

                lock (_lockerHandlerList)
                {
                    if (_lstHandler.Count > 0)
                    {
                        handler = _lstHandler.First.Value;
                        if (handler != null) _connection.Execute(handler);
                        _lstHandler.RemoveFirst();
                    }                    
                }
                _trigActionDone.CLK = (_lstHandler.Count == 0 && !_connection.IsBusy);
                if (_trigActionDone.Q)
                    OnActionDone(null);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public string Address { get; private set; }

        public bool IsConnected => _connection.IsConnected ;

        public bool Connect()
        {
            _connection.Connect();
            return true;
        }

        public bool Disconnect()
        {
            _connection.Disconnect();
            return true;
        }
        protected override bool fStartInit(object[] param)
        {
            
            return true;
        }       
        protected override bool fStartReadWaferID(object[] param)
        {
            string job = param[0].ToString();
            int lasermarkindex = (int)param[1];
            IsReadLaserMark1 = lasermarkindex == 0;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddLast(new OnlineHandler(this,false));
                _lstHandler.AddLast(new LoadJobHandler(this, job));
                _lstHandler.AddLast(new OnlineHandler(this, true));
                _lstHandler.AddLast(new ReadWaferIDHandler(this));
            }
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorReading(object[] param)
        {
            IsBusy = false;
            if(DateTime.Now - _dtActionStart >TimeSpan.FromSeconds(TimeLimitForRead))
            {
                EV.Notify(AlarmWIDReadTimeout);
                ReadOK = false;
                if(IsReadLaserMark1)
                {
                    LaserMark1 = "******";
                    LaserMark1Score = "0";
                    LaserMark1ReadTime = (DateTime.Now - _dtActionStart).TotalSeconds.ToString();
                }
                Reset();
                return true;
            }
            return false;

        }
        protected override bool fStartReadParameter(object[] param)
        {
            readparameter = param[0].ToString();
            switch (readparameter)
            {
                case "JobList":
                    if (SC.ContainsItem($"{ _scRoot}.{ Name}.DirectoryForJob") && !string.IsNullOrEmpty(SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob")))
                    {
                        string jobpath = SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob");
                        string[] files = Directory.GetFiles(jobpath);
                        JobFileList = new List<string>();
                        JobFileList.Clear();
                        foreach(string strfile in files)
                        {
                            FileInfo f = new FileInfo(strfile);
                            if(f.Extension == ".job")
                            {
                                JobFileList.Add(f.Name);
                            }
                        }
                    }
                    else
                    {
                        lock (_lockerHandlerList)
                        {
                            _lstHandler.AddLast(new GetJobListHandler(this));
                        }
                    }
                    break;
                default:
                    break;
            }
            return true; ;
        }
        private string readparameter = "";

        protected override bool fMonitorReadParameter(object[] param)
        {
            if (readparameter == "JobList" && SC.ContainsItem($"{ _scRoot}.{ Name}.DirectoryForJob") && !string.IsNullOrEmpty(SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob")))
            {
                return true;
            }
            return base.fMonitorReadParameter(param);
        }

        protected override bool fStartReset(object[] param)
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;
            _lstHandler.Clear();
            _connection.ForceClear();
            if (_connection.IsCommunicationError || !_connection.IsConnected)
            {
                Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");                
                _connection = new CognexOCRConnection(Address, this);
                _connection.Connect();
            }
            _connection.SetCommunicationError(false, "");
            //_trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            return true;
        }

        protected override bool fStartSetParameter(object[] param)
        {
            string paraname = param[0].ToString();
            switch(paraname)
            {
                case "SetOnline":
                    
                    break;
            }
            return true; ;
        }
        protected override bool fSetParameterComplete(object[] param)
        {
            return true;
        }

        public override string[] GetJobFileList()
        {
            lock(_lockerHandlerList)
            {
                _lstHandler.AddLast(new GetJobListHandler(this));
            }
            int delaytime = 0;
            while((_lstHandler.Count !=0 || _connection.IsBusy)&& delaytime<10)
            {
                Thread.Sleep(500);
                delaytime++;
            }
            return JobFileList.ToArray();
        }
        protected override bool fStartSavePicture(object[] param)
        {
            lock(_lockerHandlerList)
            {

            }
            return true;
        }
        public void SaveImage(string filename,string imgString)
        {
            DeleteEarlyImageFile();
            var byBitmap = HexStringToBytes(imgString);
            var bmp = new Bitmap(new MemoryStream(byBitmap));
            
            bmp.Save($"{_imageStorePath}{filename}.bmp", ImageFormat.Bmp);
            EV.PostInfoLog("WIDReader", $"Picture saved, file name is {filename}.bmp"); 
        }
        private void DeleteEarlyImageFile()
        {
            string[] fpath = Directory.GetFiles(_imageStorePath, "*.bmp", SearchOption.TopDirectoryOnly);
            Dictionary<string, DateTime> fCreateDate = new Dictionary<string, DateTime>();
            for (int i = 0; i < fpath.Length; i++)
            {
                FileInfo fi = new FileInfo(fpath[i]);
                fCreateDate[fpath[i]] = fi.CreationTime;
            }
            fCreateDate = fCreateDate.OrderBy(f => f.Value).ToDictionary(f => f.Key, f => f.Value);
            int fCount = fCreateDate.Count;
            while (fCount > 99)
            {
                string strFile = fCreateDate.First().Key;
                File.Delete(strFile);
                fCreateDate.Remove(strFile);
                fCount = fCreateDate.Count;
            }


        }
        private static byte[] HexStringToBytes(string hex)
        {
            byte[] data = new byte[hex.Length / 2];
            int j = 0;
            for (int i = 0; i < hex.Length; i += 2)
            {
                data[j] = Convert.ToByte(hex.Substring(i, 2), 16);
                ++j;
            }
            return data;
        }

        protected override void OnWaferIDRead(string wid, string score, string readtime)
        {
            if(DeviceState == OCRReaderStateEnum.ReadWaferID)
                base.OnWaferIDRead(wid, score, readtime);
        }

    }

}
