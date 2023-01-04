/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\DataLogs\Core\SicFastLineDataSeries.cs
* @author Su Liang
* @Date 2022-08-02
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details Cross-Thread operation allowed.
*
* *****************************************************************************/

using System;
using SciChart.Charting.Model.DataSeries;

namespace MECF.Framework.UI.Client.CenterViews.Core.Charting
{
    public class SicFastLineDataSeries : XyDataSeries<DateTime, double>
    {
        #region Properties

        public double Factor { get; set; }

        public double Offset { get; set; }

        #endregion
    }
}
