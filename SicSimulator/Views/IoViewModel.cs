using System.Collections.ObjectModel;
using Aitex.Core.UI.MVVM;
using SicSimulator.Instances;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Simulator.Core.IoProviders;

namespace SicSimulator.Views
{
    public class IoViewModel  : TimerViewModelBase
    {
        public SimulatorModulePlc PlcModule  { get; set; }
        public SimulatorIO PlcSystem { get; set; }

        public ObservableCollection<NotifiableIoItem> DIList { get; set; }
        public ObservableCollection<NotifiableIoItem> DOList { get; set; }
        public ObservableCollection<NotifiableIoItem> AIList { get; set; }
        public ObservableCollection<NotifiableIoItem> AOList { get; set; }

        public IoViewModel(int port, string source, string ioMapPathFile, string module) : base(nameof(IoViewModel))
        {
            if (string.IsNullOrEmpty(module) || module == "TM")
            {
                PlcSystem = new SimulatorIO(port, source, ioMapPathFile);

                DIList = PlcSystem.DiItemList;
                DOList = PlcSystem.DoItemList;
                AIList = PlcSystem.AiItemList;
                AOList = PlcSystem.AoItemList;
            }
            else
            {
                PlcModule = new SimulatorModulePlc(port, source, ioMapPathFile, module);

                DIList = PlcModule.DiItemList;
                DOList = PlcModule.DoItemList;
                AIList = PlcModule.AiItemList;
                AOList = PlcModule.AoItemList;
            }

        }


        protected override void Poll()
        {


        }
    }
}
