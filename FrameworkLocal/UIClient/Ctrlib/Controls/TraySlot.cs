using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenSEMI.Ctrlib.Controls
{
    public delegate void TrayDragDropHandler(object sender, TrayDragDropEventArgs e);

    public class TrayDragDropEventArgs : EventArgs
    {
        public TraySlot TranferFrom { get; set; }
        public TraySlot TranferTo { get; set; }

        public TrayDragDropEventArgs(TraySlot p_TranferFrom, TraySlot p_TranferTo)
        {
            this.TranferFrom = p_TranferFrom;
            this.TranferTo = p_TranferTo;
        }
    }


    public class TraySlot : ContentControl
    {
        public event MouseButtonEventHandler SlotMouseButtonDown;
        public event TrayDragDropHandler TrayTransferStarted;

        static TraySlot()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TraySlot), new FrameworkPropertyMetadata(typeof(TraySlot)));
        }


        public bool IsDraggable { get; set; }
        public bool IsLeftMouseDown { get; set; }     //confirm drag source is current slot

        public string ViewType
        {
            get { return (string)GetValue(ViewTypeProperty); }
            set { SetValue(ViewTypeProperty, value); }
        }
        public static readonly DependencyProperty ViewTypeProperty =
            DependencyProperty.Register("ViewType", typeof(string), typeof(TraySlot),
            new UIPropertyMetadata("Front"));


        public int TrayStatus
        {
            get { return (int)GetValue(TrayStatusProperty); }
            set { SetValue(TrayStatusProperty, value); }
        }
        public static readonly DependencyProperty TrayStatusProperty =
            DependencyProperty.Register("TrayStatus", typeof(int), typeof(TraySlot),
           new UIPropertyMetadata(0, new PropertyChangedCallback(TrayStatusChangedCallBack)));

        public int SlotID
        {
            get { return (int)GetValue(SlotIDProperty); }
            set { SetValue(SlotIDProperty, value); }
        }
        public static readonly DependencyProperty SlotIDProperty =
            DependencyProperty.Register("SlotID", typeof(int), typeof(TraySlot), new UIPropertyMetadata(-1));

        public string ModuleID
        {
            get { return (string)GetValue(ModuleIDProperty); }
            set { SetValue(ModuleIDProperty, value); }
        }
        public static readonly DependencyProperty ModuleIDProperty =
            DependencyProperty.Register("ModuleID", typeof(string), typeof(TraySlot), new UIPropertyMetadata(string.Empty));

        public bool IsDragSource
        {
            get { return (bool)GetValue(IsDragSourceProperty); }
            set { SetValue(IsDragSourceProperty, value); }
        }
        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.Register("IsDragSource", typeof(bool), typeof(TraySlot),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsDragSourcePropertyChangedCallBack)));

        public bool IsDropTarget
        {
            get { return (bool)GetValue(IsDropTargetProperty); }
            set { SetValue(IsDropTargetProperty, value); }
        }
        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.Register("IsDropTarget", typeof(bool), typeof(TraySlot),
              new UIPropertyMetadata(false, new PropertyChangedCallback(IsDropTargetPropertyChangedCallBack)));

        public bool IsDragEnter
        {
            get { return (bool)GetValue(IsDragEnterProperty); }
            set { SetValue(IsDragEnterProperty, value); }
        }
        public static readonly DependencyProperty IsDragEnterProperty =
            DependencyProperty.Register("IsDragEnter", typeof(bool), typeof(TraySlot),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsDragEnterPropertyChangedCallBack)));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(TraySlot),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsSelectedPropertyChangedCallBack)));

        public bool HasTray
        {
            get { return (bool)GetValue(HasTrayProperty); }
            set { SetValue(HasTrayProperty, value); }
        }
        public static readonly DependencyProperty HasTrayProperty =
         DependencyProperty.Register("HasTray", typeof(bool), typeof(TraySlot),
        new UIPropertyMetadata(false, HasTrayChanged));

        public SlotBorderStatus BorderStatus
        {
            get { return (SlotBorderStatus)GetValue(BorderStatusProperty); }
            set { SetValue(BorderStatusProperty, value); }
        }
        public static readonly DependencyProperty BorderStatusProperty =
            DependencyProperty.Register("BorderStatus", typeof(SlotBorderStatus), typeof(TraySlot),
            new UIPropertyMetadata(SlotBorderStatus.None));

        public string SourceName
        {
            get { return (string)GetValue(SourceNameProperty); }
            set { SetValue(SourceNameProperty, value); }
        }
        public static readonly DependencyProperty SourceNameProperty =
            DependencyProperty.Register("SourceName", typeof(string), typeof(TraySlot), new UIPropertyMetadata(string.Empty));


        public bool CanDragDrop
        {
            get { return (bool)GetValue(CanDragDropProperty); }
            set { SetValue(CanDragDropProperty, value); }
        }
        public static readonly DependencyProperty CanDragDropProperty =
            DependencyProperty.Register("CanDragDrop", typeof(bool), typeof(TraySlot), new UIPropertyMetadata(true, new PropertyChangedCallback(CanDragDropPropertyChangedCallBack)));

        public string TrayTooltip
        {
            get { return (string)GetValue(TrayTooltipProperty); }
            set { SetValue(TrayTooltipProperty, value); }
        }
        public static readonly DependencyProperty TrayTooltipProperty =
            DependencyProperty.Register("TrayTooltip", typeof(string), typeof(TraySlot), new PropertyMetadata(string.Empty));

        public string TrayTooltipExt
        {
            get { return (string)GetValue(TrayTooltipExtProperty); }
            set { SetValue(TrayTooltipExtProperty, value); }
        }
        public static readonly DependencyProperty TrayTooltipExtProperty =
            DependencyProperty.Register("TrayTooltipExt", typeof(string), typeof(TraySlot), new PropertyMetadata(string.Empty));

        public Visibility DuplicatedVisibility
        {
            get { return (Visibility)GetValue(DuplicatedVisibilityProperty); }
            set { SetValue(DuplicatedVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DuplicatedVisibilityProperty =
            DependencyProperty.Register("DuplicatedVisibility", typeof(Visibility), typeof(TraySlot), new UIPropertyMetadata(Visibility.Collapsed));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            DragDropStatusControl(this);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            this.IsDragEnter = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            this.IsDragEnter = false;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (MouseButtonState.Pressed == e.LeftButton)
                IsLeftMouseDown = true;
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            IsLeftMouseDown = false;

            MouseButtonEventHandler handler = SlotMouseButtonDown;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            this.BorderStatus = this.BorderStatus | SlotBorderStatus.MouseOver;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            IsLeftMouseDown = false;
            this.BorderStatus = this.BorderStatus & (~SlotBorderStatus.MouseOver);
        }

        private static void DragDropStatusControl(TraySlot p_slot)
        {
            p_slot.AllowDrop = false;
            p_slot.IsDraggable = false;

            if (p_slot.CanDragDrop)
            {
                if (!p_slot.IsDropTarget && p_slot.TrayStatus == 0)
                    p_slot.AllowDrop = true;

                if (!p_slot.IsDragSource && p_slot.TrayStatus != 0)
                    p_slot.IsDraggable = true;
            }
        }

        private static void IsDropTargetPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TraySlot m_slot = d as TraySlot;

            if (m_slot.IsDropTarget)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.TransferTarget;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.TransferTarget); //remove

            DragDropStatusControl(m_slot);
        }

        private static void IsDragSourcePropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TraySlot m_slot = d as TraySlot;

            if (m_slot.IsDragSource)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.TransferSource;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.TransferSource); //remove

            DragDropStatusControl(m_slot);
        }

        private static void IsSelectedPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TraySlot m_slot = d as TraySlot;

            if (m_slot.IsSelected)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.Selected;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.Selected); //remove
        }

        private static void CanDragDropPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TraySlot m_slot = d as TraySlot;
            DragDropStatusControl(m_slot);
        }

        private static void IsDragEnterPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TraySlot m_slot = d as TraySlot;

            if (m_slot.IsDragEnter)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.TransferTarget;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.TransferTarget); //remove
        }

        private static void TrayStatusChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TraySlot m_slot = d as TraySlot;
            m_slot.IsDragSource = false;
            m_slot.IsDropTarget = false;
            m_slot.IsDragEnter = false;
            DragDropStatusControl(m_slot);
        }

        public static void HasTrayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TraySlot m_slot = d as TraySlot;
            if (m_slot != null)
            {
                if (m_slot.HasTray)
                    m_slot.TrayStatus = 7;
                else
                    m_slot.TrayStatus = 0;
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed && IsDraggable && IsLeftMouseDown)
            {
                DataObject data = new DataObject(typeof(TraySlot), this);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            if (this.AllowDrop)
            {
                try
                {
                    IDataObject data = e.Data;
                    if (data.GetDataPresent(typeof(TraySlot)))
                    {
                        TraySlot m_dragSource = (TraySlot)data.GetData(typeof(TraySlot));
                        m_dragSource.IsDragSource = true;   //source
                        this.IsDropTarget = true;           //target
                        if (TrayTransferStarted != null)
                        {
                            TrayDragDropEventArgs m_arg = new TrayDragDropEventArgs(m_dragSource, this);
                            TrayTransferStarted(this, m_arg);
                        }
                    }
                    else //to support another type of wafer
                    {
                        var sourceStation = e.Data.GetData("Station").ToString();
                        var sourceSlot = (int)e.Data.GetData("Slot");
                        TraySlot m_dragSource = new TraySlot() { ModuleID = sourceStation, SlotID = sourceSlot };
                        if (TrayTransferStarted != null)
                        {
                            TrayDragDropEventArgs m_arg = new TrayDragDropEventArgs(m_dragSource, this);
                            TrayTransferStarted(this, m_arg);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public bool IsValidSlot()
        {
            if (this.ModuleID.Length > 0 && this.SlotID >= 0)
                return true;
            else
                return false;
        }

        public bool IsSameSlot(TraySlot slot)
        {
            if (slot.IsValidSlot() && this.IsValidSlot())
            {
                if (this.ModuleID == slot.ModuleID && this.SlotID == slot.SlotID)
                    return true;
            }
            return false;
        }
        public void ClearDragDropStatus()
        {
            this.IsDragEnter = false;
            this.IsDragSource = false;
            this.IsDropTarget = false;
        }


    }
}
