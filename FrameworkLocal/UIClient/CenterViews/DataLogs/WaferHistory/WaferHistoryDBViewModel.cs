using Aitex.Sorter.Common;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase.Command;
using SciChart.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory
{
    public class WaferHistoryDBViewModel : BaseModel
    {
        public WaferHistoryDBViewModel()
        {
            SelectionChangedCommand = new BaseCommand<WaferHistoryItem>(SelectionChanged);
            QueryCommand = new BaseCommand<object>(QueryLots);
            ToChartCommand = new BaseCommand<object>(GoToChart);
            HistoryData = new ObservableCollection<LazyTreeItem<WaferHistoryItem>>();

            InitTime();
        }

        private ObservableCollection<LazyTreeItem<WaferHistoryItem>> _historyData;
        public ObservableCollection<LazyTreeItem<WaferHistoryItem>> HistoryData
        {
            get { return _historyData; }
            set
            {
                _historyData = value;
                NotifyOfPropertyChange("HistoryData");
            }
        }

        private WaferHistoryItem _selectedItem;
        public WaferHistoryItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                NotifyOfPropertyChange("SelectedItem");
            }
        }

        private ObservableCollection<WaferHistoryLot> _lots = new ObservableCollection<WaferHistoryLot>();
        public ObservableCollection<WaferHistoryLot> Lots
        {
            get { return _lots; }
            set
            {
                _lots = value;
                NotifyOfPropertyChange("Lots");
            }
        }


        private ObservableCollection<WaferHistoryWafer> _wafers = new ObservableCollection<WaferHistoryWafer>();
        public ObservableCollection<WaferHistoryWafer> Wafers
        {
            get { return _wafers; }
            set
            {
                _wafers = value;
                NotifyOfPropertyChange("Wafers");
            }
        }

        private ObservableCollection<WaferHistoryMovement> _movements = new ObservableCollection<WaferHistoryMovement>();
        public ObservableCollection<WaferHistoryMovement> Movements
        {
            get { return _movements; }
            set
            {
                _movements = value;
                NotifyOfPropertyChange("Movements");
            }
        }

        private WaferHistoryRecipe _recipe;
        public WaferHistoryRecipe Recipe
        {
            get { return _recipe; }
            set
            {
                _recipe = value;
                NotifyOfPropertyChange("Recipe");
            }
        }


        public ObservableCollection<WaferHistoryRecipe> _recipes = new ObservableCollection<WaferHistoryRecipe>();
        public ObservableCollection<WaferHistoryRecipe> Recipes
        {
            get { return _recipes; }
            set
            {
                _recipes = value;
                NotifyOfPropertyChange("Recipes");
            }
        }
        private DateTime _searchBeginTime;
        public DateTime SearchBeginTime
        {
            get { return _searchBeginTime; }
            set
            {
                _searchBeginTime = value;
                NotifyOfPropertyChange("SearchBeginTime");
            }
        }

        private DateTime _searchEndTime;
        public DateTime SearchEndTime
        {
            get { return _searchEndTime; }
            set
            {
                _searchEndTime = value;
                NotifyOfPropertyChange("SearchEndTime");
            }
        }

        private string keyWord;
        private WaferHistoryDBView view;

        public string KeyWord
        {
            get { return keyWord; }
            set
            {
                keyWord = value;
                NotifyOfPropertyChange("KeyWord");
            }
        }
        public ICommand SelectionChangedCommand { get; set; }

        public ICommand QueryCommand { get; set; }

        public ICommand ToChartCommand { get; set; }

        void InitTime()
        {
            SearchBeginTime = DateTime.Now.Date;
            SearchEndTime = DateTime.Now.Date.AddDays(1).Date;
        }


        void QueryLots(object e)
        {
            this.SearchBeginTime = this.view.wfTimeFrom.Value;
            this.SearchEndTime = this.view.wfTimeTo.Value;

            if (SearchBeginTime > SearchEndTime)
            {
                MessageBox.Show("Time range invalid, start time should be early than end time");
                return;
            }

            Lots = new ObservableCollection<WaferHistoryLot>(QueryLot(SearchBeginTime, SearchEndTime, KeyWord));//.OrderByDescending(lot => lot.StartTime).ToArray();

            HistoryData.Clear();
            var lotsItem = new WaferHistoryItem() { Name = "Lots", };
            var root = new LazyTreeItem<WaferHistoryItem>(lotsItem, x => LoadSubItem(x));
            root.SubItems = new ObservableCollection<LazyTreeItem<WaferHistoryItem>>(Lots.Select(x => new LazyTreeItem<WaferHistoryItem>(new WaferHistoryItem() { ID = x.ID, Name = x.Name, StartTime = x.StartTime, EndTime = x.EndTime, Type = WaferHistoryItemType.Lot }, LoadSubItem)));
            root.IsExpanded = true;
            HistoryData.Add(root);

            SelectedItem = lotsItem;
        }
        /// <summary>
        /// Notify chart page to prepare datas
        /// </summary>
        /// <param name="o"></param>
        void GoToChart(object o)
        {
            WaferHistoryRecipe chartQuery = null;

            if (o is string name)
            {
                var query = _recipes.FirstOrDefault(t => t.Recipe == name);
                if (query is null) return;
                chartQuery = query;
            }
            if (o is WaferHistoryLot waferLot)
            {
                DateTime start = waferLot.StartTime;
                double.TryParse(waferLot.Duration, out double duration);
                DateTime end = start.AddSeconds(duration);
                chartQuery = new WaferHistoryRecipe() { StartTime = start, EndTime = end, /*Chamber = waferLot.CarrierID */};

            }
            else if (o is WaferHistoryWafer wafer)
            {
                chartQuery = new WaferHistoryRecipe() { StartTime = wafer.StartTime, EndTime = wafer.EndTime/*,Chamber = waferLot.CarrierID*/ };
            }

            //导航切换到chart页面
            BaseApp.Instance.SwitchPage("DataLog", "DataHistory", chartQuery);
        }

        public List<WaferHistoryLot> QueryLot(DateTime from, DateTime to, string key)

        {
            List<WaferHistoryLot> result = new List<WaferHistoryLot>();

            string sql = $"SELECT * FROM \"lot_data\" where \"start_time\" >= '{from:yyyy/MM/dd HH:mm:ss.fff}' and \"start_time\" <= '{to:yyyy/MM/dd HH:mm:ss.fff}' order by \"start_time\" ASC;";

            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryLot item = new WaferHistoryLot();

                    string name = dbData.Rows[i]["name"].ToString();
                    string time = "";
                    item.WaferCount = (int)dbData.Rows[i]["total_wafer_count"];
                    item.ID = dbData.Rows[i]["guid"].ToString();

                    if (!dbData.Rows[i]["start_time"].Equals(DBNull.Value))
                    {
                        item.StartTime = ((DateTime)dbData.Rows[i]["start_time"]);
                        time = item.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    if (!dbData.Rows[i]["end_time"].Equals(DBNull.Value))
                    {
                        item.EndTime = ((DateTime)dbData.Rows[i]["end_time"]);
                    }

                    item.Name = $"{name} - {time}";

                    result.Add(item);
                }
            }


            //}));

            return result;
        }

        public List<WaferHistoryWafer> QueryLotWafer(string lotGuid)
        {
            List<WaferHistoryWafer> result = new List<WaferHistoryWafer>();

            string sql = $"SELECT * FROM \"wafer_data\",\"lot_wafer_data\" where \"lot_wafer_data\".\"lot_data_guid\" = '{lotGuid}' and \"lot_wafer_data\".\"wafer_data_guid\" = \"wafer_data\".\"guid\" order by \"wafer_data\".\"create_time\" ASC;;";

            Wafers.Clear();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryWafer item = new WaferHistoryWafer();

                    item.ID = dbData.Rows[i]["guid"].ToString();
                    var itemLoadPort = dbData.Rows[i]["create_station"].ToString();
                    var itemSlot = dbData.Rows[i]["create_slot"].ToString();

                    //item.CarrierID = dbData.Rows[i]["rfid"].ToString();

                    //item.LotID = dbData.Rows[i]["lot_id"].ToString();
                    var itemWaferID = dbData.Rows[i]["wafer_id"].ToString();

                    item.Sequence = dbData.Rows[i]["sequence_name"].ToString();

                    item.Status = dbData.Rows[i]["process_status"].ToString();

                    if (!dbData.Rows[i]["create_time"].Equals(DBNull.Value))
                    {
                        item.StartTime = ((DateTime)dbData.Rows[i]["create_time"]);
                    }

                    if (!dbData.Rows[i]["delete_time"].Equals(DBNull.Value))
                        item.EndTime = ((DateTime)dbData.Rows[i]["delete_time"]);

                    item.Name = $"{itemLoadPort} - {itemSlot} - {itemWaferID}";

                    item.Type = WaferHistoryItemType.Wafer;

                    Wafers.Add(item);

                    result.Add(item);
                }
            }


            //}));

            return result;
        }


        public List<WaferHistoryRecipe> QueryWaferRecipe(string waferGuid)
        {
            List<WaferHistoryRecipe> result = new List<WaferHistoryRecipe>();

            string sql = $"SELECT * FROM \"process_data\" where \"wafer_data_guid\" = '{waferGuid}';";

            Recipes.Clear();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryRecipe item = new WaferHistoryRecipe();

                    item.ID = dbData.Rows[i]["guid"].ToString();
                    var itemName = dbData.Rows[i]["recipe_name"].ToString();

                    if (!dbData.Rows[i]["process_begin_time"].Equals(DBNull.Value))
                        item.StartTime = (DateTime)dbData.Rows[i]["process_begin_time"];

                    if (dbData.Rows[i].Table.Columns.Contains("process_end_time") && !dbData.Rows[i]["process_end_time"].Equals(DBNull.Value))
                        item.EndTime = (DateTime)dbData.Rows[i]["process_end_time"];

                    if (dbData.Rows[i].Table.Columns.Contains("recipe_setting_time") && !dbData.Rows[i]["recipe_setting_time"].Equals(DBNull.Value))
                        item.SettingTime = dbData.Rows[i]["recipe_setting_time"].ToString();

                    item.ActualTime = item.Duration;

                    item.Recipe = itemName;

                    item.Chamber = dbData.Rows[i]["process_in"].ToString();

                    item.Name = itemName;

                    item.Type = WaferHistoryItemType.Recipe;

                    Recipes.Add(item);

                    result.Add(item);
                }
            }


            //}));

            return result;
        }

        public List<WaferHistoryMovement> QueryWaferMovement(string waferGuid)
        {
            List<WaferHistoryMovement> result = new List<WaferHistoryMovement>();

            string sql = $"SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{waferGuid}' order by \"arrive_time\" ASC limit 1000;";

            Movements.Clear();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count - 1; i++)
                {
                    WaferHistoryMovement item = new WaferHistoryMovement();

                    item.Source = $"station : {dbData.Rows[i]["station"]} slot : {dbData.Rows[i]["slot"]}";
                    item.Destination = $"station : {dbData.Rows[i + 1]["station"]} slot : {dbData.Rows[i + 1]["slot"]}";
                    item.InTime = dbData.Rows[i]["arrive_time"].ToString();

                    result.Add(item);

                    Movements.Add(item);

                }
            }


            //}));

            return result;
        }

        public WaferHistoryRecipe QueryRecipe(string recipeGuid)
        {
            WaferHistoryRecipe result = new WaferHistoryRecipe();

            string sql = $"SELECT * FROM \"process_data\" where \"guid\" = '{recipeGuid}';";


            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryRecipe item = new WaferHistoryRecipe();

                    item.ID = dbData.Rows[i]["guid"].ToString();
                    var itemName = dbData.Rows[i]["recipe_name"].ToString();

                    if (!dbData.Rows[i]["process_begin_time"].Equals(DBNull.Value))
                        item.StartTime = (DateTime)dbData.Rows[i]["process_begin_time"];

                    if (!dbData.Rows[i]["process_end_time"].Equals(DBNull.Value))
                        item.EndTime = (DateTime)dbData.Rows[i]["process_end_time"];

                    if (dbData.Rows[i].Table.Columns.Contains("recipe_setting_time") && !dbData.Rows[i]["recipe_setting_time"].Equals(DBNull.Value))
                        item.SettingTime = dbData.Rows[i]["recipe_setting_time"].ToString();

                    item.ActualTime = item.Duration;

                    item.Recipe = itemName;

                    item.Chamber = dbData.Rows[i]["process_in"].ToString();

                    item.Name = itemName;

                    item.Type = WaferHistoryItemType.Recipe;

                    result = item;


                }
            }


            sql = $"SELECT * FROM \"recipe_step_data\" where \"process_data_guid\" = '{recipeGuid}' order by step_number ASC;";
            dbData = QueryDataClient.Instance.Service.QueryData(sql);

            List<RecipeStep> steps = new List<RecipeStep>();
            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    RecipeStep item = new RecipeStep();

                    item.No = int.Parse(dbData.Rows[i]["step_number"].ToString());
                    item.Name = dbData.Rows[i]["step_name"].ToString();

                    if (!dbData.Rows[i]["step_begin_time"].Equals(DBNull.Value))
                        item.StartTime = (DateTime)dbData.Rows[i]["step_begin_time"];

                    if (!dbData.Rows[i]["step_end_time"].Equals(DBNull.Value))
                        item.EndTime = (DateTime)dbData.Rows[i]["step_end_time"];

                    item.ActualTime = item.EndTime.CompareTo(item.StartTime) <= 0 ? "" : item.EndTime.Subtract(item.StartTime).TotalSeconds.ToString();

                    item.SettingTime = dbData.Rows[i]["step_time"].ToString();

                    steps.Add(item);
                }
            }


            sql = $"SELECT * FROM \"step_fdc_data\" where \"process_data_guid\" = '{recipeGuid}' order by step_number ASC;";
            dbData = QueryDataClient.Instance.Service.QueryData(sql);

            List<RecipeStepFdcData> fdcs = new List<RecipeStepFdcData>();
            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    RecipeStepFdcData item = new RecipeStepFdcData();

                    item.StepNumber = int.Parse(dbData.Rows[i]["step_number"].ToString());
                    item.Name = dbData.Rows[i]["parameter_name"].ToString();

                    if (!dbData.Rows[i]["sample_count"].Equals(DBNull.Value))
                        item.SampleCount = (int)dbData.Rows[i]["sample_count"];

                    if (!dbData.Rows[i]["setpoint"].Equals(DBNull.Value))
                        item.SetPoint = (float)dbData.Rows[i]["setpoint"];

                    if (!dbData.Rows[i]["min_value"].Equals(DBNull.Value))
                        item.MinValue = (float)dbData.Rows[i]["min_value"];

                    if (!dbData.Rows[i]["max_value"].Equals(DBNull.Value))
                        item.MaxValue = (float)dbData.Rows[i]["max_value"];

                    if (!dbData.Rows[i]["std_value"].Equals(DBNull.Value))
                        item.StdValue = (float)dbData.Rows[i]["std_value"];

                    if (!dbData.Rows[i]["mean_value"].Equals(DBNull.Value))
                        item.MeanValue = (float)dbData.Rows[i]["mean_value"];


                    fdcs.Add(item);
                }
            }

            result.Steps = steps;
            result.Fdcs = fdcs;

            Recipe = result;

            return result;
        }

        private void SelectionChanged(WaferHistoryItem item)
        {
            switch (item.Type)
            {
                case WaferHistoryItemType.Lot:

                    QueryLotWafer(item.ID);

                    break;
                case WaferHistoryItemType.Wafer:
                    QueryWaferRecipe(item.ID);
                    QueryWaferMovement(item.ID);

                    break;
                case WaferHistoryItemType.Recipe:

                    QueryRecipe(item.ID);

                    break;
                default:
                    break;
            }
            SelectedItem = item;
        }

        protected List<LazyTreeItem<WaferHistoryItem>> LoadSubItem(WaferHistoryItem item)
        {
            switch (item.Type)
            {
                case WaferHistoryItemType.Lot:
                    var wafers = QueryLotWafer(item.ID);
                    return wafers.Select(x => new LazyTreeItem<WaferHistoryItem>(x, y => LoadSubItem(y))).OrderBy(s => s.Data.StartTime).ToList();
                case WaferHistoryItemType.Wafer:
                    var recipes = QueryWaferRecipe(item.ID);
                    return recipes.Select(x => new LazyTreeItem<WaferHistoryItem>(x, y => LoadSubItem(y))).OrderBy(s => s.Data.StartTime).ToList();
                default:
                    break;
            }
            return new List<LazyTreeItem<WaferHistoryItem>> { };
        }


        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (WaferHistoryDBView)_view;
            this.view.wfTimeFrom.Value = this.SearchBeginTime;
            this.view.wfTimeTo.Value = this.SearchEndTime;

            QueryLots(new object());
            SelectionChanged(SelectedItem);
        }
    }


    public class LazyTreeItem<T> : INotifyPropertyChanged where T : ITreeItem<T>, new()
    {
        private LazyTreeItem<T> dummyChild;
        private T data;
        private Func<T, List<LazyTreeItem<T>>> loader;


        private LazyTreeItem()
        {
            data = new T();
            data.ID = Guid.NewGuid().ToString();
        }

        public LazyTreeItem(T data, Func<T, List<LazyTreeItem<T>>> loader)
        {
            this.data = data;
            this.loader = loader;

            dummyChild = new LazyTreeItem<T>();

            SubItems = new ObservableCollection<LazyTreeItem<T>>();
            SubItems.Add(dummyChild);
        }

        public T Data
        {
            get
            {
                return data;
            }
        }



        public ObservableCollection<LazyTreeItem<T>> SubItems
        {
            get; set;
        }

        private bool isExpanded;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public bool HasDummyChild
        {
            get { return SubItems.Count == 1 && SubItems.First().data.ID == dummyChild.data.ID; }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                if (HasDummyChild)
                {
                    SubItems.Remove(dummyChild);
                    var items = loader(data);
                    items.ForEachDo(x => SubItems.Add(x));
                }
            }
        }
    }


}
