using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MachineVision.Keyence
{
    public class KeyenceCVX300F : BaseDevice, IConnection, IDevice
    {
        private KeyenceCVX300FConnection _connection;

        private bool _isOn;

        private bool _activeMonitorStatus;

        private int _errorCode;
        private string _lastError = string.Empty;

        //private RD_TRIG _trigPumpOnOff = new RD_TRIG();
        //private R_TRIG _trigError = new R_TRIG();
        //private R_TRIG _trigOverTemp = new R_TRIG();
        //private R_TRIG _trigWarningMessage = new R_TRIG();        
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();


        uint iCurrentOper = 0xff;
        List<byte> liReceivedData = new List<byte>();
        byte bEnd = 0x0d;
        //bool bStaGood = true;
        //bool bCanBeat = false;
        //int iBeatTimes = 0;
        private int _iArea = 0;
        public string _sResult;
        //
        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;
        private string _strCmd;


        #region 属性
        public string Address
        {
            get; set;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected;
            }
        }
        //属性
        public bool AlarmStatus { get; set; }
        public bool PositionComplete { get; set; }
        public bool MotorBusy { get; set; }
        //
        public bool IsBusy { get; set; }
        //public bool IsStbOff { get; set; } //StbOff消息是否发送

        public int AreaData
        {
            get { return _iArea; }
        }
        #endregion


        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public KeyenceCVX300F(string module, string scRoot, string name) : base(module, name, name, name)
        {

            _scRoot = scRoot;
            _activeMonitorStatus = true;
            //

        }


        public bool Initialize()
        {
            // string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            string sAddress = SC.GetStringValue($"{Name}.Address");
            Address = sAddress;
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new KeyenceCVX300FConnection(sAddress);
            _connection.EnableLog(_enableLog);

            //
            _sResult = "";
            //

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            //设置属性
            this.SetAttr();

            //设置操作
            this.SetOP();
            //
            _thread = new PeriodicJob(200, OnTimer, $"{Name} MonitorHandler", true);



            //
            //this.InitMachine();
            //
            ConnectionManager.Instance.Subscribe($"{Name}", this);
            //SetServoOn();
            return true;
        }
        /// <summary>
        /// 设置属性
        /// </summary>
        private void SetAttr()
        {
            //DATA.Subscribe($"{Module}.{Name}.Area", () => _iArea);
            DATA.Subscribe($"{Module}.{Name}.Result", () => this._sResult);
        }
        /// <summary>
        /// 设置操作
        /// </summary>
        private void SetOP()
        {
            //读取面积
            //OP.Subscribe($"{Module}.{Name}.CVXCheckArea", (cmd, param) =>
            //{                
            //    //Set("T1","T1");
            //    //EV.PostInfoLog(Module, $"{Name} Get area data.");
            //    return true;
            //});
            //读取结果
            OP.Subscribe($"{Module}.{Name}.GetResult", (cmd, param) =>
            {
                GetResult(Convert.ToInt32(param[0]));
                //
                return true;
            });
            //转至运行模式
            OP.Subscribe($"{Module}.{Name}.RunR0", (cmd, param) =>
            {
                RunR0();
                return true;
            });
            //清除结果
            OP.Subscribe($"{Module}.{Name}.ClearResult", (cmd, param) =>
            {
                this.ClearResult();
                return true;
            });

        }
        public void ClearResult()
        {
            this._sResult = "";
        }
        /// <summary>
        /// 转到运行模式
        /// </summary>
        public void RunR0()
        {
            Set("R0", "R0");
            EV.PostInfoLog(Module, $"{Name} Run R0 command.");
        }
        /// <summary>
        /// 获取拍照比对结果
        /// </summary>
        /// <param name="iConId">执行条件编号</param>
        public void GetResult(int iConId)
        {
            SetExecCond(iConId);
            //this._strCmd = param[0] as string;
            //if (this._strCmd.Length > 0)
            //{
            //    Set(this._strCmd, this._strCmd);
            //    EV.PostInfoLog(Module, $"{Name} Get result.");
            //}

            //
            //Set("T1", "T1");
            EV.PostInfoLog(Module, $"{Name} Get result.");
        }
        /// <summary>
        /// 设置输出文件名
        /// </summary>
        /// <param name="sFileName"></param>
        public void SetFileName(string sFileName)
        {

            string sCmd = "STW,1,\"" + sFileName.Trim();//1为相机内文件名编号，相机输出时调用编号
            Set("STW", sCmd);
            EV.PostInfoLog(Module, $"{Name} Set file name:" + sFileName);
        }
        /// <summary>
        /// 写入执行条件:EXW,n
        /// </summary>
        /// <param name="iConId">条件编号</param>
        public void SetExecCond(int iConId)
        {
            string sCmd = "EXW," + iConId.ToString();//执行条件编号，现在是点1处使用条件1，点2处使用条件2 
            Set("EXW", sCmd);
            EV.PostInfoLog(Module, $"{Name} Set condition no.");
        }

        /// <summary>
        /// 加入命令队列
        /// </summary>
        /// <param name="strCmd">命令 Ascii格式</param>
        public void Set(string name, string strCmd)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if (_connection.IsBusy)
                {
                    _connection.ForceClear();
                }
                List<byte> liCmd = Encoding.ASCII.GetBytes(strCmd).ToList<byte>();
                liCmd.Add(0x0d); //CR
                //liCmd.Add(0x0a); //LF 不可颠倒          

                byte[] bCmd = liCmd.ToArray();

                //_lstHandler.AddLast(new HwAlignerGuideQueryHandler(this, strCmd, bCmd));
                _lstHandler.AddLast(new KeyenceCVX300FSetHandler(this, name, bCmd));



            }
        }


        public int _connecteTimes { get; set; }
        private bool OnTimer()
        {
            try
            {
                //
                _connection.MonitorTimeout();

                //若断，重连

                if ((!_connection.IsConnected) || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        //_trigSevroOn.RST = true;
                        //IsBusy = false;
                        _lstHandler.Clear();
                    }
                    //                   


                    _trigRetryConnect.CLK = _connection.IsConnected; //不可重复置值，否则Q会有变化
                    if (_connection.Connect())
                    {
                        //
                        //if (_trigRetryConnect.Q)
                        //{

                        EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                        LOG.Write($"{Module}.{Name} connected");
                        //}

                        //转至运行模式，此命令可以重入
                        this.RunR0();
                        this.SetFileName("sic");
                    }
                    else
                    {
                        if (_trigRetryConnect.Q)
                        {
                            EV.PostInfoLog(Module, $"Trying to connect {Module}.{Name} with address: {_connection.Address}");
                            LOG.Write($"Trying to connect {Module}.{Name} with address: {_connection.Address}");
                        }
                    }

                }


                //处理命令队列
                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0 && _activeMonitorStatus && !IsBusy)
                        {
                            //Query();
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
                //
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        internal void NoteError()
        {

        }

        public void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);
                //if (_connecteTimes < 4) return;
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Terminate()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Disconnect();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Reset()
        {
            //
            _connecteTimes = 0;
            _connection.SetCommunicationError(false, "");//这里重置了CommunicationError ，就是去了OnTimer里面的重连的触发，所以需要另设变量以触发之(使用timeout来触发的情况）
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _trigCommunicationError.RST = true;
            _trigRetryConnect.RST = true;

            //


        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }


        //internal void NoteOnOff(bool isOn)
        //{
        //    _isOn = isOn;
        //}

        public bool _PraseData(string name, byte[] buffer)
        {
            bool bRes = false;
            //
            try
            {
                this.bEnd = 0x0d;
                this.liReceivedData.AddRange(buffer);

                List<byte> liData = new List<byte>();
                int iIndex = this.liReceivedData.LastIndexOf(bEnd);
                //int iIndex = this.liReceivedData.IndexOf(bEnd);
                if (iIndex > 0)
                {
                    liData.AddRange(this.liReceivedData.GetRange(0, iIndex + 1));
                    this.liReceivedData.RemoveRange(0, iIndex + 1);
                }
                else
                {
                    return false;
                }

                string sAnswer = Encoding.ASCII.GetString(liData.ToArray());



                //string sAnswer = Encoding.ASCII.GetString(buffer);
                //返回一次 "T1\r" ,再次返回 "T1\r00004422,0\r"
                string[] sCmds = sAnswer.Split('\n', '\r');

                foreach (string sOneCmd in sCmds)
                {
                    if (sOneCmd.Length > 0 && sOneCmd.Trim().CompareTo(name) != 0)
                    {
                        //bRes = this.DoOneCmd(name, sOneCmd);
                    }
                }

            }
            catch (Exception ex)
            {
                string sEx = $"{Module}.{Name} Exception:" + ex.Message;
                EV.PostAlarmLog(Module, sEx);
                LOG.Write(sEx);
            }
            //
            return bRes;
        }

        public bool PraseData(string name, byte[] buffer)
        {
            //
            string sCmdBack = Encoding.ASCII.GetString(buffer);

            bool bRes = false;

            //

            if (sCmdBack.Trim().Length > 0)
            {

                //
                if (name.CompareTo("T1") == 0)
                {
                    if (sCmdBack.CompareTo("T1\r") == 0)
                    {
                        bRes = false;
                    }
                    else
                    {
                        bool bResult = sCmdBack.Replace("T1\r", "").Contains("0");
                        //
                        if (bResult)
                        {
                            this._sResult = "OK";
                        }
                        else
                        {
                            this._sResult = "NG";
                        }
                        bRes = true;
                    }

                }
                else if (name.CompareTo("EXW") == 0)
                {
                    Set("T1", "T1");
                    bRes = true;
                }
                else
                {
                    if (sCmdBack.Contains("ER,"))
                    {
                        EV.PostWarningLog(Module, "Keyence CCD camera get an error answer.");
                    }
                    bRes = true;
                }


            }
            //
            return bRes;
        }




        //public void SetErrorCode(int errorCode)
        //{
        //    _errorCode = errorCode;
        //}


        //public void SetError(string reason)
        //{
        //    _trigWarningMessage.CLK = true;
        //    if (_trigWarningMessage.Q)
        //    {
        //        EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
        //    }
        //}
        private static byte[] ModRTU_CRC(byte[] buffer)
        {
            ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;

            for (var pos = 0; pos < len; pos++)
            {
                crc ^= buf[pos]; // XOR byte into least sig. byte of crc

                for (var i = 8; i != 0; i--)
                    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {
                        // If the LSB is set
                        crc >>= 1; // Shift right and XOR 0xA001
                        crc ^= 0xA001;
                    }
                    else // Else LSB is not set
                    {
                        crc >>= 1; // Just shift right
                    }
            }

            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return BitConverter.GetBytes(crc);

        }
    }
}
