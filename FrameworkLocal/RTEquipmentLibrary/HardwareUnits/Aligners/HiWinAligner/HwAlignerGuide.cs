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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner
{
    public class HwAlignerGuide : BaseDevice, IConnection, IDevice
    {
        private HwAlignerGuideConnection _connection;

        private bool _activeMonitorStatus;
        
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private bool bIsDisconnect = false;
        enum OperName
        {
            NULL,
            SME11,
            SME21,
            SME31,
            HOM,
            DOC,
            CVN,
            MTM,
            CCD,
            BAL,
            MVR,
            CVF,
            ERS,
            STA,
            //
            SPS,
            WSZ,
            _WT,
            GLM,
            FWO,            
            
        };
        uint iCurrentOper = 0xff;
        List<byte> liReceivedData = new List<byte>();
        List<byte> liReceivedDataA = new List<byte>();
        byte bEnd = 0x0d;
        bool bCmdOver = true;
        //
        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;

        public bool HaveWafer
        {
            get; set;
        }

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
        public bool IsBusy => _lstHandler.Count > 0 || this.iCurrentOper != (uint)OperName.NULL;

        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public HwAlignerGuide(string module, string scRoot, string name) : base(module, name, name, name)
        {

            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        public bool Initialize()
        {
            // string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            string portName = SC.GetStringValue($"{Name}.Address");
            Address = portName;
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new HwAlignerGuideConnection(portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(200, OnTimer, $"{Name} MonitorHandler", true);

            //设置操作
            this.SetOP();

            //设置属性
            //this.SetAttr();
            //
            //this.InitMachine();
            //
            ConnectionManager.Instance.Subscribe($"{Name}", this);
            //SetServoOn();
            return true;
        }
        /// <summary>
        /// 设置操作
        /// </summary>
        private void SetOP()
        {
            DATA.Subscribe($"{Module}.{Name}.HaveWafer", () => HaveWafer);

            //参数保存
            OP.Subscribe($"{Module}.{Name}.HwSaveParameters", (cmd, param) =>
            {
                Set("SPS");
                return true;
            });
            //读取/设定晶圆尺寸
            OP.Subscribe($"{Module}.{Name}.HwWaferSize", (cmd, param) =>
            {
                Set("WSZ" + " " + param[0]);
                return true;
            });
            //晶圆种类
            OP.Subscribe($"{Module}.{Name}.HwWaferType", (cmd, param) =>
            {
                Set("_WT" + " " + param[0]);
                return true;
            });
            //寻边材质
            OP.Subscribe($"{Module}.{Name}.HwWaferMaterial", (cmd, param) =>
            {
                Set("GLM" + " " + param[0]);
                return true;
            });
            //晶圆方向
            OP.Subscribe($"{Module}.{Name}.HwWaferOrientation", (cmd, param) =>
            {
                Set("FWO" + " " + param[0]);
                return true;
            });

            //------------------------------------------------------------
            
            //原点复归
            OP.Subscribe($"{Module}.{Name}.HwHOM", (cmd, param) =>
            {
                return MsgHome();
            });
            //晶圆确认
            OP.Subscribe($"{Module}.{Name}.HwDOC", (cmd, param) =>
            {
                return CheckWaferLoad();
            });
            //开启真空
            OP.Subscribe($"{Module}.{Name}.HwCVN", (cmd, param) =>
            {
                return OpenVacuum();
            });
            //寻边补正
            OP.Subscribe($"{Module}.{Name}.HwBAL", (cmd, param) =>
            {
                return MsgAliger();
            });
            //关闭真空
            OP.Subscribe($"{Module}.{Name}.HwCVF", (cmd, param) =>
            {
                return CloseVaccum();
            });
            //移至测量中心点
            OP.Subscribe($"{Module}.{Name}.HwMTM", (cmd, param) =>
            {
                return MoveToRobotPutPlace();
            });
            //清除报警
            OP.Subscribe($"{Module}.{Name}.HwERS", (cmd, param) =>
            {
                return ClearError();
            });
            //马达激磁
            OP.Subscribe($"{Module}.{Name}.HwSME", (cmd, param) =>
            {
                InitMachine();
                return true;
            });

            //------------------------------------------------------------
            //相对位移
            OP.Subscribe($"{Module}.{Name}.HwMVR", (cmd, param) =>
            {
                Set("MVR");
                return true;
            });
            //
            OP.Subscribe($"{Module}.{Name}.HwAbort", (cmd, param) =>
            {
                Abort();
                return true;
            });


        }

        public void Abort()
        {

        }

        public void InitMachine()
        {
            _lstHandler.Clear(); 
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "SME11", "SME 11"));
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "SME12", "SME 21"));
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "SME13", "SME 31"));
            }
        }

        public void Aligner()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "CVN", "CVN"));
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "BAL", "BAL"));
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "CVF", "CVF"));
            }
        }


        public bool MsgHome()
        {
            //_lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "HOM", "HOM", 30));
            }
                return true;
        }


        public bool CheckWaferLoad()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerWaferCheckHandler(this, "DOC", "DOC"));
            }
            return true;
        }

        public bool OpenVacuum()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "CVN", "CVN"));
            }
            return true;
        }

        public bool CloseVaccum()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "CVF", "CVF"));
            }
            return true;
        }

        public bool MoveToRobotPutPlace()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "MTM", "MTM"));
            }
            return true;
        }


        public bool MsgAliger()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "BAL", "BAL"));
            }
            return true;
        }


        public bool DoAliger()
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "CVN", "CVN"));
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "BAL", "BAL"));
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "CVF", "CVF"));
            }
            return true;
        }


        public bool ClearError()
        {
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "ERS", "ERS"));
            }
            return true;
        }


        /// <summary>
        /// 加入命令队列
        /// </summary>
        /// <param name = "strCmd" > 命令 Ascii格式</param>
        public void Set(string strCmd)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HwAlignerGuideSetAHandler(this, "ERS", strCmd));
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

                if ((!_connection.IsConnected) || _connection.IsCommunicationError || this.bIsDisconnect)
                {
                    _trigRetryConnect.CLK = this.bIsDisconnect;

                    //重连机制要改，timeout之后，要是用心跳方式探测重连

                    _trigRetryConnect.CLK = this.bIsDisconnect;
                    if (_connection.Connect())
                    {
                        this.bIsDisconnect = false;
                        //
                        if (_trigRetryConnect.Q)
                        {
                            EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                            LOG.Write($"{Module}.{Name} connected");
                        }
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
                        if (this.bCmdOver)
                        {
                            this.bCmdOver = false;
                            _connection.Execute(handler);
                        }

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
            this.bIsDisconnect = _connection.IsCommunicationError;
            _connecteTimes = 0;
            _connection.SetCommunicationError(false, "");//这里重置了CommunicationError ，就是去了OnTimer里面的重连的触发，所以需要另设变量以触发之(使用timeout来触发的情况）
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");
       
            _trigCommunicationError.RST = true;
            _trigRetryConnect.RST = true; 
            this.iCurrentOper = (uint)OperName.NULL;

            _lstHandler.Clear();
            this.bCmdOver = true;
        }

        public void SetCurrentOper()
        {
            this.iCurrentOper = (uint)OperName.CCD;
        }

        public bool PraseDataA(string name, byte[] buffer,out bool bResult)
        {
            bResult = true;
            this.bCmdOver = true;
            string sAnswer = Encoding.ASCII.GetString(buffer);
            if (sAnswer.Contains("END"))
            {
                this.iCurrentOper = (uint)OperName.NULL;
                
                return true;
            }
            else if (sAnswer.Contains("ERR"))
            {
                lock (_locker)
                {
                    _lstHandler.Clear();
                }
                this.iCurrentOper = (uint)OperName.NULL;
                bResult = false;
                
                return true; //Handled
            }
            else
            {
                this.bCmdOver = false;
            }
            return false;
        }

        public bool CheckWaferDataA(string name, byte[] buffer, out bool bResult)
        {
            bResult = true;
            this.bCmdOver = true;
            string sAnswer = Encoding.ASCII.GetString(buffer.ToArray());

            LOG.Write($"HiWin Aligner {nameof(CheckWaferDataA)}() respond: {sAnswer}");

            if (sAnswer.Contains("END"))
            {
                HaveWafer = sAnswer.Contains("1\r\nEND");
                this.iCurrentOper = (uint)OperName.NULL;
                
                return true;
            }
            else if (sAnswer.Contains("ERR"))
            {
                lock (_locker)
                {
                    _lstHandler.Clear();
                }
                this.iCurrentOper = (uint)OperName.NULL;
                bResult = false;
                
                return false;     //Handle
            }
            else
            {
                this.bCmdOver = true;
            }
            return false;
        }
       
    }
}
