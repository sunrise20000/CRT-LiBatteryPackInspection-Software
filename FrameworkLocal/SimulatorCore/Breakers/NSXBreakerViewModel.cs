using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Breakers
{
    class NSXBreakerViewModel : SerialPortDeviceViewModel
    {
        private NSXBreaker _reader;

        public string ResultValue { get; set; }
        public bool IsACurrent
        {
            get
            {
                return _reader.IsACurrent;
            }
            set
            {
                _reader.IsACurrent = value;
            }
        }
        public bool IsBCurrent
        {
            get
            {
                return _reader.IsBCurrent;
            }
            set
            {
                _reader.IsBCurrent = value;
            }
        }

        public bool IsCCurrent
        {
            get
            {
                return _reader.IsCCurrent;
            }
            set
            {
                _reader.IsCCurrent = value;
            }
        }

        public bool IsAActive
        {
            get
            {
                return _reader.IsAActive;
            }
            set
            {
                _reader.IsAActive = value;
            }
        }
        public bool IsBActive
        {
            get
            {
                return _reader.IsBActive;
            }
            set
            {
                _reader.IsBActive = value;
            }
        }

        public bool IsCActive
        {
            get
            {
                return _reader.IsCActive;
            }
            set
            {
                _reader.IsCActive = value;
            }

        }
        public bool IsAReactive
        {
            get
            {
                return _reader.IsAReactive;
            }
            set
            {
                _reader.IsAReactive = value;
            }
        }
        public bool IsBReactive
        {
            get
            {
                return _reader.IsBReactive;
            }
            set
            {
                _reader.IsBReactive = value;
            }
        }

        public bool IsCReactive
        {
            get
            {
                return _reader.IsCReactive;
            }
            set
            {
                _reader.IsCReactive = value;
            }
        }

        public int APhaseCurrent
        {
            get
            {
                return _reader.APhaseCurrent;
            }
            set
            {
                _reader.APhaseCurrent = value;
            }
        }
        public int BPhaseCurrent
        {
            get
            {
                return _reader.BPhaseCurrent;
            }
            set
            {
                _reader.BPhaseCurrent = value;
            }
        }
        public int CPhaseCurrent
        {
            get
            {
                return _reader.CPhaseCurrent;
            }
            set
            {
                _reader.CPhaseCurrent = value;
            }
        }
        public int AActivePower
        {
            get
            {
                return _reader.AActivePower;
            }
            set
            {
                _reader.AActivePower = value;
            }
        }
        public int BActivePower
        {
            get
            {
                return _reader.BActivePower;
            }
            set
            {
                _reader.BActivePower = value;
            }
        }
        public int CActivePower
        {
            get
            {
                return _reader.CActivePower;
            }
            set
            {
                _reader.CActivePower = value;
            }
        }
        public int AReactivePower
        {
            get
            {
                return _reader.AReactivePower;
            }
            set
            {
                _reader.AReactivePower = value;
            }
        }
        public int BReactivePower
        {
            get
            {
                return _reader.BReactivePower;
            }
            set
            {
                _reader.BReactivePower = value;
            }
        }
        public int CReactivePower
        {
            get
            {
                return _reader.CReactivePower;
            }
            set
            {
                _reader.CReactivePower = value;
            }
        }
        public NSXBreakerViewModel(string port) : base("NSXBreakerViewModel")
        {
            _reader = new NSXBreaker(port);
            Init(_reader, true);
            _reader.receiveMsg += _reader_receiveMsg;


        }

        private void _reader_receiveMsg(string obj)
        {
            ResultValue = obj;
        }
    }
}
