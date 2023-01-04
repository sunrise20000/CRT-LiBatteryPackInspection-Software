using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using System.Text.RegularExpressions;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.MAG7
{
    public class Mag7Robot : Robot
    {
        public override bool Error
        {
            get
            {
                return _commErr || _exceuteErr;
            }
        }

        public Mag7Robot(string module, string address)
            : base(module, module, module, module, address, RobotType.MAG7)
        {
        }

        public override void OnDataChanged(string package)
        {
            try
            {
                package = package.ToUpper();
                string[] msgs = Regex.Split(package, delimiter);

                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        bool completed = false;
                        string resp = msg;

                        lock (_locker)
                        {
                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                if (completed)
                                {
                                    _foregroundHandler = null;
                                }
                            }
                            else if (_backgroundHandler != null && _backgroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                if (completed)
                                {
                                    string reason = string.Empty;
                                    QueryState(out reason);
                                    _backgroundHandler = null;
                                }
                            }
                            else
                            {
                                if (_eventHandler != null)
                                {
                                    if (_eventHandler.OnMessage(ref _socket, resp, out completed))
                                    {
                                        if (completed)
                                        {
                                            EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format(" has error. {0:X}", ErrorCode));
                                            _exceuteErr = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ExcuteFailedException e)
            {
                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format("Robot execute failed, {0}", e.Message));
                _exceuteErr = true;

                if (_foregroundHandler != null)
                {
                    _foregroundHandler = null;
                }
                else if (_backgroundHandler != null)
                {
                    _backgroundHandler = null;
                }
            }
            catch (InvalidPackageException e)
            {
                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format("receive invalid package. {0}", e.Message));
            }
            catch (System.Exception ex)
            {
                _commErr = true;
                LOG.Write("Robot failed：" + ex.ToString());
            }
        }

        public override void Reset()
        {
            _exceuteErr = false;
            if (_commErr)
            {
                Connect();
            }
            Swap = false;
        }
    }
}
