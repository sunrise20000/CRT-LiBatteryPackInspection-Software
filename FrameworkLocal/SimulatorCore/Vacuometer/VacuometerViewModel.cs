using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Vacuometer
{
    class VacuometerViewModel : SerialPortDeviceViewModel
    {
        private Vacuometers _reader;

        public string ResultValue { get; set; }
        public bool IsSent
        {
            get
            {
                return _reader.IsSent;
            }
            set
            {
                _reader.IsSent = value;
            }
        }
        public VacuometerViewModel(string port) : base("VacuometerViewModel")
        {
            _reader = new Vacuometers(port);
            Init(_reader, true);
            _reader.receiveMsg += _reader_receiveMsg;


        }

        private void _reader_receiveMsg(string obj)
        {
            ResultValue = obj;
        }
    }
}
