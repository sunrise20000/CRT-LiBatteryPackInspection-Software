using System;
using System.ComponentModel;
using SciChart.Charting.Model.DataSeries;

namespace MECF.Framework.UI.Client.CenterViews.Core
{
    public class ParameterNodePoint : IPointMetadata
    {
        #region Variables

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        internal ParameterNodePoint(DateTime time, double value)
        {
            Time = time;
            Value = value;
        }

        #endregion

        #region Properties

        internal DateTime Time { get; }

        internal double Value { get; }

        public bool IsSelected { get; set; }

        #endregion
    }
}
