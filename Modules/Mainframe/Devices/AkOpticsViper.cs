//using Aitex.Core.Common.DeviceData;
//using Aitex.Core.RT.DataCenter;
//using Aitex.Core.RT.Device;
//using Aitex.Core.RT.Event;
//using Aitex.Core.RT.Log;
//using Aitex.Core.Util;
//using MECF.Framework.Common.CommonData;
//using MECF.Framework.Common.Event;
//using RTOverEthernetDevelopmentKit;
//using System;
//using System.Collections.ObjectModel;
//using System.Runtime.Serialization;

//namespace Mainframe.Devices
//{
//    #region ZM 2021-08-03
//    [DataContract]
//    [Serializable]
//    public class OpticsViperItem : NotifiableItem, IDeviceData
//    {
//        [DataMember]
//        public int WaferNo { get; set; }
//        [DataMember]
//        public double DateTime { get; set; }
//        [DataMember]
//        public double Para1 { get; set; }
//        [DataMember]
//        public double Para2 { get; set; }
//        [DataMember]
//        public double Para3 { get; set; }
//        [DataMember]
//        public double Para4 { get; set; }
//        [DataMember]
//        public double Temperature { get; set; }

//        public OpticsViperItem()
//        {
//            //DisplayName = "Undefined";
//            //WaferNo = 0;
//            //DateTime = 0.0;
//            //Para1 = 0.0;
//            //Para2 = 0.0;
//            //Para3 = 0.0;
//            //Para4 = 0.0;
//            //Temperature = 0.0;
//        }

//        public void Update(IDeviceData data)
//        {
            
//        }
//    }

//    #endregion

//    #region ZM 2021-08-04

//    public class OpticsViperStatus : NotifiableItem
//    {
//        public int Status { get; set; }
//        public int Head { get; set; }
//        public int DetectorMask { get; set; }
//        public int Reserved { get; set; }
//        public double SusAvg_1 { get; set; }
//        public double SusAvg_2 { get; set; }
//        public double SusAvg_3 { get; set; }
//        public double SusAvg_4 { get; set; }
//        public double WaferAvg_1 { get; set; }
//        public double WaferAvg_2 { get; set; }
//        public double WaferAvg_3 { get; set; }
//        public double WaferAvg_4 { get; set; }

//        public OpticsViperStatus()
//        {
//            Status = 0;
//            Head = 0;
//            DetectorMask = 0;
//            Reserved = 0;
//            SusAvg_1 = 0.0;
//            SusAvg_2 = 0.0;
//            SusAvg_3 = 0.0;
//            SusAvg_4 = 0.0;
//            WaferAvg_1 = 0.0;
//            WaferAvg_2 = 0.0;
//            WaferAvg_3 = 0.0;
//            WaferAvg_4 = 0.0;
//        }
//    }

//    #endregion

//    #region ZM 2021-08-04

//    public class OpticsViperInfo
//    {
//        private int _lastStatus = 0;
//        public int lastStatus
//        {
//            get
//            {
//                return _lastStatus;
//            }
//        }

//        private OpticsViperStatus _opticsViperStatus = new OpticsViperStatus();
//        public OpticsViperStatus opticsViperStatus
//        {
//            get
//            {
//                return _opticsViperStatus;
//            }
//        }

//        private AITOpticsViperData[] _innerOpticsViperItem = new AITOpticsViperData[0];
//        public AITOpticsViperData[] innerOpticsViperItem
//        {
//            get
//            {
//                return _innerOpticsViperItem;
//            }
//        }

//        private AITOpticsViperData[] _middleOpticsViperItem = new AITOpticsViperData[0];
//        public AITOpticsViperData[] middleOpticsViperItem
//        {
//            get
//            {
//                return _middleOpticsViperItem;
//            }
//        }

//        private AITOpticsViperData[] _outerOpticsViperItem = new AITOpticsViperData[0];
//        public AITOpticsViperData[] outerOpticsViperItem
//        {
//            get
//            {
//                return _outerOpticsViperItem;
//            }
//        }

//        private AITOpticsViperData[] _exOpticsViperItem = new AITOpticsViperData[0];
//        public AITOpticsViperData[] exOpticsViperItem
//        {
//            get
//            {
//                return _exOpticsViperItem;
//            }
//        }

//        public OpticsViperInfo()
//        {
//            _innerOpticsViperItem = new AITOpticsViperData[0];
//            _middleOpticsViperItem = new AITOpticsViperData[0];
//            _outerOpticsViperItem = new AITOpticsViperData[0];
//            _exOpticsViperItem = new AITOpticsViperData[0];
//        }

//        public OpticsViperInfo(OpticsViperStatus opticsViperStatus, AITOpticsViperData[] innerOpticsViperItem, AITOpticsViperData[] middleOpticsViperItem, AITOpticsViperData[] outerOpticsViperItem)
//        {
//            _opticsViperStatus = opticsViperStatus;
//            _innerOpticsViperItem = innerOpticsViperItem;
//            _middleOpticsViperItem = middleOpticsViperItem;
//            _outerOpticsViperItem = outerOpticsViperItem;
//        }

//        public OpticsViperInfo(OpticsViperStatus opticsViperStatus, AITOpticsViperData[] innerOpticsViperItem, AITOpticsViperData[] middleOpticsViperItem, AITOpticsViperData[] outerOpticsViperItem, AITOpticsViperData[] exOpticsViperItem)
//        {
//            _opticsViperStatus = opticsViperStatus;
//            _innerOpticsViperItem = innerOpticsViperItem;
//            _middleOpticsViperItem = middleOpticsViperItem;
//            _outerOpticsViperItem = outerOpticsViperItem;
//            _exOpticsViperItem = exOpticsViperItem;
//            _lastStatus = opticsViperStatus.Status;
//        }

//        public void SetlastStatus(int lastStatus)
//        {
//            _lastStatus = lastStatus;
//        }
//    }

//    public class AkOpticsViper : IDevice
//    {
//        public AkOpticsViper(string module, string name)
//        {
//            this.Name = name;
//            this.Module = module;
//            RT = new RTOverEthernet();

//            //DATA.Subscribe($"PM1.{Name}.OpticsViperInfo", () => _opticsViperInfo);
//        }
//        private RTOverEthernet RT = null;
//        private bool IsNewStatus = false;
//        private PeriodicJob _thread;//工作线程

//        private OpticsViperInfo _opticsViperInfo = new OpticsViperInfo();

//        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

//        /// <summary>
//        /// 接受到数据时触发事件
//        /// </summary>
//        public event Action<OpticsViperInfo> DataReceivedEvent;
//        /// <summary>
//        /// 通讯数据
//        /// </summary>
//        public OpticsViperInfo OpticsViperInfo
//        {
//            get
//            {
//                return _opticsViperInfo;
//            }
//            set
//            {
//                _opticsViperInfo = value;
//                DataReceivedEvent?.Invoke(_opticsViperInfo);
//            }
//        }
       
//        //public AITOpticsViperData[] innerOpticsViperItem
//        //{
//        //    get;
//        //    set;
//        //}

      
//        //public AITOpticsViperData[] middleOpticsViperItem
//        //{
//        //    get;
           
//        //    set;
           
//        //}

     
//        //public AITOpticsViperData[] outerOpticsViperItem
//        //{
//        //    get;
//        //    set;

//        //}

//        //private OpticsViperItem[] _exOpticsViperItem = new OpticsViperItem[0];
//        //public OpticsViperItem[] exOpticsViperItem
//        //{
//        //    get
//        //    {
//        //        return _exOpticsViperItem;
//        //    }
//        //}
//       // public OpticsViperItem[] InnerOpticsViperItem { get; set; }
//        public string Module { get; set; }
//        public string Name { get; set; }
//        public bool HasAlarm => false;
//        public AkOpticsViper()
//        {
//            RT = new RTOverEthernet();
//            RT.CommunicationBroken += RT_CommunicationBroken;
//        }

//        private void RT_CommunicationBroken(object sender, RTCommunicationEventArgs e)
//        {
//            LOG.Write("AkOptics-" + e.Command);
//        }

//        public OpticsViperItem InitOpticsViperItem()
//        {
//            OpticsViperItem opticsViperItem = new OpticsViperItem();

//            opticsViperItem.WaferNo = 0;
//            opticsViperItem.DateTime = 0.0;
//            opticsViperItem.Para1 = 0.0;
//            opticsViperItem.Para2 = 0.0;
//            opticsViperItem.Para3 = 0.0;
//            opticsViperItem.Para4 = 0.0;
//            opticsViperItem.Temperature = 0.0;

//            return opticsViperItem;
//        }
      
//        public AITOpticsViperData ConvertTempData(int WaferNo, TempData tempData)
//        {
//            AITOpticsViperData opticsViperItem = new AITOpticsViperData();

//            opticsViperItem.WaferNo = WaferNo;
//            opticsViperItem.DateTime = tempData.DateTime;
//            opticsViperItem.Para1 = tempData.Para1;
//            opticsViperItem.Para2 = tempData.Para2;
//            opticsViperItem.Para3 = tempData.Para3;
//            opticsViperItem.Para4 = tempData.Para4;
//            opticsViperItem.Temperature = tempData.Temperature;

//            return opticsViperItem;
//        }
      
//        public AITOpticsViperData[] ConvertTempData(TempData[] tempData)
//        {
//            AITOpticsViperData[] opticsViperItem = new AITOpticsViperData[tempData.Length];
//            for (int i = 0; i < tempData.Length; i++)
//            {
              
//               opticsViperItem[i] = ConvertTempData(i + 1, tempData[i]);
//            }

//            return opticsViperItem;
//        }
//        public OpticsViperStatus ConvertStatusData(BEI_SYSTEM_STATUS_EX statusData)
//        {
//            OpticsViperStatus opticsViperStatus = new OpticsViperStatus();

//            opticsViperStatus.Status = statusData.nStatus;
//            opticsViperStatus.Head = statusData.Head;
//            opticsViperStatus.DetectorMask = statusData.DetectorMask;
//            opticsViperStatus.Reserved = statusData.Reserved;
//            opticsViperStatus.SusAvg_1 = statusData.SusAvg_1;
//            opticsViperStatus.SusAvg_2 = statusData.SusAvg_2;
//            opticsViperStatus.SusAvg_3 = statusData.SusAvg_3;
//            opticsViperStatus.SusAvg_4 = statusData.SusAvg_4;
//            opticsViperStatus.WaferAvg_1 = statusData.WaferAvg_1;
//            opticsViperStatus.WaferAvg_2 = statusData.WaferAvg_2;
//            opticsViperStatus.WaferAvg_3 = statusData.WaferAvg_3;
//            opticsViperStatus.WaferAvg_4 = statusData.WaferAvg_4;

//            return opticsViperStatus;
//        }

//        public void Connect()
//        {
//            try
//            {
//                RT.Init();
//                _thread = new PeriodicJob(1000, OnTimer, "AkOpticsTimer", true);//测试用，直接连接
//                DATA.Subscribe($"PM1.{Name}.InnerOpticsViperItem", () => _opticsViperInfo?.innerOpticsViperItem);
//                DATA.Subscribe($"PM1.{Name}.MiddleOpticsViperItem", () => _opticsViperInfo?.middleOpticsViperItem);
//                DATA.Subscribe($"PM1.{Name}.OuterOpticsViperItem", () => _opticsViperInfo?.outerOpticsViperItem);
//                DATA.Subscribe($"PM1.{Name}.ExOpticsViperItem", () => _opticsViperInfo?.exOpticsViperItem);
//                DATA.Subscribe($"PM1.{Name}.OpticsViperStatus", () => _opticsViperInfo?.opticsViperStatus);

//                //Task.Factory.StartNew(()=> {

//                //});
//            }
//            catch (Exception e)
//            {
//                LOG.Write(e.ToString());
//            }
//        }
   
//        public void SendRunInfo(string runID, string susID)
//        {
//            //Acceptable value: 
//            //ProcessWell = 0
//            //DetectorConnectError = 1 探测器连接错误=1
//            //TimeIntervalError = 2(posible freeze)时间间隔错误=2（可能冻结）
//            //MemeoryAllocError = 3, 记忆和忠诚错误=3，
//            //Initializing = 4 //Default Temperature status 默认温度状态
//            //UnStarted = 5//未启动的


//            //            LOG.Write("");
//            //            LOG.Write("error reload recipe file list, type = " + processType);
//            //            可接受的值：
//            //processwell = 0
//            //DetectorConnectError = 1
//            //时间间隔错误 = 2（可能冻结）
//            //memoryallocerror = 3，
//            //初始化 = 4//默认温度状态
//            //未启动的

//            //RT.SystemStatus.nStatus
//            try
//            {
//                if (RT.Data.status.nStatus == 0)
//                {
//                    RT.SendRunInfo(runID, susID);
//                }
//                else
//                {
//                    AddLog(_opticsViperInfo.lastStatus, (_opticsViperInfo.lastStatus == 0 ? false : true));
//                }
//            }
//            catch (Exception e)
//            {
//                LOG.Write(e.ToString());
//            }
//        }

//        public void AddLog(int Status, bool IsAdd)
//        {
//            switch (Status)
//            {
//                case 0:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": ProcessWell");
//                    break;
//                case 1:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": DetectorConnectError");
//                    break;
//                case 2:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": TimeIntervalError");
//                    break;
//                case 3:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": MemeoryAllocError");
//                    break;
//                case 4:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": Initializing");
//                    break;
//                case 5:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": UnStarted");
//                    break;
//                default:
//                    if (IsAdd) LOG.Write("AkOptics-" + Status.ToString() + ": 未知错误");
//                    break;
//            }
//        }

//        public void StartRT()
//        {
//            try
//            {
//                RT.StartRT();
//            }
//            catch (Exception e)
//            {
//                LOG.Write(e.ToString());
//            }
//        }

//        public void Close()
//        {
//            try
//            {
//                RT.Close();
//                _thread?.Stop();
//            }
//            catch (Exception ex)
//            {
//                LOG.Write("AkOpticsViper Close Error:" + ex.Message);
//            }
//        }
//        public object locker = new object();

//        public bool ReadData()
//        {
//            bool Result = false;
//            try
//            {
//                //lock (locker)
//                {
//                    int lastStatus = _opticsViperInfo.lastStatus;
//                    //innerOpticsViperItem = ConvertTempData1(RT.Data.zoneInner);
//                    //middleOpticsViperItem = ConvertTempData1(RT.Data.zoneMiddle);
//                    //outerOpticsViperItem= ConvertTempData1(RT.Data.zoneOuter);
//                    OpticsViperInfo = new OpticsViperInfo(ConvertStatusData(RT.Data.status), ConvertTempData(RT.Data.zoneInner), ConvertTempData(RT.Data.zoneMiddle), ConvertTempData(RT.Data.zoneOuter), ConvertTempData(RT.Data.zoneEx));
//                    if (_opticsViperInfo.lastStatus == lastStatus)
//                    {
//                        IsNewStatus = false;
//                    }
//                    else
//                    {
//                        IsNewStatus = true;
//                        _opticsViperInfo.SetlastStatus(lastStatus);
//                        AddLog(_opticsViperInfo.lastStatus, (_opticsViperInfo.lastStatus == 0 ? false : true));
//                    }

//                    Result = true;
//                }
//            }
//            catch (Exception e)
//            {
//                LOG.Write("AkOpticsViper ReadData Error:" + e.ToString());
//            }

//            return Result;
//        }

//        public bool OnTimer()
//        {
//            try
//            {
//                //if (RT.Data.status.nStatus == 0)//如果未读取到数据，尝试重新连接
//                //{
//                //    Close();
//                //    Connect();
//                //}
//                ReadData();
//            }
//            catch (Exception ex)
//            {
//                LOG.Write("AkOpticsViper OnTimer Error：" + ex.Message);
//            }
//            return true;
//        }

//        public void ChangeStepID(int stepID, string stepName, string stepDesc)
//        {
//            try
//            {
//                if (RT.Data.status.nStatus == 0)
//                {
//                    RT.ChangeStepID(stepID, stepName, stepDesc);
//                }
//                else
//                {
//                    AddLog(_opticsViperInfo.lastStatus, (_opticsViperInfo.lastStatus == 0 ? false : true));
//                }
//            }
//            catch (Exception e)
//            {
//                LOG.Write(e.ToString());
//            }
//        }

//        #region 接口
//        /// <summary>
//        /// 初始化
//        /// </summary>
//        /// <returns></returns>
//        public bool Initialize()
//        {
//            try
//            {
//                Connect();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                EV.PostAlarmLog("AkOpticsViper", "AkOpticsViper connect failed.");
//                LOG.Write(ex);
//                return false;
//            }
//        }

//        public void Monitor()
//        {
//            return;
//        }

//        /// <summary>
//        /// 终止
//        /// </summary>
//        public void Terminate()
//        {
//            try
//            {
//                Close();
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Trace.WriteLine(ex.Message);
//            }
//        }
//        /// <summary>
//        /// 复位
//        /// </summary>
//        public void Reset()
//        {
//            //_Job.Stop();
//            //Close();
//            //IsNewStatus = true;
//        }
//        #endregion
//    }

//    #endregion
//}
