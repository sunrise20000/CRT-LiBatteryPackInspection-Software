using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.IOCore;
using System.Xml;
using Mainframe.Devices;

namespace Mainframe.Feeder
{
    public class CrtDevFeeder : BaseDevice, IDevice
    {

        #region Variables

        private IoInterLock _ioInterlock;

        #endregion

        #region Constructors

        public CrtDevFeeder(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");

            // 解析当前模组关联的IO
            DIBattMon = ParseDiNode(nameof(DIBattMon), node, ioModule);
            DIBattBlock = ParseDiNode(nameof(DIBattBlock), node, ioModule);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 返回电池来料传感器。
        /// </summary>
        public DIAccessor DIBattMon { get;  }
        
        /// <summary>
        /// 返回电池到达挡停位置传感器。
        /// </summary>
        public DIAccessor DIBattBlock { get;  }

        #endregion

        #region Methods

        private void InitData()
        {
            /*DATA.Subscribe($"{Name}.IsAtm", () => { return CheckAtm(); });
            DATA.Subscribe($"{Name}.IsVacuum", () => { return CheckVacuum(); });
            DATA.Subscribe($"{Name}.ChamberPressure", () => ChamberPressure);
            DATA.Subscribe($"{Name}.ForelinePressure", () => ForelinePressure);*/
        }

        public bool Initialize()
        {
            _ioInterlock = DEVICE.GetDevice<IoInterLock>("IoInterLock");
            
            InitData();
            return true;
        }
        

        public void Monitor()
        {
            Debug.WriteLine($"{Module}.{nameof(DIBattMon)} = {DIBattMon.Value}");
        }

        public void Terminate()
        {
            
        }

        public void Reset()
        {
            
        }

        #endregion
    }
}
