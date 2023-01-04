using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Pumps.DRYVacuum
{
    class DRYVacuumPumpViewModel : SerialPortDeviceViewModel
    {
        private DRYVacuumPump _reader;

        public string ResultValue { get; set; }

        public bool IsRunS
        {
            get
            {
                return _reader.IsRunS;
            }
            set
            {
                _reader.IsRunS = value;
            }
        }
        public bool IsMPS
        {
            get
            {
                return _reader.IsMPS;
            }
            set
            {
                _reader.IsMPS = value;
            }
        }
        public bool IsBPS
        {
            get
            {
                return _reader.IsBPS;
            }
            set
            {
                _reader.IsBPS = value;
            }
        }
        public bool IsAlarmRondom
        {
            get
            {
                return _reader.IsAlarmRondom;
            }
            set
            {
                _reader.IsAlarmRondom = value;
            }
        }
        public bool IsAlarm
        {
            get
            {
                return _reader.IsAlarm;
            }
            set
            {
                _reader.IsAlarm = value;
            }
        }

        public DRYVacuumPumpViewModel(string port) : base("DRYVacuumPumpViewModel")
        {
            _reader = new DRYVacuumPump(port);
            Init(_reader, true);
            _reader.receiveMsg += _reader_receiveMsg;


        }

        private void _reader_receiveMsg(string obj)
        {
            ResultValue = obj;
        }
    }
}
