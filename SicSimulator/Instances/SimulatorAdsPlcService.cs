using Aitex.Core.RT.IOCore;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.PLC;

namespace SicSimulator.Instances
{
    public class SimulatorAdsPlcService : IWcfPlcService
    {

        //diVariable=""  doVariable=""  aiVariable=""  aoVariable=""
        public bool CheckIsConnected()
        {
            return true;
        }

        public bool Read(string variable, out object data, string type, int length, out string reason)
        {
            reason = string.Empty;
            data = null;

            switch (variable)
            {
                case "GVL_IO.PM1_DI_G":
                    data = IoManager.Instance.GetDiBuffer("PM1.PLC")[0];
                    break;
                case "GVL_IO.PM1_DO_G":
                    data = IoManager.Instance.GetDoBuffer("PM1.PLC")[0];
                    break;
                case "GVL_IO.PM1_AI_G":
                    data = IoManager.Instance.GetAiBufferFloat("PM1.PLC")[0];
                    break;
                case "GVL_IO.PM1_AO_G":
                    data = IoManager.Instance.GetAoBufferFloat("PM1.PLC")[0];
                    break;

                case "GVL_IO.PM2_DI_G":
                    data = IoManager.Instance.GetDiBuffer("PM2.PLC")[0];
                    break;
                case "GVL_IO.PM2_DO_G":
                    data = IoManager.Instance.GetDoBuffer("PM2.PLC")[0];
                    break;
                case "GVL_IO.PM2_AI_G":
                    data = IoManager.Instance.GetAiBufferFloat("PM2.PLC")[0];
                    break;
                case "GVL_IO.PM2_AO_G":
                    data = IoManager.Instance.GetAoBufferFloat("PM2.PLC")[0];
                    break;
            }

            return true;
        }

        public bool WriteArrayElement(string variable, int index, object value, out string reason)
        {
            reason = string.Empty;
            switch (variable)
            {
                case "GVL_IO.PM1_DI_G":
                    break;
                case "GVL_IO.PM1_DO_G":
                    IoManager.Instance.GetDoBuffer("PM1.PLC")[0][index] = (bool)value;
                    break;
                case "GVL_IO.PM1_AI_G":
                    break;
                case "GVL_IO.PM1_AO_G":
                    IoManager.Instance.GetAoBufferFloat("PM1.PLC")[0][index] = (float)value;
                    break;

                case "GVL_IO.PM2_DI_G":
                    break;
                case "GVL_IO.PM2_DO_G":
                    IoManager.Instance.GetDoBuffer("PM2.PLC")[0][index] = (bool)value;
                    break;
                case "GVL_IO.PM2_AI_G":
                    break;
                case "GVL_IO.PM2_AO_G":
                    IoManager.Instance.GetAoBufferFloat("PM2.PLC")[0][index] = (float)value;
                    break;
            }
            return true;
        }

        public bool[] ReadDi(int offset, int size, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public float[] ReadAiFloat(int offset, int size, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public int[] ReadAiInt(int offset, int size, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public bool WriteDo(int offset, bool[] buffer, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public bool WriteAoFloat(int offset, float[] buffer, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public bool WriteAoInt(int offset, int[] buffer, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public int Heartbeat(int counter)
        {
            return counter;
        }
 
    }
}