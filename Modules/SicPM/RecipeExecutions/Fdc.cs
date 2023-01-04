using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.SCCore;

namespace SicPM.RecipeExecutions
{
    public class Fdc
    {
        public List<FdcDataItem> DataList
        {
            get
            {
                return _lstItems;
            }
        }

        private List<FdcDataItem> _lstItems = new List<FdcDataItem>();

        private PeriodicJob _monitorThread;

        private string _module;

        private Stopwatch _delaytimer = new Stopwatch();

        private int _delayTime;

        private Dictionary<string, string> _setpointDataMap = new Dictionary<string, string>();

        public Fdc(string module )
        {
            _monitorThread = new PeriodicJob(300, OnMonitor, "fdc thread");

            _module = module;
        }

        public void Reset()
        {
            _lstItems.Clear();
            _setpointDataMap.Clear();


            string groupName = SC.GetStringValue("System.FDC.DataGroupName");
            if (string.IsNullOrEmpty(groupName))
                groupName = "Process";

            int interval = SC.GetValue<int>("System.FDC.SampleInterval");
            if (interval < 50)
                interval = 50;

            _delayTime = SC.GetValue<int>("System.FDC.DelayTime");
            if (_delayTime < 0)
                _delayTime = 10;

            var content = TypedConfigManager.Instance.GetTypedConfigContent("DataGroup", "");

            XmlDocument xmlContent = new XmlDocument();
            xmlContent.LoadXml(content);

            var items = xmlContent.SelectNodes($"DataGroupConfig/DataGroup[@name='{groupName}']/DataItem");
 
            foreach (var item in items)
            {
                var node = item as XmlElement;

                string name = node.GetAttribute("name");

                if (!string.IsNullOrEmpty(name) && name.StartsWith($"{_module}.")  && (_lstItems.FirstOrDefault(x=>x.Name==name)==null))
                {
                    var dataType = Singleton<DataManager>.Instance.GetDataType(name);
                    if (dataType == typeof(double) || dataType == typeof(float) ||
                        dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short))
                    {
                        _lstItems.Add(new FdcDataItem()
                        {
                            Name = name,
                        });
                    }

                }
            }



            var fdcMap = xmlContent.SelectNodes($"DataGroupConfig/DataGroup[@name='Fdc']/DataItem");

            foreach (var item in fdcMap)
            {
                var node = item as XmlElement;

                string name = node.GetAttribute("name");

                if (!string.IsNullOrEmpty(name) && name.StartsWith($"{_module}.") && (_lstItems.FirstOrDefault(x => x.Name == name) != null))
                {
                   string controlName = node.GetAttribute("control_name");

                   if (!string.IsNullOrEmpty(controlName))
                    _setpointDataMap[name] = controlName;
                }
            }


            _monitorThread.ChangeInterval(interval);
        }

        //pair: controlname - setpoint value
        public void Start(Dictionary<string, string> setpointControlData)
        {
            ClearPreviousData();

            foreach (var fdcDataItem in _lstItems)
            {
                if (!_setpointDataMap.ContainsKey(fdcDataItem.Name))
                    continue;
                
                if (!setpointControlData.ContainsKey(_setpointDataMap[fdcDataItem.Name]))
                    continue;

                if (!float.TryParse(setpointControlData[_setpointDataMap[fdcDataItem.Name]], out float floatValue))
                    continue;

                fdcDataItem.SetPoint = floatValue;
            }

            _delaytimer.Restart();

            _monitorThread.Start();
        }

        public void Stop()
        {
            _monitorThread.Pause();

            ClearPreviousData();
        }

        public void ClearPreviousData()
        {
            foreach (var fdcDataItem in _lstItems)
            {
                fdcDataItem.Clear();
            }
        }

        private bool OnMonitor()
        {
            try
            {
                if (_delaytimer.IsRunning)
                {
                    if (_delaytimer.ElapsedMilliseconds < _delayTime)
                        return true;
                    else
                    {
                        _delaytimer.Stop();
                    }
                }

                foreach (var fdcDataItem in _lstItems)
                {
                    var objValue = DATA.Poll(fdcDataItem.Name);
                    float floatValue = 0f;
                    if (objValue != null)
                    {
                        float.TryParse(objValue.ToString(), out floatValue);
                    }
                     
                    fdcDataItem.Update(floatValue);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                
            }

            return true;
        }

    }
}
