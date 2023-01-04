using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS
{
    #region Message
    public class ITAUPSMessage : MessageBase
    {
        public string Oid;

        public string Value;

        public Dictionary<string, string> oidic;
    }
    #endregion

    #region Connection
    public class ITAUPSConnection : SNMPBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        private string sendMessage;
        public ITAUPSConnection(string portName) : base(portName, "public", 1, "\n")
        {

        }
        public override bool SendMessage(string message)
        {
            _lstCacheBuffer.Clear();
            sendMessage = message;
            return base.SendMessage(message);
        }
        public override bool SendMessage(List<string> message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(string rawMessage)
        {
            //ITAUPSMessage msg = new ITAUPSMessage();
            //msg.IsResponse = false;
            //msg.IsAck = false;
            //msg.IsComplete = false;
                
            //if (sendMessage.Equals(rawMessage)) return msg;

            //msg.Oid = rawMessage;
            //msg.IsResponse = true;
            //msg.IsAck = true;
            //msg.IsComplete = true;

            return null;
        }
        protected override MessageBase ParseResponse(string Oid, string Value)
        {

            ITAUPSMessage msg = new ITAUPSMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;

            if (!sendMessage.Equals(Oid)) return msg;
            msg.Oid = Oid;
            msg.Value = Value;
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
        protected override MessageBase ParseResponse(Dictionary<string, string> rawMessagelist)
        {
            ITAUPSMessage msg = new ITAUPSMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;

            //if (!) return msg;
            //msg.Oid = Oid;
            // msg.Value = Value;
            if (rawMessagelist != null && rawMessagelist.Count == 0)
            {
                return msg;
            }
            msg.oidic = rawMessagelist;
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;
            return msg;
        }   
    }

    #endregion

    #region Handler
    public abstract class ITAUPSHandler : HandlerBase
    {
        public ITAUPS Device { get; }

        protected ITAUPSHandler(ITAUPS device, string Oid)
            : base(Oid)
        {
            Device = device;
        }
        protected ITAUPSHandler(ITAUPS device, List<string> Oid)
            : base(Oid)
        {
            Device = device;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as ITAUPSMessage;
            ResponseMessage = msg;
            handled = true;
            return true;
        }    
    }
    public class ITAUPSGetHandler : ITAUPSHandler
    {
        public ITAUPSGetHandler(ITAUPS device,string name,string messageType, string Oid)
            : base(device, Oid)
        {
            Name = name;
            MessageType = messageType;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as ITAUPSMessage;
            handled = false;
            if (!result.IsResponse) return true;

            //if (Name.Contains("Status"))                    
            //{   
            //    if  (result.Value=="0")
            //        Device.SystemStatus = "Normal";
            //    if (result.Value == "1")
            //        Device.SystemStatus = "Warning";
            //    if (result.Value == "2")
            //        Device.SystemStatus = "Critical";
            //}
            //try
            //{
            //    if (Name.Contains("InputVoltage"))
            //        Device.InputVoltage = Convert.ToSingle(result.Value) / 100;
            //    if (Name.Contains("BatteryVoltage"))
            //        Device.BatteryVoltage = Convert.ToSingle(result.Value) / 100;
            //    if (Name.Contains("BatteryRemainsTime"))
            //        Device.BatteryRemainsTime = Convert.ToInt32(result.Value);
            //}catch(Exception ex)
            //{
            //    LOG.Write(ex.Message);
            //}
            //if (Name.Contains("BatteryRemainsTime"))
            //    ResponseMessage = msg;

            try
            {

                foreach (var dic in result.oidic)
                {
                    var oid = Device.Oids.FirstOrDefault(m => m.Value.Substring(1) == dic.Key);
                    if (oid.Key == null)
                    {
                        continue;
                    }
                    if (oid.Key.Contains("SystemStatus"))
                    {
                        Device.PraseSystemStatus(dic.Value);
                    }
                    if (oid.Key.Contains("InputVoltage"))
                    {
                        Device.InputVoltage = Convert.ToSingle(dic.Value);
                    }
                    if (oid.Key.Contains("BatteryVoltage"))
                    {
                        Device.BatteryVoltage = Convert.ToSingle(dic.Value);
                    }
                    if (oid.Key.Contains("BatteryRemainsTime"))
                    {
                        Device.BatteryRemainsTime = Convert.ToInt32(dic.Value);
                    }
                    if (oid.Key.Contains("upsOutputSource"))
                    {
                        Device.ParseOutputSource(dic.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
            Device._connectTimes = 0;
            handled = true;
            Thread.Sleep(1000);
            return true;
        }
    }
    public class ITAUPSGetListHandler : ITAUPSHandler
    {
        public ITAUPSGetListHandler(ITAUPS device, string name, string messageType, List<string> Oid)
            : base(device, Oid)
        {
            Name = name;
            MessageType = messageType;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as ITAUPSMessage;
            handled = false;
            if (!result.IsResponse) return true;
            try
            {
                foreach (var dic in result.oidic)
                {
                    var oid = Device.Oids.First(m => m.Value == dic.Key);
                    if (oid.Key.Contains("Status"))
                        Device.SystemStatus = dic.Value;
                    if (oid.Key.Contains("InputVoltage"))
                        Device.InputVoltage = Convert.ToSingle(dic.Value);
                    if (oid.Key.Contains("BatteryVoltage"))
                        Device.BatteryVoltage = Convert.ToSingle(dic.Value);
                    if (oid.Key.Contains("BatteryRemainsTime"))
                        Device.BatteryRemainsTime = Convert.ToInt32(dic.Value);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
            Device._connectTimes = 0;
            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(500);
            return true;
        }
    }
    public class ITAUPSGetNextHandler : ITAUPSHandler
    {
        public ITAUPSGetNextHandler(ITAUPS device, string name, string messageType, string Oid)
            : base(device, Oid)
        {
            Name = name;
            MessageType = messageType;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {

            var result = msg as ITAUPSMessage;
            if (result.oidic == null)
            {
                handled = true;
                Device.UtilityPowerAlarm(false);
                Device.BatteryUnderVoltage(false);
                return true;
            }
            handled = false;
            if (!result.IsResponse) return true;

            bool alarm18 = false;
            bool alarm26 = false;
            try
            {
                foreach (var dic in result.oidic)
                {
                    if (dic.Value == "18")
                        alarm18 = true;
                    if (dic.Value == "26")
                        alarm26 = true;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
            Device.UtilityPowerAlarm(alarm18);
            Device.BatteryUnderVoltage(alarm26);
            Device._connectTimes = 0;
            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(500);
            return true;
        }
    }
      public class ITAUPSGetBulkHandler : ITAUPSHandler
      {
        public ITAUPSGetBulkHandler(ITAUPS device, string name, string messageType, string Oid)
            : base(device, Oid)
        {
            Name = name;
            MessageType = messageType;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
           
            var result = msg as ITAUPSMessage;
            if (result.oidic == null)
            {
                handled = true;
                Device.UtilityPowerAlarm(false);
                Device.BatteryUnderVoltage(false);
                return true;
            }
            handled = false;
            if (!result.IsResponse) return true;

            bool alarm18 = false;
            bool alarm26 = false;
            try
            {
                foreach (var dic in result.oidic)
                {
                    if (!dic.Key.Contains("1.3.6.1.4.1.13400.2.26.3.1.6")) continue;
                    
                    if (dic.Value == "18")
                        alarm18 = true;
                    if (dic.Value == "26")
                        alarm26 = true;
                }
            }catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
            Device.UtilityPowerAlarm(alarm18);
            Device.BatteryUnderVoltage(alarm26);
            Device._connectTimes = 0;
            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(500);
            return true;
        }
    }

    #endregion
}