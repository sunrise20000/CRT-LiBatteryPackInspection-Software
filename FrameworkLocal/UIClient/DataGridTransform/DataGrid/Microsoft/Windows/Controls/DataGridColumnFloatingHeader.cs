//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

using MS.Internal;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// The control which would be used to indicate the drag during column header drag-drop
    /// </summary>
    [TemplatePart(Name = "PART_VisualBrushCanvas", Type = typeof(Canvas))]
    internal class DataGridColumnFloatingHeader : Control
    {
        #region Constructors

        static DataGridColumnFloatingHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EGC.DataGridColumnFloatingHeader), new FrameworkPropertyMetadata(typeof(EGC.DataGridColumnFloatingHeader)));
            WidthProperty.OverrideMetadata(
                typeof(EGC.DataGridColumnFloatingHeader), 
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnWidthChanged), new CoerceValueCallback(OnCoerceWidth)));

            HeightProperty.OverrideMetadata(
                typeof(EGC.DataGridColumnFloatingHeader), 
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnHeightChanged), new CoerceValueCallback(OnCoerceHeight)));
        }

        #endregion

        #region Static Methods

        private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumnFloatingHeader header = (EGC.DataGridColumnFloatingHeader)d;
            double width = (double)e.NewValue;
            if (header._visualBrushCanvas != null && !DoubleUtil.IsNaN(width))
            {
                VisualBrush brush = header._visualBrushCanvas.Background as VisualBrush;
                if (brush != null)
                {
                    Rect viewBox = brush.Viewbox;
                    brush.Viewbox = new Rect(viewBox.X, viewBox.Y, width - header.GetVisualCanvasMarginX(), viewBox.Height);
                }
            }
        }

        private static object OnCoerceWidth(DependencyObject d, object baseValue)
        {
            double width = (double)baseValue;
            EGC.DataGridColumnFloatingHeader header = (EGC.DataGridColumnFloatingHeader)d;
            if (header._referenceHeader != null && DoubleUtil.IsNaN(width))
            {
                return header._referenceHeader.ActualWidth + header.GetVisualCanvasMarginX();
            }

            return baseValue;
        }

        private static void OnHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumnFloatingHeader header = (EGC.DataGridColumnFloatingHeader)d;
            double height = (double)e.NewValue;
            if (header._visualBrushCanvas != null && !DoubleUtil.IsNaN(height))
            {
                VisualBrush brush = header._visualBrushCanvas.Background as VisualBrush;
                if (brush != null)
                {
                    Rect viewBox = brush.Viewbox;
                    brush.Viewbox = new Rect(viewBox.X, viewBox.Y, viewBox.Width, height - header.GetVisualCanvasMarginY());
                }
            }
        }

        private static object OnCoerceHeight(DependencyObject d, object baseValue)
        {
            double height = (double)baseValue;
            EGC.DataGridColumnFloatingHeader header = (EGC.DataGridColumnFloatingHeader)d;
            if (header._referenceHeader != null && DoubleUtil.IsNaN(height))
            {
                return header._referenceHeader.ActualHeight + header.GetVisualCanvasMarginY();
            }

            return baseValue;
        }

        #endregion

        #region Methods and Properties

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _visualBrushCanvas = GetTemplateChild(VisualBrushCanvasTemplateName) as Canvas;
            UpdateVisualBrush();
        }

        internal EGC.DataGridColumnHeader ReferenceHeader
        {
            get
            {
                return _referenceHeader;
            }

            set
            {
                _referenceHeader = value;
            }
        }

        private void UpdateVisualBrush()
        {
            if (_referenceHeader != null && _visualBrushCanvas != null)
            {
                VisualBrush visualBrush = new VisualBrush(_referenceHeader);

                visualBrush.ViewboxUnits = BrushMappingMode.Absolute;

                double width = Width;
                if (DoubleUtil.IsNaN(width))
                {
                    width = _referenceHeader.ActualWidth;
                }
                else
                {
                    width = width - GetVisualCanvasMarginX();
                }

                double height = Height;
                if (DoubleUtil.IsNaN(height))
                {
                    height = _referenceHeader.ActualHeight;
                }
                else
                {
                    height = height - GetVisualCanvasMarginY();
                }

                Vector offset = VisualTreeHelper.GetOffset(_referenceHeader);
                visualBrush.Viewbox = new Rect(offset.X, offset.Y, width, height);

                _visualBrushCanvas.Background = visualBrush;
            }
        }

        internal void ClearHeader()
        {
            _referenceHeader = null;
            if (_visualBrushCanvas != null)
            {
                _visualBrushCanvas.Background = null;
            }
        }

        private double GetVisualCanvasMarginX()
        {
            double delta = 0;
            if (_visualBrushCanvas != null)
            {
                Thickness margin = _visualBrushCanvas.Margin;
                delta += margin.Left;
                delta += margin.Right;
            }

            return delta;
        }

        private double GetVisualCanvasMarginY()
        {
            double delta = 0;
            if (_visualBrushCanvas != null)
            {
                Thickness margin = _visualBrushCanvas.Margin;
                delta += margin.Top;
                delta += margin.Bottom;
            }

            return delta;
        }

        #endregion

        #region Data

        private EGC.DataGridColumnHeader _referenceHeader;
        private const string VisualBrushCanvasTemplateName = "PART_VisualBrushCanvas";
        private Canvas _visualBrushCanvas;

        #endregion
    }
}
