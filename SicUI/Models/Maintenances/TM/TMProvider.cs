using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.ClientBase.ServiceProvider;

namespace SicUI.Client.Models.Platform.TM
{
    public class TMProvider : IProvider
    {
        private static TMProvider _Instance = null;
        public static TMProvider Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new TMProvider();

                return _Instance;
            }
        }

        public void Create()
        {

        }

        public void ServoToLL(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.ServoPressure");
        }

        public void Pump(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Pump");
        }

        public void Vent(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Vent");
        }

        public void Purge(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Purge");
        }

        public void LiftUp(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.MoveLift", 0);
        }

        public void LiftDown(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.MoveLift", 1);
        }

        public void OpenDoor(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.OpenDoor");
        }

        public void CloseDoor(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.CloseDoor");
        }

        public void OpenSlitValve(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"TM.OpenSlitValve", module);
        }

        public void CloseSlitValve(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"TM.CloseSlitValve", module);
        }
        public void Abort(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Abort");
        }

        public void Home(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Home");
        }

        public void Reset(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Reset");
        }
        #region TM Robot

        public void TMRobot_Retract(string module, int slot, int blade)
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.Retract", module, slot, blade);
        }

        public void TMRobot_Extend(string module, int slot, int blade)
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.Extend", module, slot, blade);
        }
 

        internal void TMRobot_Home()
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.Home");
        }

        internal void TMRobot_Pick(string pickSelectedModule, int slot, int blade)
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.Pick", pickSelectedModule, slot, blade);
        }

        internal void TMRobot_Place(string placeSelectedModule, int slot, int blade)
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.Place", placeSelectedModule, slot, blade);
        }

        internal void TMRobot_Goto(string placeSelectedModule, int slot, int blade,string R,string Z)
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.Goto", placeSelectedModule, slot, blade, R,Z);
        }

        internal void PrepareTransfer(string module, string type)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.PrepareTransfer", type);
        }

        internal void TransferHandoff(string module, string type)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.TransferHandoff", type);
        }
        #endregion
    }
}
