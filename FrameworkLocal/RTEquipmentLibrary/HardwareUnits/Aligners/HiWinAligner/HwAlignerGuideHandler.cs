using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner
{
    public abstract class HwAlignerGuideHandler : HandlerBase
    {
        public HwAlignerGuide Device { get; }

        protected HwAlignerGuideHandler(HwAlignerGuide device, string commandvalue)
        : base(BuildMessage(commandvalue))
        {
            Device = device;
        }

        private static byte[] BuildMessage(string commandvalue)
        {
            List<byte> liCmd = Encoding.ASCII.GetBytes(commandvalue).ToList<byte>();
            liCmd.Add(0x0d); //CR
            liCmd.Add(0x0a); //LF 不可颠倒          

            return liCmd.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwAlignerGuideMessage;

            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    ////===================================================================================
    //public class HwAlignerGuideQueryHandler : HwAlignerGuideHandler
    //{
    //    //public HwAlignerGuideQueryHandler(HwAlignerGuide device, string name, byte groupAddress, byte functionCode, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
    //    //    : base(device, new byte[] { groupAddress, functionCode, offerHigh, offerLow, dataHigh, dataLow })
    //    //{
    //    //    Name = name;
    //    //}
    //    public HwAlignerGuideQueryHandler(HwAlignerGuide device, string name, byte[] commandvalue)
    //       : base(device, commandvalue)
    //    {
    //        Name = name;
    //        device.ClearMsg();
    //    }

    //    public override bool HandleMessage(MessageBase msg, out bool handled)
    //    {
    //        var result = msg as HwAlignerGuideMessage;
    //        handled = false;
    //        if (!result.IsResponse) return true;
    //        //if (result.FunctionCode==0x01)
    //        //{
    //        //    Device.PraseData(Name,result.Data);
    //        //}
    //        //
    //        Device.PraseData(Name, result.Data);
    //        //
    //        Device._connecteTimes = 0;
    //        ResponseMessage = msg;
    //        handled = true;
    //        return true;
    //    }
    //}
    //public class HwAlignerGuideSetHandler : HwAlignerGuideHandler
    //{
    //    public int iIfMe = 12;
    //    //public HwAlignerGuideSetHandler(HwAlignerGuide device, string name, byte groupAddress,byte functionCode, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
    //    //    : base(device, new byte[] { groupAddress, functionCode, offerHigh, offerLow, dataHigh, dataLow })       
    //    //{
    //    //    Name =name;
    //    //}
    //    public HwAlignerGuideSetHandler(HwAlignerGuide device, string name, byte[] commandvalue)
    //      : base(device, commandvalue)
    //    {
    //        Name = name;
    //        device.ClearMsg();
    //    }

    //    public override bool HandleMessage(MessageBase msg, out bool handled)
    //    {
    //        var result = msg as HwAlignerGuideMessage;
    //        handled = false;
    //        //if (!result.IsResponse)
    //        //{
    //        //    return true;
    //        //}
    //        //if (Name == "Clear Error On")
    //        //{

    //        //}
    //        //
    //        handled = Device.PraseData(Name, result.Data);
    //        //
    //        ResponseMessage = msg;
    //        //handled = true;
    //        return true;
    //    }
    //}


    public class HwAlignerGuideSetAHandler : HwAlignerGuideHandler
    {
        public HwAlignerGuideSetAHandler(HwAlignerGuide device, string name, string commandvalue, int timeout = 30)
          : base(device, commandvalue)
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
            Name = name;
            device.SetCurrentOper();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwAlignerGuideMessage;
            if (!result.IsResponse)
            {
                handled = true;
                return false;
            }

            handled = Device.PraseDataA(Name, result.Data,out bool returnIfo);
            return returnIfo;
        }
    }

    public class HwAlignerWaferCheckHandler : HwAlignerGuideHandler
    {
        public HwAlignerWaferCheckHandler(HwAlignerGuide device, string name, string commandvalue, int timeout = 100)
          : base(device, commandvalue)
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
            Name = name;
            device.HaveWafer = false;
            device.SetCurrentOper();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HwAlignerGuideMessage;
            if (!result.IsResponse)
            {
                handled = true;
                return false;
            }

            handled = Device.CheckWaferDataA(Name, result.Data, out bool returnIfo);
            return returnIfo;
        }
    }
}
