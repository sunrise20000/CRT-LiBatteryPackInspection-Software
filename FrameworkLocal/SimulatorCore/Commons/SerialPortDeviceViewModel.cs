using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SerialPortDeviceViewModel : TimerViewModelBase
    {
        public ObservableCollection<string> PortList { get; set; }

        public ICommand ClearLogCommand { get; set; }
        public ICommand EnableCommand { get; set; }
        public ICommand DisableCommand { get; set; }

        public bool IsEnableEnable
        {
            get { return !_simulator.IsEnabled; }
        }
        public bool IsEnableDisable
        {
            get { return _simulator.IsEnabled; }
        }

        public string RemoteConnection
        {
            get { return _simulator.RemoteConnection; }
        }

        public string LocalPort
        {
            get { return _simulator.PortName; }
        }
        public bool IsConnected
        {
            get { return _simulator.IsConnected; }
        }

        public string ConnectionStatus
        {
            get
            {
                if (_simulator.IsEnabled)
                    return "Receiving";

                return "Disable";
            }
        }

        public string LocalPortSetPoint { get; set; }

        public ObservableCollection<TransactionLogItem> TransactionLogItems { get; set; }

        protected SerialPortDeviceSimulator _simulator;

        public SerialPortDeviceViewModel(string name) : base(name)
        {
            ClearLogCommand = new DelegateCommand<string>(ClearLog);
            EnableCommand = new DelegateCommand<string>(Enable);
            DisableCommand = new DelegateCommand<string>(Disable);

            TransactionLogItems = new ObservableCollection<TransactionLogItem>();

            LocalPortSetPoint = "COM2";

            PortList = new ObservableCollection<string>()
            {
                "COM1", "COM2", "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8", "COM9", "COM10",
                "COM11", "COM12", "COM13", "COM14", "COM15",
                "COM16", "COM17", "COM18", "COM19", "COM20",
                "COM21", "COM22", "COM23", "COM24", "COM25",
            };
        }

        protected void Init(SerialPortDeviceSimulator sim, bool enable = true)
        {
            LocalPortSetPoint = sim.PortName;
            InvokePropertyChanged("LocalPortSetPoint");

            _simulator = sim;
            _simulator.MessageOut += _simulator_MessageOut;
            _simulator.MessageIn += _simulator_MessageIn;

            if (enable)
                _simulator.Enable();
        }

        private void Disable(string obj)
        {
            _simulator.Disable();
        }

        private void Enable(string obj)
        {
            _simulator.PortName = LocalPortSetPoint;

            _simulator.Enable();
        }

        private void ClearLog(string obj)
        {
            TransactionLogItems.Clear();
        }

        private void _simulator_MessageIn(string obj)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                TransactionLogItems.Add(new TransactionLogItem() { Incoming = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                while (TransactionLogItems.Count > 100)
                {
                    TransactionLogItems.RemoveAt(0);
                }
            }));
        }

        private void _simulator_MessageOut(string obj)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                TransactionLogItems.Add(new TransactionLogItem() { Outgoing = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });
                while (TransactionLogItems.Count > 100)
                {
                    TransactionLogItems.RemoveAt(0);
                }
            }));
        }

        protected override void Poll()
        {
            if(Application.Current != null)  
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    InvokePropertyChanged();
                }));
            }
        }
    }
}
