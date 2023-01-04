using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Numerics;
using Sicentury.Core.Tree;
using DrawingColor = System.Drawing.Color;

namespace MECF.Framework.UI.Client.CenterViews.Core.Charting
{
    /// <summary>
    /// 需要显示在图表中的项目列表。
    /// </summary>
    public class ChartingLineSeriesCollection : ObservableCollection<IRenderableSeries>
    {
        #region Variables

        private Queue<Color> _colorQueue;
        private readonly Random _colorRandom;

        #endregion

        #region Constructors

        public ChartingLineSeriesCollection(string displayName)
        {
            _colorQueue = GetNewColorPatternQueue();
            _colorRandom = new Random();
            DisplayName = displayName;
        }

        #endregion

        #region Properties

        public string DisplayName { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 获取颜色列队。
        /// </summary>
        /// <returns></returns>
        private static Queue<Color> GetNewColorPatternQueue()
        {
            var colorList =  new List<DrawingColor>(new DrawingColor[]
            {
                DrawingColor.Red, DrawingColor.Orange, DrawingColor.Yellow, DrawingColor.Green, DrawingColor.Blue,
                DrawingColor.Pink, DrawingColor.Purple, DrawingColor.Aqua,
                DrawingColor.Bisque, DrawingColor.Brown, DrawingColor.BurlyWood, DrawingColor.CadetBlue,
                DrawingColor.CornflowerBlue, DrawingColor.DarkBlue, DrawingColor.DarkCyan, DrawingColor.DarkGray,
                DrawingColor.DarkGreen, DrawingColor.DarkKhaki,
                DrawingColor.DarkMagenta, DrawingColor.DarkOliveGreen, DrawingColor.DarkOrange,
                DrawingColor.DarkSeaGreen, DrawingColor.DarkSlateBlue, DrawingColor.DarkSlateGray,
                DrawingColor.DarkViolet, DrawingColor.DeepPink,
                DrawingColor.DeepSkyBlue, DrawingColor.DimGray, DrawingColor.DodgerBlue, DrawingColor.ForestGreen,
                DrawingColor.Gold,
                DrawingColor.Gray, DrawingColor.GreenYellow, DrawingColor.HotPink, DrawingColor.Indigo,
                DrawingColor.Khaki, DrawingColor.LightBlue,
                DrawingColor.LightCoral, DrawingColor.LightGreen, DrawingColor.LightPink, DrawingColor.LightSalmon,
                DrawingColor.LightSkyBlue,
                DrawingColor.LightSlateGray, DrawingColor.LightSteelBlue, DrawingColor.LimeGreen,
                DrawingColor.MediumOrchid, DrawingColor.MediumPurple,
                DrawingColor.MediumSeaGreen, DrawingColor.MediumSlateBlue, DrawingColor.MediumSpringGreen,
                DrawingColor.MediumTurquoise, DrawingColor.Moccasin, DrawingColor.NavajoWhite, DrawingColor.Olive,
                DrawingColor.OliveDrab, DrawingColor.OrangeRed,
                DrawingColor.Orchid, DrawingColor.PaleGoldenrod, DrawingColor.PaleGreen,
                DrawingColor.PeachPuff, DrawingColor.Peru, DrawingColor.Plum, DrawingColor.PowderBlue,
                DrawingColor.RosyBrown, DrawingColor.RoyalBlue,
                DrawingColor.SaddleBrown, DrawingColor.Salmon, DrawingColor.SeaGreen, DrawingColor.Sienna,
                DrawingColor.SkyBlue, DrawingColor.SlateBlue, DrawingColor.SlateGray, DrawingColor.SpringGreen,
                DrawingColor.Teal, DrawingColor.Aquamarine,
                DrawingColor.Tomato, DrawingColor.Turquoise, DrawingColor.Violet, DrawingColor.Wheat,
                DrawingColor.YellowGreen
            });

            return new Queue<Color>(colorList.Select(x => Color.FromRgb(x.R, x.G, x.B)));
        }

        /// <summary>
        /// 复位色板。
        /// </summary>
        /// <param name="isRandomColor"></param>
        private void ResetColorQueue(bool isRandomColor = false)
        {
            if (isRandomColor)
            {
                _colorQueue.Clear();
                for (var i = 0; i < 200; i++)
                {
                    _colorQueue.Enqueue(Color.FromRgb( 
                        (byte)_colorRandom.Next(0, 255),
                        (byte)_colorRandom.Next(0, 255),
                        (byte)_colorRandom.Next(0, 255)));
                }
            }
            else
                _colorQueue = GetNewColorPatternQueue();
        }

        /// <summary>
        /// 从色板中获取一个颜色。
        /// </summary>
        /// <returns></returns>
        private Color GetColor()
        {
            if (!_colorQueue.Any())
                ResetColorQueue();

            var dc = _colorQueue.Dequeue();
            return Color.FromRgb(dc.R, dc.G, dc.B);
        }

        /// <summary>
        /// 重置图表序列的颜色。
        /// </summary>
        public void ResetColors()
        {
            ResetColorQueue();
            foreach (var series in this)
            {
                series.Stroke = GetColor();
            }
        }

        /// <summary>
        /// 追加新的查询数据到列表。
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="antiAliasing"></param>
        /// <param name="resamplingMode"></param>
        /// <returns>成功追加的项目。</returns>
        public List<IRenderableSeries> Append(List<TreeNode> collection, bool antiAliasing = true,
            ResamplingMode resamplingMode = ResamplingMode.MinMax)
        {
            var existed = this.Select(x => ((SicFastLineSeries)x).BackendParameterNode);
            var toBeAdded = collection.Except(existed).ToList();

            foreach (var node in toBeAdded)
            {
                var line = new SicFastLineSeries(node.FullName)
                {
                    AntiAliasing = antiAliasing,
                    ResamplingMode = resamplingMode,
                    BackendParameterNode = node
                };

                node.ClearStatistic();

                Add(line);
            }

            // 清除本次追加的曲线数据，否则不可添加新数据点。
            var toBeCleaned = 
                this.Where(x =>
                collection.Select(t => t.FullName)
                    .Contains(x.DataSeries.SeriesName))
                    .Cast<SicFastLineSeries>()
                    .ToList();
            foreach (var line in toBeCleaned)
            {
                line.DataSeries.Clear();
                line.BackendParameterNode?.ClearStatistic();
            }

            return toBeCleaned.Cast<IRenderableSeries>().ToList();
        }

        /// <summary>
        /// 根据指定的<see cref="TreeNode"/>列表重新整理项目。
        /// <para>该方法提供一种快速更新列表的方法，尽量避免DataGrid刷新造成的UI卡顿问题。</para>
        /// </summary>a
        /// <param name="collection"></param>
        /// <param name="antiAliasing"></param>
        /// <param name="resamplingMode"></param>
        public void ReArrange(List<TreeNode> collection, bool antiAliasing = true,
            ResamplingMode resamplingMode = ResamplingMode.MinMax)
        {
            // 剔除本次没有选择但存在于列表中的项目。

            // 筛选本次选择并已存在于列表中的项目
            var toBeKept = this.Where(x =>
                collection.Select(t => t.FullName).Contains(x.DataSeries.SeriesName));

            // 删除本次没有选择的项目。
            var toBeRemoved = this.Except(toBeKept).ToList();
            foreach (var t in toBeRemoved)
                this.Remove(t);

            // 筛选未存在于列表中的ParameterNode
            var toBeAdded = collection
                .Except(collection.Where(x => this.Select(s => s.DataSeries.SeriesName).Contains(x.FullName))).ToList();

            foreach (var node in toBeAdded)
            {
                var line2D = new SicFastLineSeries(node.FullName)
                {
                    AntiAliasing = antiAliasing,
                    ResamplingMode = resamplingMode,
                    BackendParameterNode = node
                };
                Add(line2D);
            }

            this.ToList().ForEach(x =>
            {
                x.DataSeries.Clear();
                ((SicFastLineSeries)x)?.BackendParameterNode?.ClearStatistic();
            });
        }


        public void SetFifoParam()
        {
            var dataSeries = this.Select(x => x.DataSeries).ToList();
            
        }

        #endregion

        #region Overrided Methods


        protected override void InsertItem(int index, IRenderableSeries item)
        {
            if(item != null)
                item.Stroke = GetColor();

            base.InsertItem(index, item);
        }

        #endregion
    }
}
