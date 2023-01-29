using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using DePacker;
using Global;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Event;
//using RTOverEthernetDevelopmentKit;
using SessionLayer;
using System;
using System.Runtime.InteropServices;

namespace Mainframe.Devices
{
    public struct BEI_SYSTEM_STATUS_ZH
    {
        public int nStatus;
        public double dReflectWaferAverageInner; //内圈衬底反射率平均值
        public double dReflectWaferAverageMiddle; //中圈衬底反射率平均值
        public double dReflectWaferAverageOutter; //外圈衬底反射率平均值
        public double dTemperatureAverageWaferInner; //内圈衬底温度平均值
        public double dTemperatureAverageWaferMiddle; //中圈衬底温度平均值
        public double dTemperatureAverageWaferOutter; //外圈衬底温度平均值
        public double dTemperatureAverageSusceptorInner; //内圈托盘温度平均值
        public double dTemperatureAverageSusceptorMiddle; //中圈托盘温度平均值
        public double dTemperatureAverageSusceptorOutter; //外圈托盘温度平均值
    };

    public class OpticsViperStatus : NotifiableItem
    {
        public int Status { get; set; }
        public int Head { get; set; }
        public int DetectorMask { get; set; }
        public int Reserved { get; set; }
        public double SusAvg_1 { get; set; }
        public double SusAvg_2 { get; set; }
        public double SusAvg_3 { get; set; }
        public double SusAvg_4 { get; set; }
        public double WaferAvg_1 { get; set; }
        public double WaferAvg_2 { get; set; }
        public double WaferAvg_3 { get; set; }
        public double WaferAvg_4 { get; set; }

        public OpticsViperStatus()
        {
            Status = 0;
            Head = 0;
            DetectorMask = 0;
            Reserved = 0;
            SusAvg_1 = 0.0;
            SusAvg_2 = 0.0;
            SusAvg_3 = 0.0;
            SusAvg_4 = 0.0;
            WaferAvg_1 = 0.0;
            WaferAvg_2 = 0.0;
            WaferAvg_3 = 0.0;
            WaferAvg_4 = 0.0;
        }
    }

    public class OpticsViperInfo
    {
        private int _lastStatus = 0;

        public int lastStatus => _lastStatus;

        private OpticsViperStatus _opticsViperStatus = new OpticsViperStatus();

        public OpticsViperStatus opticsViperStatus => _opticsViperStatus;

        private AITOpticsViperData[] _innerOpticsViperItem = new AITOpticsViperData[0];

        public AITOpticsViperData[] innerOpticsViperItem => _innerOpticsViperItem;


        private AITOpticsViperData[] _middleOpticsViperItem = new AITOpticsViperData[0];

        public AITOpticsViperData[] middleOpticsViperItem => _middleOpticsViperItem;

        private AITOpticsViperData[] _outerOpticsViperItem = new AITOpticsViperData[0];

        public AITOpticsViperData[] outerOpticsViperItem => _outerOpticsViperItem;

        private AITOpticsViperData[] _exOpticsViperItem = new AITOpticsViperData[0];

        public AITOpticsViperData[] exOpticsViperItem => _exOpticsViperItem;

        public OpticsViperInfo()
        {
            _innerOpticsViperItem = new AITOpticsViperData[0];
            _middleOpticsViperItem = new AITOpticsViperData[0];
            _outerOpticsViperItem = new AITOpticsViperData[0];
            _exOpticsViperItem = new AITOpticsViperData[0];
        }

        public OpticsViperInfo(OpticsViperStatus opticsViperStatus, AITOpticsViperData[] innerOpticsViperItem,
            AITOpticsViperData[] middleOpticsViperItem, AITOpticsViperData[] outerOpticsViperItem)
        {
            _opticsViperStatus = opticsViperStatus;
            _innerOpticsViperItem = innerOpticsViperItem;
            _middleOpticsViperItem = middleOpticsViperItem;
            _outerOpticsViperItem = outerOpticsViperItem;
        }

        public OpticsViperInfo(OpticsViperStatus opticsViperStatus, AITOpticsViperData[] innerOpticsViperItem,
            AITOpticsViperData[] middleOpticsViperItem, AITOpticsViperData[] outerOpticsViperItem,
            AITOpticsViperData[] exOpticsViperItem)
        {
            _opticsViperStatus = opticsViperStatus;
            _innerOpticsViperItem = innerOpticsViperItem;
            _middleOpticsViperItem = middleOpticsViperItem;
            _outerOpticsViperItem = outerOpticsViperItem;
            _exOpticsViperItem = exOpticsViperItem;
            _lastStatus = opticsViperStatus.Status;

            //_opticsViperStatus = opticsViperStatus;
            //_innerOpticsViperItem = new AITOpticsViperData[innerOpticsViperItem.Length / 2];
            //_middleOpticsViperItem = new AITOpticsViperData[middleOpticsViperItem.Length / 2];
            //_outerOpticsViperItem = new AITOpticsViperData[outerOpticsViperItem.Length / 2];
            //_exOpticsViperItem = new AITOpticsViperData[exOpticsViperItem.Length / 2];
            //_lastStatus = opticsViperStatus.Status;

            //bool tempIsWafer = SC.GetValue<bool>("PM.AnngkunWaferTemp");
            //for (int i = 0; i < _innerOpticsViperItem.Length; i++)
            //{
            //    if (tempIsWafer)
            //    {
            //        _innerOpticsViperItem[i] = innerOpticsViperItem[i * 2];
            //    }
            //    else if(i * 2 + 1 < innerOpticsViperItem.Length)
            //    {
            //        _innerOpticsViperItem[i] = innerOpticsViperItem[i * 2 + 1];
            //    }
            //}

            //for (int i = 0; i < _middleOpticsViperItem.Length; i++)
            //{
            //    if (tempIsWafer)
            //    {
            //        _middleOpticsViperItem[i] = middleOpticsViperItem[i * 2];
            //    }
            //    else if (i * 2 + 1 < middleOpticsViperItem.Length)
            //    {
            //        _middleOpticsViperItem[i] = middleOpticsViperItem[i * 2 + 1];
            //    }
            //}

            //for (int i = 0; i < _outerOpticsViperItem.Length; i++)
            //{
            //    if (tempIsWafer)
            //    {
            //        _outerOpticsViperItem[i] = outerOpticsViperItem[i * 2];
            //    }
            //    else if (i * 2 + 1 < outerOpticsViperItem.Length)
            //    {
            //        _outerOpticsViperItem[i] = outerOpticsViperItem[i * 2 + 1];
            //    }
            //}

            //for (int i = 0; i < exOpticsViperItem.Length; i++)
            //{
            //    if (tempIsWafer)
            //    {
            //        _exOpticsViperItem[i] = exOpticsViperItem[i * 2];
            //    }
            //    else
            //    {
            //        _exOpticsViperItem[i] = exOpticsViperItem[i * 2 + 1];
            //    }
            //}
        }

        public void SetlastStatus(int lastStatus)
        {
            _lastStatus = lastStatus;
        }
    }

    public class AkOpticsViperGuide : BaseDevice, IDevice
    {


        public AkOpticsViperGuide(string module, string name)
        {
            this.Name = name;
            this.Module = module;

            TcpSession = new Session();


            TcpSession.BeforeConnect += TcpSession_BeforeConnect;
            TcpSession.AfterConnect += TcpSession_AfterConnect;
            TcpSession.DisConnected += TcpSession_DisConnected;
            TcpSession.MsgRecv += TcpSession_MsgRecv;

            DP = new DePacker.DPack();
            DP.GetPackCallback += GetPack;
            DP.BeginDepack();
            DATA.Subscribe($"PM1.{Name}.InnerOpticsViperItem", () => _opticsViperInfo?.innerOpticsViperItem);
            DATA.Subscribe($"PM1.{Name}.MiddleOpticsViperItem", () => _opticsViperInfo?.middleOpticsViperItem);
            DATA.Subscribe($"PM1.{Name}.OuterOpticsViperItem", () => _opticsViperInfo?.outerOpticsViperItem);
            DATA.Subscribe($"PM1.{Name}.ExOpticsViperItem", () => _opticsViperInfo?.exOpticsViperItem);
            DATA.Subscribe($"PM1.{Name}.OpticsViperStatus", () => _opticsViperInfo?.opticsViperStatus);


        }



        struct BinaryData
        {
            public BEI_SYSTEM_STATUS_EX status;

            public TempData[] zoneInner;

            public TempData[] zoneMiddle;

            public TempData[] zoneOuter;

            public TempData[] zoneEx;

            public BinaryData(bool IsCreate)
            {
                status = new BEI_SYSTEM_STATUS_EX();
                zoneInner = new TempData[18];
                zoneMiddle = new TempData[36];
                zoneOuter = new TempData[36];
                zoneEx = new TempData[72];
            }
        }

        BinaryData Data = new BinaryData(true);

        private bool IsNewStatus = false;
        private PeriodicJob _thread; //工作线程


        Session TcpSession;
        DPack DP;

        private OpticsViperInfo _opticsViperInfo = new OpticsViperInfo();

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

        /// <summary>
        /// 接受到数据时触发事件
        /// </summary>
        public event Action<OpticsViperInfo> DataReceivedEvent;

        /// <summary>
        /// 通讯数据
        /// </summary>
        public OpticsViperInfo OpticsViperInfo
        {
            get => _opticsViperInfo;
            set
            {
                _opticsViperInfo = value;
                DataReceivedEvent?.Invoke(_opticsViperInfo);
            }
        }


        /*public string Module { get; set; }
        public string Name { get; set; }
        public bool HasAlarm => false;
        */
        public int ReconnectionNumber;

        public bool JobThreadStop;

        public AkOpticsViperGuide()
        {


        }

        BEI_SYSTEM_STATUS_ZH SystemStatus = new BEI_SYSTEM_STATUS_ZH();

        const int StatusSize = 7856;
        const int VoltBinarySize = 48024;

        private void GetPack(byte[] bin)
        {
            switch (bin.Length)
            {
                case StatusSize:
                    UpdateStatus(bin);
                    break;
                //case VoltBinarySize:
                //    {
                //        UpdateVoltData(bin);
                //        if (IsSaveVolt)
                //        {
                //            SaveVoltData();
                //            IsSaveVolt = false;
                //            btnSaveVolt.Enabled = true;
                //        }
                //    }

                //    break;
                //default:
                //    txtMsgRecv.Text += System.Text.Encoding.Default.GetString(bin);
                // break;
            }
        }

        private void TcpSession_MsgRecv(int id, byte[] buf)
        {
            DP.AddStream(buf);
            //throw new NotImplementedException();
        }

        private void TcpSession_DisConnected(int id, bool IsSuccess)
        {
            //throw new NotImplementedException();
        }

        private void TcpSession_AfterConnect(int id, bool IsSuccess)
        {
            string fmtLeft = "<RT_Plus>";
            string fmtRight = "<End>;";
            string request_command = fmtLeft + "RuntimeData" + fmtRight;

            byte[] buf = System.Text.Encoding.ASCII.GetBytes(request_command);

            TcpSession.Send(buf, buf.Length);
        }

        private void TcpSession_BeforeConnect()
        {

        }


        public AITOpticsViperData ConvertTempData(int WaferNo, TempData tempData)
        {
            AITOpticsViperData opticsViperItem = new AITOpticsViperData();

            opticsViperItem.WaferNo = WaferNo;
            opticsViperItem.DateTime = tempData.DateTime;
            opticsViperItem.Para1 = tempData.Para1;
            opticsViperItem.Para2 = tempData.Para2;
            opticsViperItem.Para3 = tempData.Para3;
            opticsViperItem.Para4 = tempData.Para4;
            opticsViperItem.Temperature = tempData.Temperature;

            return opticsViperItem;
        }

        public AITOpticsViperData[] ConvertTempData(TempData[] tempData)
        {
            AITOpticsViperData[] opticsViperItem = new AITOpticsViperData[tempData.Length];
            for (int i = 0; i < tempData.Length; i++)
            {

                opticsViperItem[i] = ConvertTempData(i + 1, tempData[i]);
            }

            return opticsViperItem;
        }

        public OpticsViperStatus ConvertStatusData(BEI_SYSTEM_STATUS_EX statusData)
        {
            OpticsViperStatus opticsViperStatus = new OpticsViperStatus();

            opticsViperStatus.Status = statusData.nStatus;
            opticsViperStatus.Head = statusData.Head;
            opticsViperStatus.DetectorMask = statusData.DetectorMask;
            opticsViperStatus.Reserved = statusData.Reserved;
            opticsViperStatus.SusAvg_1 = statusData.SusAvg_1;
            opticsViperStatus.SusAvg_2 = statusData.SusAvg_2;
            opticsViperStatus.SusAvg_3 = statusData.SusAvg_3;
            opticsViperStatus.SusAvg_4 = statusData.SusAvg_4;
            opticsViperStatus.WaferAvg_1 = statusData.WaferAvg_1;
            opticsViperStatus.WaferAvg_2 = statusData.WaferAvg_2;
            opticsViperStatus.WaferAvg_3 = statusData.WaferAvg_3;
            opticsViperStatus.WaferAvg_4 = statusData.WaferAvg_4;

            return opticsViperStatus;
        }

        public void Connect()
        {
            try
            {
                string ip = SC.GetStringValue($"{Name}.Address");
                ;
                int port = SC.GetValue<int>($"{Name}.Port");

                if (!TcpSession.Query(ip, port))
                {
                    //MessageBox.Show("Target IP  port  is error!");
                    return;
                }

                if (_thread == null)
                {
                    _thread = new PeriodicJob(1000, OnTimer, "AkOpticsTimer", true); //测试用，直接连接
                }


                if (!TcpSession.Link())
                {
                    // MessageBox.Show("Target connect failed !");
                    return;
                }

                // RT.Init();
                //Task.Factory.StartNew(()=> {

                //});
            }
            catch (Exception e)
            {
                LOG.Write(e.ToString());
            }
        }



        public void AddLog(int Status, bool IsAdd)
        {
            switch (Status)
            {
                case 0:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": ProcessWell");
                    break;
                case 1:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": DetectorConnectError");
                    break;
                case 2:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": TimeIntervalError");
                    break;
                case 3:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": MemeoryAllocError");
                    break;
                case 4:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": Initializing");
                    break;
                case 5:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": UnStarted");
                    break;
                default:
                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": 未知错误");
                    break;
            }
        }

        public void StartRT()
        {
            try
            {
                ///RT.StartRT();
            }
            catch (Exception e)
            {
                LOG.Write(e.ToString());
            }
        }

        public void Close()
        {
            try
            {
                //DP.StopDepack();
                //TcpSession.UnLink();
                //RT.Close();
                //_thread?.Stop();
            }
            catch (Exception ex)
            {
                LOG.Write("AkOpticsViper Close Error:" + ex.Message);
            }
        }

        public object locker = new object();

        public bool ReadData()
        {
            bool Result = false;
            try
            {

                int lastStatus = _opticsViperInfo.lastStatus;

                OpticsViperInfo = new OpticsViperInfo(ConvertStatusData(Data.status), ConvertTempData(Data.zoneInner),
                    ConvertTempData(Data.zoneMiddle), ConvertTempData(Data.zoneOuter), ConvertTempData(Data.zoneEx));
                if (_opticsViperInfo.lastStatus == lastStatus)
                {
                    IsNewStatus = false;
                }
                else
                {
                    IsNewStatus = true;
                    _opticsViperInfo.SetlastStatus(lastStatus);
                    AddLog(_opticsViperInfo.lastStatus, (_opticsViperInfo.lastStatus == 0 ? false : true));

                }

                Result = true;

            }
            catch (Exception e)
            {
                LOG.Write("AkOpticsViper ReadData Error:" + e.ToString());
            }

            return Result;
        }



        public bool OnTimer()
        {
            try
            {
                if (!TcpSession.IsConnected) //未连接，尝试重新连接
                {
                    Close();
                    if (ReconnectionNumber > 3)
                    {
                        ReconnectionNumber = 0;
                        EV.PostAlarmLog(Module, "AkOptics connection failed");
                        JobThreadStop = true;
                        _thread?.Stop();

                        return false;
                    }

                    if (JobThreadStop)
                    {
                        return false;
                    }

                    Connect();
                    ReconnectionNumber++;
                    return true;
                }

                ReadData();
            }
            catch (Exception ex)
            {
                LOG.Write("AkOpticsViper OnTimer Error：" + ex.Message);
            }

            return true;
        }

        public void ChangeStepID(int stepID, string stepName, string stepDesc)
        {
            try
            {
                //if (RT.Data.status.nStatus == 0)
                //{
                //    RT.ChangeStepID(stepID, stepName, stepDesc);
                //}
                //else
                //{
                //    AddLog(_opticsViperInfo.lastStatus, (_opticsViperInfo.lastStatus == 0 ? false : true));
                //}
            }
            catch (Exception e)
            {
                LOG.Write(e.ToString());
            }
        }

        #region 接口

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            try
            {
                Connect();
                return true;
            }
            catch (Exception ex)
            {
                EV.PostAlarmLog("AkOpticsViper", "AkOpticsViper connect failed.");
                LOG.Write(ex);
                return false;
            }
        }

        public void Monitor()
        {
            //if (!TcpSession.IsConnected)//如果未读取到数据，尝试重新连接
            //{
            //    Close();
            //    Connect();
            //return;
            //}
            //ReadData();
        }

        /// <summary>
        /// 终止
        /// </summary>
        public void Terminate()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 复位
        /// </summary>
        public void Reset()
        {

            if (!TcpSession.IsConnected)
            {
                JobThreadStop = false;
                _thread = null;
                Connect();
            }

            //IsNewStatus = true;
        }

        #endregion

        const int BinSize = 7856;

        void UpdateStatus(byte[] bin)
        {
            IntPtr hPack = Marshal.AllocHGlobal(BinSize);

            Marshal.Copy(bin, 0, hPack, bin.Length);

            Data.status = (BEI_SYSTEM_STATUS_EX)Marshal.PtrToStructure(hPack, typeof(BEI_SYSTEM_STATUS_EX));

            SystemStatus.nStatus = Data.status.nStatus;
            SystemStatus.dTemperatureAverageWaferInner = Data.status.WaferAvg_1;
            SystemStatus.dTemperatureAverageWaferMiddle = Data.status.WaferAvg_2;
            SystemStatus.dTemperatureAverageWaferOutter = Data.status.WaferAvg_3;

            SystemStatus.dTemperatureAverageSusceptorInner = Data.status.SusAvg_1;
            SystemStatus.dTemperatureAverageSusceptorMiddle = Data.status.SusAvg_2;
            SystemStatus.dTemperatureAverageSusceptorOutter = Data.status.SusAvg_3;


            int offset = Marshal.SizeOf(typeof(BEI_SYSTEM_STATUS_EX));
            int sizeTemp = Marshal.SizeOf(typeof(TempData));

            double reflectAvg = 0.0;
            for (int i = 0; i < Data.zoneInner.Length; ++i)
            {
                Data.zoneInner[i] = (TempData)Marshal.PtrToStructure(hPack + offset + i * sizeTemp, typeof(TempData));
                reflectAvg += Data.zoneInner[i].Para2;
            }

            SystemStatus.dReflectWaferAverageInner = reflectAvg / Data.zoneInner.Length;

            reflectAvg = 0.0;

            offset += Data.zoneInner.Length * sizeTemp;

            for (int i = 0; i < Data.zoneMiddle.Length; ++i)
            {
                Data.zoneMiddle[i] = (TempData)Marshal.PtrToStructure(hPack + offset + i * sizeTemp, typeof(TempData));
                reflectAvg += Data.zoneMiddle[i].Para2;
            }

            SystemStatus.dReflectWaferAverageMiddle = reflectAvg / Data.zoneMiddle.Length;

            reflectAvg = 0.0;

            offset += Data.zoneMiddle.Length * sizeTemp;

            for (int i = 0; i < Data.zoneOuter.Length; ++i)
            {
                Data.zoneOuter[i] = (TempData)Marshal.PtrToStructure(hPack + offset + i * sizeTemp, typeof(TempData));
                reflectAvg += Data.zoneOuter[i].Para2;
            }

            SystemStatus.dReflectWaferAverageOutter = reflectAvg / Data.zoneOuter.Length;

            offset += Data.zoneOuter.Length * sizeTemp;

            for (int i = 0; i < Data.zoneEx.Length; ++i)
            {
                Data.zoneEx[i] = (TempData)Marshal.PtrToStructure(hPack + offset + i * sizeTemp, typeof(TempData));
            }

            Marshal.FreeHGlobal(hPack);
        }
    }
}
