using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory
{
    //public class ChartParameter : PropertyChangedBase
    //{
    //    public bool Visible { get; set; }
    //    public string DataSource { get; set; }
    //    public string DataVariable { get; set; }     
    //    public string Factor { get; set; }
    //    public string Offset { get; set; }
    //    public string LineWidth { get; set; }

    //    private Brush _Color;
    //    public Brush Color
    //    {
    //        get { return _Color; }
    //        set { _Color = value; NotifyOfPropertyChange("Color"); }
    //    }

    //    public ParameterNode RelatedNode { get; set; }

    //}

    public class RecipeItem : PropertyChangedBase
    {
        #region Variables

        private bool _selected = false;

        #endregion


        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyOfPropertyChange(nameof(Selected));
            }
        }

        public string Recipe { get; set; }
        public string LotID { get; set; }

        public string SlotID { get; set; }

        public string CjName { get; set; }

        public string PjName { get; set; }
        public string Chamber { get; set; }
        public string Status { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}
