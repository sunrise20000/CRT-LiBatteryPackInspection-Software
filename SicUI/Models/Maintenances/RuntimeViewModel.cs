using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicUI.Models.Maintenances
{
    public class RuntimeItem : NotifiableItem
    {
        public string Guid { get; set; }
        public string DeviceName { get; set; }
        public string SetValue { get; set; }
        public string ElapseValue { get; set; }
        public string IsPM { get; set; }
    }


    public class RuntimeViewModel : SicUIViewModelBase
    {
        public RuntimeViewModel()
        {
            RuntimeData = new ObservableCollection<RuntimeItem>();
        }

        public ObservableCollection<RuntimeItem> RuntimeData { get; set; }
        public string DeviceNameSet { get; set; }
        public string DeviceValueSet { get; set; }

        protected override void OnActivate()
        {
            base.OnActivate();
            AddNew("PM1 Process", 0, true);
            GetData();
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            base.InvokeAfterUpdateProperty(data);
        }

        public void GetData()
        {
            RuntimeData.Clear();

            string sql = $"SELECT * FROM \"runtime_data\" order by \"ispm\" DESC,guid;";
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    RuntimeItem item = new RuntimeItem();

                    item.Guid = dbData.Rows[i]["guid"].ToString();
                    item.DeviceName = dbData.Rows[i]["device_name"].ToString();
                    item.SetValue = Convert.ToDouble(dbData.Rows[i]["set_minutes"].ToString()).ToString("0.00");
                    item.ElapseValue = (Convert.ToDouble(dbData.Rows[i]["elapse_minutes"].ToString())/60).ToString("0.00");
                    item.IsPM = dbData.Rows[i]["ispm"].ToString();

                    RuntimeData.Add(item);
                }
            }
        }

        public void Add()
        {
            double sValue = 0;
            if (!String.IsNullOrEmpty(DeviceNameSet) && Double.TryParse( DeviceValueSet,out sValue))
            {
                AddNew(DeviceNameSet, sValue, false);
                GetData();
            }
        }

        public void Reset(RuntimeItem item)
        {
            if (item != null)
            {
                ResetByGuid(item.Guid);
                GetData();
            }            
        }

        public void Delete(RuntimeItem item)
        {
            if (item != null)
            {
                DeleteByGuid(item.Guid);
                GetData();
            }
        }

        public void Update(RuntimeItem item)
        {
            if (item != null)
            {
                UpdateByGuid(item.Guid, item.SetValue);
                GetData();
            }
        }

        public void AddNew(string devicename,double valueSet, bool ispm)
        {
            string sql0 = $"Update \"runtime_data\" set  \"device_name\"='PM1 Process' where \"device_name\" ='PM1';";
            QueryDataClient.Instance.Service.QueryData(sql0);


            string sql = $"SELECT * FROM \"runtime_data\" where \"device_name\" ='{devicename}';";
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);
            if (dbData != null && dbData.Rows.Count > 0)
            {
                return;
            }

            if (devicename == ModuleName.PM1.ToString() || devicename == ModuleName.PM2.ToString() || devicename == ModuleName.PMA.ToString() || devicename == ModuleName.PMB.ToString())
            {
                ispm = true;
            }

            sql = string.Format("INSERT INTO \"runtime_data\" (\"guid\", \"device_name\", \"set_minutes\" , \"elapse_minutes\", \"ispm\" )VALUES ('{0}', '{1}', {2}, {3}, {4});",
                Guid.NewGuid(),devicename, valueSet.ToString("0.00"), 0,ispm);
            QueryDataClient.Instance.Service.QueryData(sql);
        }

        public void DeleteByGuid(string guid)
        {
            string sql = string.Format("Delete from \"runtime_data\" where \"guid\"='{0}' and ispm=false;", guid);
            QueryDataClient.Instance.Service.QueryData(sql);
        }

        public void ResetByGuid(string guid)
        {
            string sql = string.Format("Update \"runtime_data\" set \"elapse_minutes\"='0' where \"guid\"='{0}';", guid);
            QueryDataClient.Instance.Service.QueryData(sql);
        }

        public void UpdateByGuid(string guid,string valueSet)
        {
            string sql = string.Format("Update \"runtime_data\" set \"set_minutes\"='{0}' where \"guid\"='{1}';", (Convert.ToDouble(valueSet)).ToString("0.00"), guid);
            QueryDataClient.Instance.Service.QueryData(sql);
        }
    }   
}
