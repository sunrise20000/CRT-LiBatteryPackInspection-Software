using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Aligners
{
    public class CognexOcrReaderSimulator : SocketDeviceSimulator
    {
        Random _rd = new Random();

        public CognexOcrReaderSimulator()
            : base(23, -1, "\r\n", ' ')
        {

        }

        public CognexOcrReaderSimulator(int port)
            : base(port, -1, "\r\n", ' ')
        {

        }

        int GetSleepTime()
        {
            return 50;
        }
        private int _slotID = 0;
        private int GeneratorSlotID()
        {
            int ret;
            if (_slotID < 1)
            {
                _slotID = 1;

            }
            if (_slotID > 25)
            {
                _slotID = 1;

            }
            ret = _slotID;
            _slotID++;
            return ret;
        }

        protected override void ProcessUnsplitMessage(string msg)
        {
            List<string> result = new List<string>();
            var isMultipleWrite = false;
            switch (msg)
            {
                case "admin":
                    result.Add("Password:");
                    break;
                case "":
                    result.Add("User Logged In");
                    break;
                case "SM\"READ\"0 ":
                    // [PNGHE001MXC2,400.000,1.000]. 
                    isMultipleWrite = true;
                    Thread.Sleep(2000);
                    result.Add("1");
                    result.Add($"[ABCD{GeneratorSlotID():D2}EF,{1:F3}]");
                    //result.Add($"ABCDEF,{_rd.Next(300, 400):F3},{1:F3}]");
                    break;
                case "Get Filelist":
                    isMultipleWrite = true;
                    result.AddRange(GenerateFilelist());
                    break;
                default:
                    result.Add("1");
                    break;
            }

            if (isMultipleWrite)
            {
                Thread.Sleep(GetSleepTime());
                foreach (var res in result)
                {
                    Thread.Sleep(5);
                    OnWriteMessage(res);
                }
            }
            else
            {
                Task.Run(() =>
                {
                    Thread.Sleep(GetSleepTime());

                    OnWriteMessage(result.FirstOrDefault());
                });
            }
        }

        private static char[] constant =
        {
            '0','1','2','3','4','5','6','7','8','9',
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        };
        public static string GenerateRandomNumber(int Length)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(62);
            Random rd = new Random();
            for (int i = 0; i < Length; i++)
            {
                newRandom.Append(constant[rd.Next(62)]);
            }
            return newRandom.ToString();
        }

        public static List<string> GenerateFilelist()
        {
            var newRandom = new System.Text.StringBuilder(62);
            var rd = new Random();
            var quantity = 50;
            var listStr = new List<string>();
            listStr.Add("1");
            listStr.Add(quantity.ToString());
            for (var i = 0; i < quantity; i++)
            {
                for (int j = 0; j < rd.Next(2, 8); j++)
                {
                    newRandom.Append(constant[rd.Next(62)]);
                }
                listStr.Add(newRandom.Append(".job").ToString());
                newRandom.Clear();
            }

            return listStr;
        }
    }
}