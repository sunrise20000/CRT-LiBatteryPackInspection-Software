using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// AutomationPeer for an item in a DataGrid
    /// This automation peer correspond to a row data item which may not have a visual container
    /// </summary>
    public sealed class DataGridItemAutomationPeer : AutomationPeer,
        IInvokeProvider, IScrollItemProvider, ISelectionItemProvider, ISelectionProvider
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for an item in a DataGrid
        /// </summary>
        public DataGridItemAutomationPeer(object item, EGC.DataGrid dataGrid)
            : base()
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (dataGrid == null)
            {
                throw new ArgumentNullException("dataGrid");
            }

            _item = item;
            _dataGridAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(dataGrid);
        }

        #endregion

        #region AutomationPeer Overrides
        
        ///
        protected override string GetAcceleratorKeyCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetAcceleratorKey() : string.Empty;
        }

        ///
        protected override string GetAccessKeyCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetAccessKey() : string.Empty;
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataItem;
        }

        ///
        protected override string GetAutomationIdCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetAutomationId() : string.Empty;
        }

        ///
        protected override Rect GetBoundingRectangleCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetBoundingRectangle() : new Rect();
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetChildren() : GetCellItemPeers();
        }
        
        ///
        protected override string GetClassNameCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetClassName() : string.Empty;
        }

        ///
        protected override Point GetClickablePointCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetClickablePoint() : new Point(double.NaN, double.NaN);
        }

        ///
        protected override string GetHelpTextCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetHelpText() : string.Empty;
        }

        ///
        protected override string GetItemStatusCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetItemStatus() : string.Empty;
        }

        ///
        protected override string GetItemTypeCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetItemType() : string.Empty;
        }

        ///
        protected override AutomationPeer GetLabeledByCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetLabeledBy() : null;
        }

        ///
        protected override string GetLocalizedControlTypeCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetLocalizedControlType() : base.GetLocalizedControlTypeCore();
        }

        ///
        protected override string GetNameCore()
        {
            return _item.ToString();
        }

        ///
        protected override AutomationOrientation GetOrientationCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.GetOrientation() : AutomationOrientation.None;
        }

        ///
        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.Invoke:
                    {
                        if (!this.OwningDataGrid.IsReadOnly)
                        {
                            return this;
                        }

                        break;
                    }

                case PatternInterface.ScrollItem:
                case PatternInterface.Selection:
                case PatternInterface.SelectionItem:
                    return this;
            }

            return null;
        }

        ///
        protected override bool HasKeyboardFocusCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.HasKeyboardFocus() : false;
        }

        ///
        protected override bool IsContentElementCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsContentElement() : true;
        }

        ///
        protected override bool IsControlElementCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsControlElement() : true;
        }

        ///
        protected override bool IsEnabledCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsEnabled() : true;
        }

        ///
        protected override bool IsKeyboardFocusableCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsKeyboardFocusable() : false;
        }

        ///
        protected override bool IsOffscreenCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsOffscreen() : true;
        }

        ///
        protected override bool IsPasswordCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsPassword() : false;
        }

        ///
        protected override bool IsRequiredForFormCore()
        {
            return (this.OwningRowPeer != null) ? this.OwningRowPeer.IsRequiredForForm() : false;
        }

        ///
        protected override void SetFocusCore()
        {
            if (this.OwningRowPeer != null && this.OwningRowPeer.Owner.Focusable)
            {
                this.OwningRowPeer.SetFocus();
            }
        }
        
        #endregion

        #region IInvokeProvider

        // Invoking DataGrid item should commit the item if it is in edit mode
        // or BeginEdit if item is not in edit mode
        void IInvokeProvider.Invoke()
        {
            EnsureEnabled();

            if (this.OwningRowPeer == null)
            {
                this.OwningDataGrid.ScrollIntoView(_item);
            }
              
            bool success = false;
            if (this.OwningRow != null)
            {
                IEditableCollectionView iecv = (IEditableCollectionView)this.OwningDataGrid.Items;
                if (iecv.CurrentEditItem == _item)
                {
                    success = this.OwningDataGrid.CommitEdit();
                }
                else
                {
                    if (this.OwningDataGrid.Columns.Count > 0)
                    {
                        EGC.DataGridCell cell = this.OwningDataGrid.TryFindCell(_item, this.OwningDataGrid.Columns[0]);
                        if (cell != null)
                        {
                            this.OwningDataGrid.UnselectAll();
                            cell.Focus();
                            success = this.OwningDataGrid.BeginEdit();
                        }
                    }
                }
            }

            if (!success)
            {
                throw new InvalidOperationException();
            } 
        }

        #endregion

        #region IScrollItemProvider
        
        void IScrollItemProvider.ScrollIntoView()
        {
            this.OwningDataGrid.ScrollIntoView(_item);
        }
        
        #endregion

        #region ISelectionItemProvider
        
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                return this.OwningDataGrid.SelectedItems.Contains(_item);
            }
        }

        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                return ProviderFromPeer(_dataGridAutomationPeer);
            }
        }

        void ISelectionItemProvider.AddToSelection()
        {
            // If item is already selected - do nothing
            if (this.OwningDataGrid.SelectedItems.Contains(_item))
            {
                return;
            }

            EnsureEnabled();

            if (this.OwningDataGrid.SelectionMode == EGC.DataGridSelectionMode.Single &&
                this.OwningDataGrid.SelectedItems.Count > 0)
            {
                throw new InvalidOperationException();
            }

            if (this.OwningDataGrid.Items.Contains(_item))
            {
                this.OwningDataGrid.SelectedItems.Add(_item);
            }
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            EnsureEnabled();

            if (this.OwningDataGrid.SelectedItems.Contains(_item))
            {
                this.OwningDataGrid.SelectedItems.Remove(_item);
            }
        }

        void ISelectionItemProvider.Select()
        {
            EnsureEnabled();

            this.OwningDataGrid.SelectedItem = _item;
        }
        
        #endregion

        #region ISelectionProvider
        
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return this.OwningDataGrid.SelectionMode == EGC.DataGridSelectionMode.Extended;
            }
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return false;
            }
        }

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            EGC.DataGrid dataGrid = this.OwningDataGrid;
            int rowIndex = dataGrid.Items.IndexOf(_item);

            // If row has selection
            if (rowIndex > -1 && dataGrid.SelectedCellsInternal.Intersects(rowIndex)) 
            {
                List<IRawElementProviderSimple> selectedProviders = new List<IRawElementProviderSimple>();

                for (int i = 0; i < this.OwningDataGrid.Columns.Count; i++)
                {
                    // cell is selected
                    if (dataGrid.SelectedCellsInternal.Contains(rowIndex, i)) 
                    {
                        EGC.DataGridColumn column = dataGrid.ColumnFromDisplayIndex(i);
                        EGC.DataGridCellItemAutomationPeer peer = GetOrCreateCellItemPeer(column);
                        if (peer != null)
                        {
                            selectedProviders.Add(ProviderFromPeer(peer));
                        }
                    }
                }

                if (selectedProviders.Count > 0)
                {
                    return selectedProviders.ToArray();
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        internal List<AutomationPeer> GetCellItemPeers()
        {
            List<AutomationPeer> peers = new List<AutomationPeer>();
            Dictionary<EGC.DataGridColumn, EGC.DataGridCellItemAutomationPeer> oldChildren = new Dictionary<EGC.DataGridColumn, EGC.DataGridCellItemAutomationPeer>(_itemPeers);
            _itemPeers.Clear();

            foreach (EGC.DataGridColumn column in this.OwningDataGrid.Columns)
            {
                EGC.DataGridCellItemAutomationPeer peer = null;
                bool peerExists = oldChildren.TryGetValue(column, out peer);
                if (!peerExists || peer == null)
                {
                    peer = new EGC.DataGridCellItemAutomationPeer(_item, column);
                }

                peers.Add(peer);
                _itemPeers.Add(column, peer);
            }

            return peers;
        }

        internal EGC.DataGridCellItemAutomationPeer GetOrCreateCellItemPeer(EGC.DataGridColumn column)
        {
            EGC.DataGridCellItemAutomationPeer peer = null;
            bool peerExists = _itemPeers.TryGetValue(column, out peer);
            if (!peerExists || peer == null)
            {
                peer = new EGC.DataGridCellItemAutomationPeer(_item, column);
                _itemPeers.Add(column, peer);
            }

            return peer;
        }

        internal AutomationPeer RowHeaderAutomationPeer
        {
            get
            {
                return (this.OwningRowPeer != null) ? this.OwningRowPeer.RowHeaderAutomationPeer : null;
            }
        }

        private void EnsureEnabled()
        {
            if (!_dataGridAutomationPeer.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }
        }
        
        #endregion

        #region Private Properties

        private EGC.DataGrid OwningDataGrid
        {
            get
            {
                EGC.DataGridAutomationPeer gridPeer = _dataGridAutomationPeer as EGC.DataGridAutomationPeer;
                return (EGC.DataGrid)gridPeer.Owner;
            }
        }

        private EGC.DataGridRow OwningRow
        {
            get
            {
                return this.OwningDataGrid.ItemContainerGenerator.ContainerFromItem(_item) as EGC.DataGridRow;
            }
        }

        internal EGC.DataGridRowAutomationPeer OwningRowPeer
        {
            get
            {
                EGC.DataGridRowAutomationPeer rowPeer = null;
                EGC.DataGridRow row = this.OwningRow;
                if (row != null)
                {
                    rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(row) as EGC.DataGridRowAutomationPeer;
                }

                return rowPeer;
            }
        }

        #endregion

        #region Data

        private object _item;
        private AutomationPeer _dataGridAutomationPeer;
        private Dictionary<EGC.DataGridColumn, EGC.DataGridCellItemAutomationPeer> _itemPeers = new Dictionary<EGC.DataGridColumn, EGC.DataGridCellItemAutomationPeer>();

        #endregion
    }
}
