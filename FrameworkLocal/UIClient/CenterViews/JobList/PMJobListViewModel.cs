using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.ClientBase;
using System;
using System.Collections.ObjectModel;
using System.Data;
namespace MECF.Framework.UI.Client.CenterViews.JobList
{
    public class JobListItem : NotifiableItem
    {
        public string RecipeName { get; set; }
        public string LotName { get; set; }
        public string Status { get; set; }
        public string StartTime { get; set; }
        public string ProcessBeginTime { get; set; }
        public string ProcessEndTime { get; set; }
        public string EndTime { get; set; }
    }

    public class PMJobListViewModel : BaseModel
    {
        public ObservableCollection<JobListItem> JobListData { get; set; }
        private PMJobListView view;

        public PMJobListViewModel()
        {
            this.DisplayName = "JobList";
            JobListData = new ObservableCollection<JobListItem>();
            QueryData();
        }

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            view = (PMJobListView)_view;
            this.view.wfStartTime.Value = DateTime.Now.AddHours(-24);
            this.view.wfEndTime.Value = DateTime.Now;
        }

        public void QueryData()
        {
            JobListData.Clear();
            DateTime dtStartTime = DateTime.Now.AddHours(-24);
            DateTime dtEndTime = DateTime.Now;

            if (this.view != null && this.view.wfStartTime.Value != null && this.view.wfEndTime.Value != null)
            {
                dtStartTime = this.view.wfStartTime.Value;
                dtEndTime = this.view.wfEndTime.Value;
            }

            string sql = String.Format(@"SELECT   a.recipe_name,lot_name, status,start_time, process_begin_time, process_end_time,end_time
	                FROM public.autojob_data a left join process_data b on a.wafer_guid=b.wafer_data_guid where start_time>='{0}' and (end_time<='{1}'  or end_time is null) order by start_time desc;",
                    dtStartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    dtEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    JobListItem item = new JobListItem();

                    item.RecipeName = dbData.Rows[i]["recipe_name"].ToString();
                    item.LotName = dbData.Rows[i]["lot_name"].ToString();
                    item.Status = dbData.Rows[i]["status"].ToString();
                    item.StartTime = dbData.Rows[i]["start_time"].ToString();
                    item.ProcessBeginTime = dbData.Rows[i]["process_begin_time"].ToString();
                    item.ProcessEndTime = dbData.Rows[i]["process_end_time"].ToString();
                    item.EndTime = dbData.Rows[i]["end_time"].ToString();

                    JobListData.Add(item);
                }
            }
        }
    }
}
