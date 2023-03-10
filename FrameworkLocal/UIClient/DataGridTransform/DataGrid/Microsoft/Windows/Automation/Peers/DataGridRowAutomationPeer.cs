using System;
using System.Collections.Generic;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// AutomationPeer for DataGridRow
    /// </summary>
    public sealed class DataGridRowAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridRow
        /// </summary>
        /// <param name="owner">DataGridRow</param>
        public DataGridRowAutomationPeer(EGC.DataGridRow owner)
            : base(owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            UpdateEventSource();
        }

        #endregion

        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataItem;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType, 
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        /// 
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = new List<AutomationPeer>(3);

            // Step 1: Add row header if exists
            AutomationPeer dataGridRowHeaderAutomationPeer = RowHeaderAutomationPeer;
            if (dataGridRowHeaderAutomationPeer != null)
            {
                children.Add(dataGridRowHeaderAutomationPeer);
            }

            // Step 2: Add all cells
            EGC.DataGridItemAutomationPeer itemPeer = this.EventsSource as DataGridItemAutomationPeer;
            if (itemPeer != null)
            {
                children.AddRange(itemPeer.GetCellItemPeers());
            }
            
            // Step 3: Add DetailsPresenter last if exists
            AutomationPeer dataGridDetailsPresenterAutomationPeer = DetailsPresenterAutomationPeer;
            if (dataGridDetailsPresenterAutomationPeer != null)
            {
                children.Add(dataGridDetailsPresenterAutomationPeer);
            }

            return children;
        }

        #endregion

        #region Private helpers
        internal AutomationPeer RowHeaderAutomationPeer
        {
            get
            {
                EGC.DataGridRowHeader dataGridRowHeader = OwningDataGridRow.RowHeader;
                if (dataGridRowHeader != null)
                {
                    return CreatePeerForElement(dataGridRowHeader);
                }

                return null;
            }
        }

        private AutomationPeer DetailsPresenterAutomationPeer
        {
            get
            {
                EGC.DataGridDetailsPresenter dataGridDetailsPresenter = OwningDataGridRow.DetailsPresenter;
                if (dataGridDetailsPresenter != null)
                {
                    return CreatePeerForElement(dataGridDetailsPresenter);
                }

                return null;
            }
        }

        internal void UpdateEventSource()
        {
            EGC.DataGrid dataGrid = OwningDataGridRow.DataGridOwner;
            if (dataGrid != null)
            {
                EGC.DataGridAutomationPeer dataGridAutomationPeer = CreatePeerForElement(dataGrid) as EGC.DataGridAutomationPeer;
                if (dataGridAutomationPeer != null)
                {
                    AutomationPeer itemAutomationPeer = dataGridAutomationPeer.GetOrCreateItemPeer(OwningDataGridRow.Item);
                    if (itemAutomationPeer != null)
                    {
                        this.EventsSource = itemAutomationPeer;
                    }
                }
            }
        }

        private EGC.DataGridRow OwningDataGridRow
        {
            get
            {
                return (EGC.DataGridRow)Owner;
            }
        }

        #endregion
    }
}
