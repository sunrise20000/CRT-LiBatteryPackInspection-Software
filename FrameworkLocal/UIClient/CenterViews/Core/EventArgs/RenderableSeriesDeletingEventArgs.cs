using System.Collections.Generic;
using System.ComponentModel;
using SciChart.Charting.Visuals.RenderableSeries;

namespace MECF.Framework.UI.Client.CenterViews.Core.EventArgs
{
    public class RenderableSeriesDeletingEventArgs : CancelEventArgs
    {
        #region Constructors

        public RenderableSeriesDeletingEventArgs(IEnumerable<IRenderableSeries> seriesCollection)
        {
            Source = new List<IRenderableSeries>(seriesCollection);
        }

        #endregion

        #region Properties

        public List<IRenderableSeries> Source { get; }

        #endregion
    }
}
