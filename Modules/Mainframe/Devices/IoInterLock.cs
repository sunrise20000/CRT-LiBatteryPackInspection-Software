using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using System;
using System.Xml;

namespace Mainframe.Devices
{
    public class IoInterLock : BaseDevice, IDevice
    {
        //public bool DoTMAtProcessPress
        //{
        //    get
        //    {
        //        if (_doTMAtProcessPress != null)
        //        {
        //            return _doTMAtProcessPress.Value;
        //        }
        //        return false;
        //    }
        //}

        public bool DoLLAtProcessPress
        {
            get
            {
                if (_doLLAtProcessPress != null)
                {
                    return _doLLAtProcessPress.Value;
                }
                return false;
            }
        }

        public bool DiTMatATM
        {
            //在大气模式下取0,非大气取1
            get
            {
                if (_diTMatATM != null)
                {
                    return !_diTMatATM.Value;
                }
                return false;
            }
        }

        
        public bool DiTMUnderVac
        {
            get
            {
                if (_diTMUnderVac != null)
                {
                    return _diTMUnderVac.Value;
                }
                return false;
            }
        }


        public bool DiLoadLockAtATM
        {
            get
            {
                if (_aiLLPressure != null)
                {
                    return _aiLLPressure.FloatValue > _scLoadLockAtmBasePressure.DoubleValue;
                }
                return false;
            }
        }

        public bool DoTmCyclePurgeRoutineRunning
        {
            get
            {
                if (_doTmCyclePurgeRoutineRunning != null)
                {
                    return _doTmCyclePurgeRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doTmCyclePurgeRoutineRunning != null)
                {
                    _doTmCyclePurgeRoutineRunning.Value = value;
                }
            }
        }

        public bool DoTmLeakCheckRoutineRunning
        {
            get
            {
                if (_doTmLeakCheckRoutineRunning != null)
                {
                    return _doTmLeakCheckRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doTmLeakCheckRoutineRunning != null)
                {
                    _doTmLeakCheckRoutineRunning.Value = value;
                }
            }
        }

        public bool DoTmPumpDownRoutineRunning
        {
            get
            {
                if (_doTmPumpDownRoutineRunning != null)
                {
                    return _doTmPumpDownRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doTmPumpDownRoutineRunning != null)
                {
                    _doTmPumpDownRoutineRunning.Value = value;
                }
            }
        }

        public bool DoTmServoPressRoutineRunning
        {
            get
            {
                if (_doTmServoPressRoutineRunning != null)
                {
                    return _doTmServoPressRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doTmServoPressRoutineRunning != null)
                {
                    _doTmServoPressRoutineRunning.Value = value;
                }
            }
        }

        public bool DoTmVentUpRoutineRunning
        {
            get
            {
                if (_doTmVentUpRoutineRunning != null)
                {
                    return _doTmVentUpRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doTmVentUpRoutineRunning != null)
                {
                    _doTmVentUpRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLLCyclePurgeRoutineRunning
        {
            get
            {
                if (_doLLCyclePurgeRoutineRunning != null)
                {
                    return _doLLCyclePurgeRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doLLCyclePurgeRoutineRunning != null)
                {
                    _doLLCyclePurgeRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLLLeakCheckRoutineRunning
        {
            get
            {
                if (_doLLLeakCheckRoutineRunning != null)
                {
                    return _doLLLeakCheckRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doLLLeakCheckRoutineRunning != null)
                {
                    _doLLLeakCheckRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLLPumpDownRoutineRunning
        {
            get
            {
                if (_doLLPumpDownRoutineRunning != null)
                {
                    return _doLLPumpDownRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doLLPumpDownRoutineRunning != null)
                {
                    _doLLPumpDownRoutineRunning.Value = value;
                }
            }
        }

        public bool DoLLVentUpRoutineRunning
        {
            get
            {
                if (_doLLVentUpRoutineRunning != null)
                {
                    return _doLLVentUpRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doLLVentUpRoutineRunning != null)
                {
                    _doLLVentUpRoutineRunning.Value = value;
                }
            }
        }









        public bool DoUnLoadPurgeRoutineRunning
        {
            get
            {
                if (_doUnLoadPurgeRoutineRunning != null)
                {
                    return _doUnLoadPurgeRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doUnLoadPurgeRoutineRunning != null)
                {
                    _doUnLoadPurgeRoutineRunning.Value = value;
                }
            }
        }

        public bool DoUnLoadLeakCheckRoutineRunning
        {
            get
            {
                if (_doUnLoadLeakCheckRoutineRunning != null)
                {
                    return _doUnLoadLeakCheckRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doUnLoadLeakCheckRoutineRunning != null)
                {
                    _doUnLoadLeakCheckRoutineRunning.Value = value;
                }
            }
        }

        public bool DoUnLoadPumpDownRoutineRunning
        {
            get
            {
                if (_doUnLoadPumpDownRoutineRunning != null)
                {
                    return _doUnLoadPumpDownRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doUnLoadPumpDownRoutineRunning != null)
                {
                    _doUnLoadPumpDownRoutineRunning.Value = value;
                }
            }
        }

        public bool DoUnLoadVentUpRoutineRunning
        {
            get
            {
                if (_doUnLoadVentUpRoutineRunning != null)
                {
                    return _doUnLoadVentUpRoutineRunning.Value;
                }
                return false;
            }
            set
            {
                if (_doUnLoadVentUpRoutineRunning != null)
                {
                    _doUnLoadVentUpRoutineRunning.Value = value;
                }
            }
        }



        public bool DoVacRobotExtendPMAEnable
        {
            get
            {
                if (_doVacRobotExtendPMAEnable != null)
                {
                    return _doVacRobotExtendPMAEnable.Value;
                }
                return false;
            }
            set
            {
                if (_doVacRobotExtendPMAEnable != null)
                {
                    _doVacRobotExtendPMAEnable.Value = value;
                }
            }
        }
        public bool DoVacRobotExtendBufferEnable
        {
            set
            {
                if (_doVacRobotExtendBufferEnable != null)
                {
                    _doVacRobotExtendBufferEnable.Value = value;
                }
            }
        }
        public bool DoVacRobotExtenLLEnable
        {           
            set
            {
                if (_doVacRobotExtenLLEnable != null)
                {
                    _doVacRobotExtenLLEnable.Value = value;
                }
            }
        }

        public bool DoVacRobotExtendUnloadEnable
        {
            set
            {
                if (_doVacRobotExtendUnloadEnable != null)
                {
                    _doVacRobotExtendUnloadEnable.Value = value;
                }
            }
        }

        public bool DoVacRobotExtendPMBEnable
        {
            set
            {
                if (_doVacRobotExtendPMBEnable != null)
                {
                    _doVacRobotExtendPMBEnable.Value = value;
                }
            }
        }

        public bool DoATMRobotExtendUnloadEnable
        {
            set
            {
                if (_doATMRobotExtendUnloadEnable != null)
                {
                    _doATMRobotExtendUnloadEnable.Value = value;
                }
            }
        }
        public bool DoATMRobotExtendLoaLSideEnable
        {
            set
            {
                if (_doATMRobotExtendLoaLSideEnable != null)
                {
                    _doATMRobotExtendLoaLSideEnable.Value = value;
                }
            }
        }

        public bool DoATMRobotExtendLoaRSideEnable
        {
            set
            {
                if (_doATMRobotExtendLoaRSideEnable != null)
                {
                    _doATMRobotExtendLoaRSideEnable.Value = value;
                }
            }
        }




        public bool DiVacRobotExtenLLEnableFB
        {
            get
            {
                if (_diVacRobotExtenLLEnableFB != null)
                {
                    return _diVacRobotExtenLLEnableFB.Value;
                }
                return false;
            }
        }
        public bool DiVacRobotExtendBufferEnableFB
        {
            get
            {
                if (_diVacRobotExtendBufferEnableFB != null)
                {
                    return _diVacRobotExtendBufferEnableFB.Value;
                }
                return false;
            }
        }
        public bool DiVacRobotExtendPMAEnableFB
        {
            get
            {
                if (_diVacRobotExtendPMAEnableFB != null)
                {
                    return _diVacRobotExtendPMAEnableFB.Value;
                }
                return false;
            }
        }

        public bool DiVacRobotExtendUnloadEnableFB
        {
            get
            {
                if (_diVacRobotExtendUnloadEnableFB != null)
                {
                    return _diVacRobotExtendUnloadEnableFB.Value;
                }
                return false;
            }
        }
        public bool DiVacRobotExtendPMBEnableFB
        {
            get
            {
                if (_diVacRobotExtendPMBEnableFB != null)
                {
                    return _diVacRobotExtendPMBEnableFB.Value;
                }
                return false;
            }
        }
        public bool DiATMRobotExtendUnloadEnableFB
        {
            get
            {
                if (_diATMRobotExtendUnloadEnableFB != null)
                {
                    return _diATMRobotExtendUnloadEnableFB.Value;
                }
                return false;
            }
        }
        public bool DiATMRobotExtendLoadLSideEnableFB
        {
            get
            {
                if (_diATMRobotExtendLoadLSideEnableFB != null)
                {
                    return _diATMRobotExtendLoadLSideEnableFB.Value;
                }
                return false;
            }
        }
        public bool DiATMRobotExtendLoadRSideEnableFB
        {
            get
            {
                if (_diATMRobotExtendLoadRSideEnableFB != null)
                {
                    return _diATMRobotExtendLoadRSideEnableFB.Value;
                }
                return false;
            }
        }






        public bool DoRectorAATMTransferReady
        {
            get
            {
                if (_doRectorAATMTransferReady != null)
                {
                    return _doRectorAATMTransferReady.Value;
                }
                return false;
            }
            set
            {
                if (_doRectorAATMTransferReady != null)
                {
                    _doRectorAATMTransferReady.Value = value;
                }
            }
        }

        public bool DoRectorAProcessTransferReady
        {
            get
            {
                if (_doRectorAProcessTransferReady != null)
                {
                    return _doRectorAProcessTransferReady.Value;
                }
                return false;
            }
            set
            {
                if (_doRectorAProcessTransferReady != null)
                {
                    _doRectorAProcessTransferReady.Value = value;
                }
            }
        }

        public bool DoPm1LidClosed
        {
            get
            {
                if (_doPm1LidClosed != null)
                {
                    return _doPm1LidClosed.Value;
                }
                return false;
            }
            set
            {
                if (_doPm1LidClosed != null)
                {
                    _doPm1LidClosed.Value = value;
                }
            }
        }

        private DOAccessor _doTMAtProcessPress = null;
        private DOAccessor _doLLAtProcessPress = null;
        private DIAccessor _diTMatATM = null;
        private DIAccessor _diLoadLockAtATM = null;
        private AIAccessor _aiTmPressure = null;
        private AIAccessor _aiLLPressure = null;
        private DIAccessor _diTMUnderVac = null;

        private DOAccessor _doTmCyclePurgeRoutineRunning = null;
        private DOAccessor _doTmLeakCheckRoutineRunning = null;
        private DOAccessor _doTmPumpDownRoutineRunning = null;
        private DOAccessor _doTmServoPressRoutineRunning = null;
        private DOAccessor _doTmVentUpRoutineRunning = null;
        private DOAccessor _doLLCyclePurgeRoutineRunning = null;
        private DOAccessor _doLLLeakCheckRoutineRunning = null;
        private DOAccessor _doLLPumpDownRoutineRunning = null;
        private DOAccessor _doLLVentUpRoutineRunning = null;


        private DIAccessor _diVacRobotExtenLLEnableFB = null;
        private DIAccessor _diVacRobotExtendBufferEnableFB = null;
        private DIAccessor _diVacRobotExtendPMAEnableFB = null;
        private DIAccessor _diVacRobotExtendUnloadEnableFB = null; //20220713
        private DIAccessor _diVacRobotExtendPMBEnableFB  = null; //20220713
        private DIAccessor _diATMRobotExtendUnloadEnableFB = null; //20220713
        private DIAccessor _diATMRobotExtendLoadLSideEnableFB = null; //20220713
        private DIAccessor _diATMRobotExtendLoadRSideEnableFB = null; //20220713
        private DOAccessor _doVacRobotExtenLLEnable = null;
        private DOAccessor _doVacRobotExtendBufferEnable = null;
        private DOAccessor _doVacRobotExtendPMAEnable = null;
        private DOAccessor _doVacRobotExtendUnloadEnable = null; //20220713
        private DOAccessor _doVacRobotExtendPMBEnable = null; //20220713
        private DOAccessor _doATMRobotExtendUnloadEnable = null; //20220713
        private DOAccessor _doATMRobotExtendLoaLSideEnable = null; //20220713
        private DOAccessor _doATMRobotExtendLoaRSideEnable = null; //20220713


        private DOAccessor _doRectorAATMTransferReady = null;
        private DOAccessor _doRectorAProcessTransferReady = null;
        private DOAccessor _doPm1LidClosed = null;

        private DOAccessor _doUnLoadPurgeRoutineRunning = null;
        private DOAccessor _doUnLoadLeakCheckRoutineRunning = null;
        private DOAccessor _doUnLoadPumpDownRoutineRunning = null;
        private DOAccessor _doUnLoadVentUpRoutineRunning = null;


        private SCConfigItem _scLoadLockVacBasePressure;
        private SCConfigItem _scLoadLockAtmBasePressure;

        public IoInterLock(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doTMAtProcessPress = ParseDoNode("doTmAtProcessPress", node, ioModule);
            _doLLAtProcessPress = ParseDoNode("doLLAtProcessPress", node, ioModule);
            _diTMatATM = ParseDiNode("diTmAtATM", node, ioModule);
            _diLoadLockAtATM = ParseDiNode("diLoadLockAtATm", node, ioModule); 
            _aiTmPressure = ParseAiNode("aiTmPressure", node, ioModule);
            _aiLLPressure = ParseAiNode("aiLLPressure", node, ioModule);    
            _diTMUnderVac = ParseDiNode("diTmUnderVac", node, ioModule); 

            _doTmCyclePurgeRoutineRunning = ParseDoNode("doTmCyclePurgeRoutineRunning", node, ioModule);
            _doTmLeakCheckRoutineRunning = ParseDoNode("doTmLeakCheckRoutineRunning", node, ioModule);
            _doTmPumpDownRoutineRunning = ParseDoNode("doTmPumpDownRoutineRunning", node, ioModule);
            _doTmServoPressRoutineRunning = ParseDoNode("doTmServoPressRoutineRunning", node, ioModule);
            _doTmVentUpRoutineRunning = ParseDoNode("doTmVentUpRoutineRunning", node, ioModule);
            _doLLCyclePurgeRoutineRunning = ParseDoNode("doLLCyclePurgeRoutineRunning", node, ioModule);
            _doLLLeakCheckRoutineRunning = ParseDoNode("doLLLeakCheckRoutineRunning", node, ioModule);
            _doLLPumpDownRoutineRunning = ParseDoNode("doLLPumpDownRoutineRunning", node, ioModule);
            _doLLVentUpRoutineRunning = ParseDoNode("doLLVentUpRoutineRunning", node, ioModule);

            _diVacRobotExtenLLEnableFB = ParseDiNode("diVacRobotExtenLLEnableFB", node, ioModule);
            _diVacRobotExtendBufferEnableFB = ParseDiNode("diVacRobotExtendBufferEnableFB", node, ioModule);
            _diVacRobotExtendPMAEnableFB = ParseDiNode("diVacRobotExtendPMAEnableFB", node, ioModule);
            _doVacRobotExtenLLEnable = ParseDoNode("doVacRobotExtenLLEnable", node, ioModule);
            _doVacRobotExtendBufferEnable = ParseDoNode("doVacRobotExtendBufferEnable", node, ioModule);
            _doVacRobotExtendPMAEnable = ParseDoNode("doVacRobotExtendPMAEnable", node, ioModule);



            _diVacRobotExtendUnloadEnableFB = ParseDiNode("diVacRobotExtendUnloadEnableFB", node, ioModule); ; //20220713
            _diVacRobotExtendPMBEnableFB = ParseDiNode("diVacRobotExtendPMBEnableFB", node, ioModule); ; //20220713
            _diATMRobotExtendUnloadEnableFB = ParseDiNode("diATMRobotExtendUnloadEnableFB", node, ioModule); ; //20220713
            _diATMRobotExtendLoadLSideEnableFB = ParseDiNode("diATMRobotExtendLoadLSideEnableFB", node, ioModule); ; //20220713
            _diATMRobotExtendLoadRSideEnableFB = ParseDiNode("diATMRobotExtendLoadRSideEnableFB", node, ioModule); ; //20220713
            _doVacRobotExtendUnloadEnable = ParseDoNode("doVacRobotExtendUnloadEnable", node, ioModule); //20220713
            _doVacRobotExtendPMBEnable = ParseDoNode("doVacRobotExtendPMBEnable", node, ioModule); //20220713
            _doATMRobotExtendUnloadEnable = ParseDoNode("doATMRobotExtendUnloadEnable", node, ioModule); //20220713
            _doATMRobotExtendLoaLSideEnable = ParseDoNode("doATMRobotExtendLoaLSideEnable", node, ioModule); //20220713
            _doATMRobotExtendLoaRSideEnable = ParseDoNode("doATMRobotExtendLoaRSideEnable", node, ioModule); //20220713


            _doRectorAATMTransferReady = ParseDoNode("doRectorAATMTransferReady", node, ioModule);
            _doRectorAProcessTransferReady= ParseDoNode("doRectorAProcessTransferReady", node, ioModule);
            //_doPm1LidClosed = ParseDoNode("doPm1LidClosed", node, ioModule);

            _doUnLoadPurgeRoutineRunning= ParseDoNode("doUnLoadPurgeRoutineRunning", node, ioModule);
            _doUnLoadLeakCheckRoutineRunning = ParseDoNode("doUnLoadLeakCheckRoutineRunning", node, ioModule);
            _doUnLoadPumpDownRoutineRunning = ParseDoNode("doUnLoadPumpDownRoutineRunning", node, ioModule);
            _doUnLoadVentUpRoutineRunning = ParseDoNode("doUnLoadVentUpRoutineRunning", node, ioModule);

            _scLoadLockVacBasePressure = ParseScNode("LLVacuumPressureBase", node, ioModule, $"LoadLock.VacuumPressureBase");
            _scLoadLockAtmBasePressure = ParseScNode("LLVacuumPressureBase", node, ioModule, $"LoadLock.AtmPressureBase");


            DATA.Subscribe($"{Module}.DiTMUnderVac", () => _diTMUnderVac);

        }

        public bool SetRobotExtenLLEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVacRobotExtenLLEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doVacRobotExtenLLEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetRobotExtendBufferEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVacRobotExtendBufferEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doVacRobotExtendBufferEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetRobotExtendPMAEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVacRobotExtendPMAEnable.Check(eValue, out reason))
            {
                return false;
            }          
            if (eValue && !_doRectorAATMTransferReady.Value && !_doRectorAProcessTransferReady.Value)
            {
                reason = "Can not set [DO-21]DO_VacRobotExtendPM1Enable to true \r\n reason:[DO-64]DO_RectorAATMTransferReady is true or [DO-65]DO_RectorAProcessTransferReady is true";
                return false;
            }
            if (!_doVacRobotExtendPMAEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetRobotExtendUnLoadEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVacRobotExtendUnloadEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doVacRobotExtendUnloadEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }
        public bool SetRobotExtendPMBEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doVacRobotExtendPMBEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doVacRobotExtendPMBEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetWaferRobotExtendLoadEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doATMRobotExtendLoaLSideEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doATMRobotExtendLoaLSideEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }
        public bool SetWaferRobotExtendUnLoadEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doATMRobotExtendUnloadEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doATMRobotExtendUnloadEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }
        public bool SetTrayRobotExtendLoadEnable(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doATMRobotExtendLoaRSideEnable.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doATMRobotExtendLoaRSideEnable.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }




        public bool SetTMPurgeRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doTmCyclePurgeRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doTmCyclePurgeRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetTMVentRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doTmVentUpRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doTmVentUpRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetTMPumpRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doTmPumpDownRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doTmPumpDownRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetTMLeakCheckRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doTmLeakCheckRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doTmLeakCheckRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetLLPurgeRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doLLCyclePurgeRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doLLCyclePurgeRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetLLVentRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doLLVentUpRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doLLVentUpRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetLLPumpRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doLLPumpDownRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doLLPumpDownRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetLLLeakCheckRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doLLLeakCheckRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doLLLeakCheckRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }




        public bool SetUnloadPurgeRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doUnLoadPurgeRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doUnLoadPurgeRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetUnloadVentRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doUnLoadVentUpRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doUnLoadVentUpRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetUnloadPumpRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doUnLoadPumpDownRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doUnLoadPumpDownRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool SetUnloadLeakCheckRoutineRunning(bool eValue, out string reason)
        {
            reason = String.Empty;

            if (!_doUnLoadLeakCheckRoutineRunning.Check(eValue, out reason))
            {
                return false;
            }
            if (!_doUnLoadLeakCheckRoutineRunning.SetValue(eValue, out reason))
            {
                return false;
            }

            return true;
        }

        public bool Initialize()
        {
            return true;
            //throw new NotImplementedException();
        }

        public void Monitor()
        {
            //_doLLAtProcessPress.Value = _aiLLPressure != null && _aiLLPressure.FloatValue <= _scLoadLockVacBasePressure.DoubleValue;

        }

        public void Reset()
        {
            //throw new NotImplementedException();
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }
    }
}
