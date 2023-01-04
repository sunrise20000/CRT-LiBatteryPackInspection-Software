using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Sevors.NAIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Sevors
{
    class NAISSevorViewModel : SerialPortDeviceViewModel
    {
        private NAISSevor _reader;

        public string ResultValue { get; set; }
        //public bool IsSetting
        //{
        //    get
        //    {
        //        return _reader.IsSetting;
        //    }
        //    set
        //    {
        //        _reader.IsSetting = value;
        //    }
        //}
        //public bool IsActual
        //{
        //    get
        //    {
        //        return _reader.IsActual;
        //    }
        //    set
        //    {
        //        _reader.IsActual = value;
        //    }
        //}
        //public int ActualTemp
        //{
        //    get
        //    {
        //        return _reader.ActualTemp;
        //    }
        //    set
        //    {
        //        _reader.ActualTemp = value;
        //    }
        //}
        //public int SettingTemp
        //{
        //    get
        //    {
        //        return _reader.SettingTemp;
        //    }
        //    set
        //    {
        //        _reader.SettingTemp = value;
        //    }
        //}
        public NAISSevorViewModel(string port) : base("NAISSevorViewModel")
        {
            _reader = new NAISSevor(port);
            Init(_reader, true);
            _reader.receiveMsg += _reader_receiveMsg;
        }

        private void _reader_receiveMsg(string obj)
        {
            ResultValue = obj;
        }
    }
}
