using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Buffers;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;

namespace Mainframe.Buffers
{
    public class SicBuffer : Buffer
    {
        private ModuleName _module;
        private IoSensor _sensorAtm;
        private IoSensor _sensorVacuum;

        public SicBuffer(string module, XmlElement node, string ioModule = ""): base(module)
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _module = ModuleHelper.Converter(Module);
        }


        public override bool Initialize()
        {
            //_sensorAtm = DEVICE.GetDevice<IoSensor>("SensorIsAtmBuffer");
            //_sensorVacuum = DEVICE.GetDevice<IoSensor>("SensorIsVacBuffer");
            //_bufferPressure=DEVICE.GetDevice<IoPressureMeter3>("TM.BufferPressure");

            return base.Initialize();
        }
    }
}
