using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Caliburn.Micro;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SicUI.Models.PMs
{
    public class PMMfcDynamicFlowViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {

        public void SaveMFCFlow()
        {
            string setStr = "";
            int flow0 ,flow1, flow2, flow3, flow4, flow5, flow6, flow7, flow8, flow9;
            int time0, time1, time2, time3, time4, time5, time6, time7, time8, time9;
           
            Int32.TryParse(MFC290, out flow0);
            Int32.TryParse(MFC291, out flow1);
            Int32.TryParse(MFC292, out flow2);
            Int32.TryParse(MFC293, out flow3);
            Int32.TryParse(MFC294, out flow4);
            Int32.TryParse(MFC295, out flow5);
            Int32.TryParse(MFC296, out flow6);
            Int32.TryParse(MFC297, out flow7);
            Int32.TryParse(MFC298, out flow8);
            Int32.TryParse(MFC299, out flow9);

            Int32.TryParse(MFCTime290, out time0);
            Int32.TryParse(MFCTime291, out time1);
            Int32.TryParse(MFCTime292, out time2);
            Int32.TryParse(MFCTime293, out time3);
            Int32.TryParse(MFCTime294, out time4);
            Int32.TryParse(MFCTime295, out time5);
            Int32.TryParse(MFCTime296, out time6);
            Int32.TryParse(MFCTime297, out time7);
            Int32.TryParse(MFCTime298, out time8);
            Int32.TryParse(MFCTime299, out time9);

            setStr = String.Format("{0}*{1},{2}*{3},{4}*{5},{6}*{7},{8}*{9},{10}*{11},{12}*{13},{14}*{15},{16}*{17},{18}*{19}",
                    flow0, time0, flow1, time1, flow2, time2, flow3, time3, flow4, time4,
                    flow5, time5, flow6, time6, flow7, time7, flow8, time8, flow9, time9);

           InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"PM.{SystemName}.{SelectedMFC}DynamicFlow", setStr);
        }

        private void ShowMFCFlow()
        {
            string configStr = "";

           configStr = QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.{SelectedMFC}DynamicFlow").ToString();

            string[] array = configStr.Split(',');
            if (array.Length >= 10)
            {
                string[] mfcDetail0 = array[0].Split('*');
                if (mfcDetail0.Length == 2)
                {
                    MFC290 = mfcDetail0[0];
                    MFCTime290 = mfcDetail0[1];
                }
                string[] mfcDetail1 = array[1].Split('*');
                if (mfcDetail1.Length == 2)
                {
                    MFC291 = mfcDetail1[0];
                    MFCTime291 = mfcDetail1[1];
                }
                string[] mfcDetail2 = array[2].Split('*');
                if (mfcDetail2.Length == 2)
                {
                    MFC292 = mfcDetail2[0];
                    MFCTime292 = mfcDetail2[1];
                }
                string[] mfcDetail3 = array[3].Split('*');
                if (mfcDetail3.Length == 2)
                {
                    MFC293 = mfcDetail3[0];
                    MFCTime293 = mfcDetail3[1];
                }
                string[] mfcDetail4 = array[4].Split('*');
                if (mfcDetail4.Length == 2)
                {
                    MFC294 = mfcDetail4[0];
                    MFCTime294 = mfcDetail4[1];
                }
                string[] mfcDetail5 = array[5].Split('*');
                if (mfcDetail5.Length == 2)
                {
                    MFC295 = mfcDetail5[0];
                    MFCTime295 = mfcDetail5[1];
                }
                string[] mfcDetail6 = array[6].Split('*');
                if (mfcDetail6.Length == 2)
                {
                    MFC296 = mfcDetail6[0];
                    MFCTime296 = mfcDetail6[1];
                }
                string[] mfcDetail7 = array[7].Split('*');
                if (mfcDetail7.Length == 2)
                {
                    MFC297 = mfcDetail7[0];
                    MFCTime297 = mfcDetail7[1];
                }
                string[] mfcDetail8 = array[8].Split('*');
                if (mfcDetail8.Length == 2)
                {
                    MFC298 = mfcDetail8[0];
                    MFCTime298 = mfcDetail8[1];
                }
                string[] mfcDetail9 = array[9].Split('*');
                if (mfcDetail9.Length == 2)
                {
                    MFC299 = mfcDetail9[0];
                    MFCTime299 = mfcDetail9[1];
                }
            }
        }


        private List<string> _MFCGroup = new List<string>() { "MFC28", "MFC29", "MFC31","MFC40" };
        public List<string> MFCGroup
        {
            get { return _MFCGroup; }
            set { _MFCGroup = value; NotifyOfPropertyChange("MFCGroup"); }
        }

        string _selectMfc;
        public string SelectedMFC
        {
            get 
            {
                return _selectMfc;
            }
            set
            {
                _selectMfc = value;
                ShowMFCFlow();
            }
        }

        private string _mfc290;
        public string MFC290
        {
            get { return _mfc290; }
            set
            {
                _mfc290 = value;
                NotifyOfPropertyChange("MFC290");
            }
        }

        private string _mfc291;
        public string MFC291
        {
            get { return _mfc291; }
            set
            {
                _mfc291 = value;
                NotifyOfPropertyChange("MFC291");
            }
        }

        private string _mfc292;
        public string MFC292
        {
            get { return _mfc292; }
            set
            {
                _mfc292 = value;
                NotifyOfPropertyChange("MFC292");
            }
        }


        private string _mfc293;
        public string MFC293
        {
            get { return _mfc293; }
            set
            {
                _mfc293 = value;
                NotifyOfPropertyChange("MFC293");
            }
        }


        private string _mfc294;
        public string MFC294
        {
            get { return _mfc294; }
            set
            {
                _mfc294 = value;
                NotifyOfPropertyChange("MFC294");
            }
        }


        private string _mfc295;
        public string MFC295
        {
            get { return _mfc295; }
            set
            {
                _mfc295 = value;
                NotifyOfPropertyChange("MFC295");
            }
        }


        private string _mfc296;
        public string MFC296
        {
            get { return _mfc296; }
            set
            {
                _mfc296 = value;
                NotifyOfPropertyChange("MFC296");
            }
        }

        private string _mfc297;
        public string MFC297
        {
            get { return _mfc297; }
            set
            {
                _mfc297 = value;
                NotifyOfPropertyChange("MFC297");
            }
        }


        private string _mfc298;
        public string MFC298
        {
            get { return _mfc298; }
            set
            {
                _mfc298 = value;
                NotifyOfPropertyChange("MFC298");
            }
        }


        private string _mfc299;
        public string MFC299
        {
            get { return _mfc299; }
            set
            {
                _mfc299= value;
                NotifyOfPropertyChange("MFC299");
            }
        }

        private string _mfcTime290;
        public string MFCTime290
        {
            get { return _mfcTime290; }
            set
            {
                _mfcTime290 = value;
                NotifyOfPropertyChange("MFCTime290");
            }
        }

        private string _mfcTime291;
        public string MFCTime291
        {
            get { return _mfcTime291; }
            set
            {
                _mfcTime291 = value;
                NotifyOfPropertyChange("MFCTime291");
            }
        }


        private string _mfcTime292;
        public string MFCTime292
        {
            get { return _mfcTime292; }
            set
            {
                _mfcTime292 = value;
                NotifyOfPropertyChange("MFCTime292");
            }
        }


        private string _mfcTime293;
        public string MFCTime293
        {
            get { return _mfcTime293; }
            set
            {
                _mfcTime293 = value;
                NotifyOfPropertyChange("MFCTime293");
            }
        }


        private string _mfcTime294;
        public string MFCTime294
        {
            get { return _mfcTime294; }
            set
            {
                _mfcTime294 = value;
                NotifyOfPropertyChange("MFCTime294");
            }
        }


        private string _mfcTime295;
        public string MFCTime295
        {
            get { return _mfcTime295; }
            set
            {
                _mfcTime295 = value;
                NotifyOfPropertyChange("MFCTime295");
            }
        }


        private string _mfcTime296;
        public string MFCTime296
        {
            get { return _mfcTime296; }
            set
            {
                _mfcTime296 = value;
                NotifyOfPropertyChange("MFCTime296");
            }
        }


        private string _mfcTime297;
        public string MFCTime297
        {
            get { return _mfcTime297; }
            set
            {
                _mfcTime297 = value;
                NotifyOfPropertyChange("MFCTime297");
            }
        }


        private string _mfcTime298;
        public string MFCTime298
        {
            get { return _mfcTime298; }
            set
            {
                _mfcTime298 = value;
                NotifyOfPropertyChange("MFCTime298");
            }
        }


        private string _mfcTime299;
        public string MFCTime299
        {
            get { return _mfcTime299; }
            set
            {
                _mfcTime299 = value;
                NotifyOfPropertyChange("MFCTime299");
            }
        }

    }
}
