using Aitex.Core.RT.Log;
using Aitex.Core.UI.Control;
using Caliburn.Micro;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using OpenSEMI.Ctrlib.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class MenuElement
    {
        public string Name { get; set; }
        public string Invoke { get; set; }
        public bool IsSeparator { get; set; }
    }

    public class ContextMenuManager
    {
        public bool ShowAligner { get; set; }
        public bool ShowCooling { get; set; }

        public ContextMenuManager()
        {
            _slotMenus = InitMenus(_SlotMenuElements);
            _carrierMenus = InitMenus(_carrierMenuElements);

            ShowAligner = true;
            ShowCooling = true;
        }

        #region single Instance
        private static ContextMenuManager m_Instance = null;
        public static ContextMenuManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new ContextMenuManager();
                }
                return m_Instance;
            }
        }
        #endregion

        public ContextMenu GetSlotMenus(Slot slot)
        {
            if (slot != null)
            {
                if (slot.ModuleID == ModuleName.CassAL.ToString() || slot.ModuleID == ModuleName.CassAR.ToString())
                {
                    return GetWaferMenus(slot);
                }
                else if (slot.ModuleID == ModuleName.CassBL.ToString() || slot.ModuleID == ModuleName.TrayRobot.ToString())
                {
                    return GetTrayMenus(slot);
                }
                else if (slot.ModuleID == ModuleName.LoadLock.ToString() || slot.ModuleID == ModuleName.UnLoad.ToString() 
                    || slot.ModuleID == ModuleName.TMRobot.ToString() || slot.ModuleID == ModuleName.Buffer.ToString())
                {
                    return GetAllMenus(slot);
                }
                else
                {
                    return GetWaferMenus(slot);
                }

            }
            return null;
        }

        public void OnContextMenuClick(object sender, RoutedEventArgs args)
        {
            try
            {
                MenuItem m_menu = sender as MenuItem;
                Slot CurrentSlot = ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent(m_menu)) as Slot;
                if (CurrentSlot == null)
                    return;

                MenuElement ele = m_menu.Tag as MenuElement;

                //if (ele.Invoke == "ReturnWafer")
                //{
                //    ReturnWafer(ele.Invoke, CurrentSlot);
                //}
                //else
                //{
                //    InvokeClient.Instance.Service.DoOperation(ele.Invoke, CurrentSlot.ModuleID, CurrentSlot.SlotID);
                //}

                //var param = new object[] { ModuleName.LP1, 5, WaferStatus.Normal };

                if (ele.Invoke != "AlterInfo")
                {
                    InvokeClient.Instance.Service.DoOperation(ele.Invoke, CurrentSlot.ModuleID, CurrentSlot.SlotID);
                }
                else
                {
                    if (CurrentSlot.WaferStatus == (int)WaferStatus.Empty)
                    {
                        MessageBox.Show("There is no wafer in the slot.", "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                    
                    if (!(CurrentSlot.DataContext is WaferInfo waferInfo))
                    {
                        const string ERR = "The type of DataContext is not WaferInfo.";
                        LOG.Error($"Unable to open Alter Info dialog, {ERR}");
                        MessageBox.Show(ERR, "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    var slotDialog = new SlotEditorDialogBox(waferInfo.WaferStatus, waferInfo.WaferTrayStatus)
                    {
                        Title = "Wafer Editor",
                        WaferID = waferInfo.WaferID,
                        RecipeName = CurrentSlot.RecipeName,
                        TrayProcessCount = CurrentSlot.TrayProcessCount,
                        ModuleID = CurrentSlot.ModuleID,
                        SlotID = CurrentSlot.SlotID
                    };

                    slotDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private ContextMenu InitMenus(List<MenuElement> menus)
        {
            ContextMenu cm = new ContextMenu();

            foreach (MenuElement element in menus)
            {
                if (element.IsSeparator)
                {
                    cm.Items.Add(new Separator());
                }
                else
                {
                    MenuItem m_item = new MenuItem();
                    m_item.Header = element.Name;
                    m_item.Tag = element;
                    m_item.Click += new RoutedEventHandler(OnContextMenuClick);
                    m_item.IsEnabled = true;
                    cm.Items.Add(m_item);
                }

            }
            return cm;
        }


        #region Wafer Menus

        private ContextMenu _slotMenus;
        public ContextMenu SlotMenus
        {
            get { return _slotMenus; }
        }

        private readonly List<MenuElement> _SlotMenuElements = new List<MenuElement>() {
            
            new MenuElement(){ Name="Create Wafer", Invoke="CreateWafer"},
            new MenuElement(){ Name="Create Tray", Invoke="CreateWafer"},
            new MenuElement(){ Name="Delete Wafer", Invoke="DeleteWafer"},
            new MenuElement(){ Name="Delete Tray", Invoke="DeleteTray"},
            new MenuElement(){ Name="-", IsSeparator = true},
            new MenuElement(){ Name="Return Wafer", Invoke="ReturnWafer"},
            new MenuElement(){ Name="Alter Info", Invoke="AlterInfo"}
        };
        
        public ContextMenu GetWaferMenus(Slot slot)
        {

            MenuItem createWafer = _slotMenus.Items.GetItemAt(0) as MenuItem;
            MenuItem createTray = _slotMenus.Items.GetItemAt(1) as MenuItem;
            MenuItem deleteWafer = _slotMenus.Items.GetItemAt(2) as MenuItem;
            MenuItem deleteTray = _slotMenus.Items.GetItemAt(3) as MenuItem;
            MenuItem returnWafer = _slotMenus.Items.GetItemAt(5) as MenuItem;
            Separator separator = _slotMenus.Items.GetItemAt(4) as Separator;
            MenuItem alertInfo = _slotMenus.Items.GetItemAt(6) as MenuItem;

            createWafer.IsEnabled = (WaferStatus)slot.WaferStatus == WaferStatus.Empty ;
            deleteWafer.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty ;
            returnWafer.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty && !slot.ModuleID.StartsWith("Cass");
            alertInfo.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty;

            createWafer.Visibility = Visibility.Visible;
            deleteWafer.Visibility = Visibility.Visible;
            createTray.Visibility = Visibility.Collapsed;
            deleteTray.Visibility = Visibility.Collapsed;
            separator.Visibility = Visibility.Visible;
            returnWafer.Visibility = Visibility.Visible;
            return _slotMenus;
        }

        public ContextMenu GetTrayMenus(Slot slot)
        {
            MenuItem createWafer = _slotMenus.Items.GetItemAt(0) as MenuItem;
            MenuItem createTray = _slotMenus.Items.GetItemAt(1) as MenuItem;
            MenuItem deleteWafer = _slotMenus.Items.GetItemAt(2) as MenuItem;
            MenuItem deleteTray = _slotMenus.Items.GetItemAt(3) as MenuItem;
            MenuItem returnWafer = _slotMenus.Items.GetItemAt(5) as MenuItem;
            Separator separator = _slotMenus.Items.GetItemAt(4) as Separator;
            MenuItem alertInfo = _slotMenus.Items.GetItemAt(6) as MenuItem;

            createTray.IsEnabled = true; //slot.TrayStatus == 0;
            deleteTray.IsEnabled = true; //slot.TrayStatus != 0;
            alertInfo.IsEnabled = slot.WaferStatus != (int)WaferStatus.Empty;

            createTray.Visibility = Visibility.Visible;
            deleteTray.Visibility = Visibility.Visible;
            separator.Visibility = Visibility.Collapsed;
            deleteWafer.Visibility = Visibility.Collapsed;
            createWafer.Visibility = Visibility.Collapsed;             
            returnWafer.Visibility = Visibility.Collapsed;
            return _slotMenus;
        }

        public ContextMenu GetAllMenus(Slot slot)
        {
            MenuItem createWafer = _slotMenus.Items.GetItemAt(0) as MenuItem; 
            MenuItem createTray = _slotMenus.Items.GetItemAt(1) as MenuItem;
            MenuItem deleteWafer = _slotMenus.Items.GetItemAt(2) as MenuItem;
            MenuItem deleteTray = _slotMenus.Items.GetItemAt(3) as MenuItem;
            MenuItem returnWafer = _slotMenus.Items.GetItemAt(5) as MenuItem;
            Separator separator = _slotMenus.Items.GetItemAt(4) as Separator;
            MenuItem alertInfo = _slotMenus.Items.GetItemAt(6) as MenuItem;

            createWafer.IsEnabled = (WaferStatus)slot.WaferStatus == WaferStatus.Empty ;
            createTray.IsEnabled = true;
            deleteWafer.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty ;
            deleteTray.IsEnabled = true; // slot.TrayStatus != 0; 
            returnWafer.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty && !slot.ModuleID.StartsWith("Cass");
            alertInfo.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty;

            separator.Visibility = Visibility.Visible;
            createWafer.Visibility = Visibility.Visible;
            deleteWafer.Visibility = Visibility.Visible;
            createTray.Visibility = Visibility.Visible;
            deleteTray.Visibility = Visibility.Visible;
            returnWafer.Visibility = Visibility.Visible;
            return _slotMenus;
        }

        public void ReturnWafer(string menu, Slot p_from)
        {
            try
            {
                if (p_from == null || !p_from.IsValidSlot())
                    return;

                //DialogButton btns = DialogButton.Transfer | DialogButton.Cancel;       
                string info = " from " + p_from.ModuleID + " slot " + (p_from.SlotID + 1).ToString();
                string message = "Are you sure to return the wafer: \n" + info;
                //DialogButton m_dResult = DialogBox.ShowDialog(btns, DialogType.CONFIRM, message);

                bool displayAlignerCondition = p_from.ModuleID == "LP1" || p_from.ModuleID == "LP2" || p_from.ModuleID == "LP3" || p_from.ModuleID == "EfemRobot"
                    || p_from.ModuleID == "LLA" || p_from.ModuleID == "LLB" || p_from.ModuleID == "LLC" || p_from.ModuleID == "LLD" || p_from.ModuleID == "Buffer";

                displayAlignerCondition = displayAlignerCondition && (p_from.ModuleID != "Aligner");

                bool displayPassCoolingCondition = (p_from.ModuleID.Contains("PM") || p_from.ModuleID == "TMRobot");

                WindowManager wm = new WindowManager();
                WaferTransferDialogViewModel _transferVM = new WaferTransferDialogViewModel(message, displayAlignerCondition, displayPassCoolingCondition);
                _transferVM.AlignerVisibility = ShowAligner ? Visibility.Visible : Visibility.Hidden;
                _transferVM.CoolingVisibility = ShowCooling ? Visibility.Visible : Visibility.Hidden;
                bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
                if ((bool)bret)
                {
                    //get and use transfer conditions
                    WaferTransferCondition conditions = _transferVM.DialogResult;

                    InvokeClient.Instance.Service.DoOperation(menu, p_from.ModuleID, p_from.SlotID,
                        conditions.IsPassCooling, conditions.CoolingTime);
                }

                p_from.ClearDragDropStatus();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        #endregion

        #region Tray Menus

        //private ContextMenu _trayMenus;
        //public ContextMenu TrayMenus
        //{
        //    get { return _trayMenus; }
        //}

        //private readonly List<MenuElement> _TrayMenuElements = new List<MenuElement>() {

        //    new MenuElement(){ Name="Create Tray", Invoke="CreateTray"},
        //    new MenuElement(){ Name="Delete Tray", Invoke="DeleteTray"},
        //};


        //public ContextMenu GetTrayMenus(Slot slot)
        //{
        //    if (slot != null)
        //    {
        //        MenuItem createTray = _trayMenus.Items.GetItemAt(0) as MenuItem;
        //        MenuItem deleteTray = _trayMenus.Items.GetItemAt(1) as MenuItem;

        //        createTray.IsEnabled = true;// (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? true : false;
        //        deleteTray.IsEnabled = true;// (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? false : true;               
        //    }
        //    return _trayMenus;
        //}

        //public ContextMenu GetAllMenus(Slot slot)
        //{
        //    if (slot != null)
        //    {
        //        MenuItem createTray = _trayMenus.Items.GetItemAt(0) as MenuItem;
        //        MenuItem deleteTray = _trayMenus.Items.GetItemAt(1) as MenuItem;
        //        MenuItem returnTray = _trayMenus.Items.GetItemAt(3) as MenuItem;

        //        createTray.IsEnabled = true;// (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? true : false;
        //        deleteTray.IsEnabled = true;// (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? false : true;                
        //        returnTray.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty && !slot.ModuleID.StartsWith("LP");
        //    }
        //    return _trayMenus;
        //}


        #endregion



        #region Carrier menus

        private ContextMenu _carrierMenus;
        public ContextMenu CarrierMenus
        {
            get { return _carrierMenus; }
        }

        private readonly List<MenuElement> _carrierMenuElements = new List<MenuElement>() {
            new MenuElement(){ Name="Create Carrier", Invoke="System.CreateCarrier"},
            new MenuElement(){ Name="Delete Carrier", Invoke="System.DeleteCarrier"},

        };

        public void OnCarrierContextMenuClick(object sender, RoutedEventArgs args)
        {
            try
            {
                MenuItem menuItem = sender as MenuItem;
                CarrierContentControl locateCarrier = ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent(menuItem)) as CarrierContentControl;
                if (locateCarrier == null)
                    return;

                MenuElement ele = menuItem.Tag as MenuElement;
 
                InvokeClient.Instance.Service.DoOperation(ele.Invoke, locateCarrier.ModuleID, locateCarrier.CarrierID);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        public ContextMenu GetCarrierMenus(CarrierContentControl carrier)
        {
            if (carrier != null)
            {
                MenuItem createCarrier = _carrierMenus.Items.GetItemAt(0) as MenuItem;
                MenuItem deleteWafer = _carrierMenus.Items.GetItemAt(1) as MenuItem;

                createCarrier.IsEnabled = (WaferStatus)carrier.WaferStatus == WaferStatus.Empty ? true : false;
                deleteWafer.IsEnabled = (WaferStatus)carrier.WaferStatus == WaferStatus.Empty ? false : true;
            }
            return _carrierMenus;
        }

        #endregion

    }
}
