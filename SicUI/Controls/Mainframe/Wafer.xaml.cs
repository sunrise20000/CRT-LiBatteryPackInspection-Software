using Aitex.Core.UI.MVVM;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MECF.Framework.UI.Client.ClientBase;
using GUI = OpenSEMI.Ctrlib.Controls;
using WaferInfo = Aitex.Core.Common.WaferInfo;
using WaferStatus = Aitex.Core.Common.WaferStatus;

namespace SicUI.Controls.Parts
{
	/// <summary>
	/// Wafer.xaml 的交互逻辑
	/// </summary>
	public partial class Wafer : UserControl
	{
		public static readonly DependencyProperty WaferItemProperty = DependencyProperty.Register(
		"WaferItem", typeof(WaferInfo), typeof(Wafer),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

		public WaferInfo WaferItem
		{
			get
			{
				return (WaferInfo)GetValue(WaferItemProperty);
			}
			set
			{
				SetValue(WaferItemProperty, value);
			}
		}

		public int Slot
		{
			get { return (int)GetValue(SlotProperty); }
			set { SetValue(SlotProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Slot.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SlotProperty =
			DependencyProperty.Register("Slot", typeof(int), typeof(Wafer), new PropertyMetadata(-1));

		public ModuleName Station
		{
			get { return (ModuleName)GetValue(StationProperty); }
			set { SetValue(StationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StationProperty =
			DependencyProperty.Register("Station", typeof(ModuleName), typeof(Wafer), new PropertyMetadata(ModuleName.System));

		public ICommand WaferTransferCommand
		{
			get { return (ICommand)GetValue(WaferTransferCommandProperty); }
			set { SetValue(WaferTransferCommandProperty, value); }
		}

		public bool ShowSlot
		{
			get { return (bool)GetValue(ShowSlotProperty); }
			set { SetValue(ShowSlotProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowSlot.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowSlotProperty =
			DependencyProperty.Register("ShowSlot", typeof(bool), typeof(Wafer), new PropertyMetadata(false));

		// Using a DependencyProperty as the backing store for WaferMovementCommand.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty WaferTransferCommandProperty =
			DependencyProperty.Register("WaferTransferCommand", typeof(ICommand), typeof(Wafer), new PropertyMetadata(null));

		//public WaferDisplayMode WaferIDDisplayMode
		//{
		//	get { return (WaferDisplayMode)GetValue(WaferDisplayModeProperty); }
		//	set { SetValue(WaferDisplayModeProperty, value); }
		//}

		// Using a DependencyProperty as the backing store for WaferIDDisplayMode.  This enables animation, styling, binding, etc...
		//public static readonly DependencyProperty WaferDisplayModeProperty =
		//	DependencyProperty.Register("WaferIDDisplayMode", typeof(WaferDisplayMode), typeof(Wafer), new PropertyMetadata(WaferDisplayMode.None));

		public ICommand WaferTransferOptionCommand
		{
			get { return (ICommand)GetValue(WaferTransferOptionCommandProperty); }
			set { SetValue(WaferTransferOptionCommandProperty, value); }
		}

		// Using a DependencyProperty as the backing store for WaferTransferOptionCommand.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty WaferTransferOptionCommandProperty =
			DependencyProperty.Register("WaferTransferOptionCommand", typeof(ICommand), typeof(Wafer), new PropertyMetadata(null));

		public ICommand CreateDeleteWaferCommand
		{
			get { return (ICommand)GetValue(CreateDeleteWaferCommandProperty); }
			set { SetValue(CreateDeleteWaferCommandProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CreateDeleteWaferCommand.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CreateDeleteWaferCommandProperty =
			DependencyProperty.Register("CreateDeleteWaferCommand", typeof(ICommand), typeof(Wafer), new PropertyMetadata(null));

		private WaferStyle waferStyle = WaferStyle.Rect;

		public WaferStyle WaferStyle
		{
			get
			{
				return waferStyle;
			}
			set
			{
				waferStyle = value;
			}
		}

		public Wafer()
		{
			InitializeComponent();

			CreateDeleteWaferCommand = new DelegateCommand<string>(CreateDeleteWafer);

			root.DataContext = this;
		}

		private void Wafer_MouseMove(object sender, MouseEventArgs e)
		{
			if (WaferItem == null)
			{
				return;
			}

			if (e.LeftButton == MouseButtonState.Pressed && (WaferItem.Status == WaferStatus.Normal || WaferItem.Status == WaferStatus.Dummy) && !WaferItem.IsSource)
			{
				var data = new DataObject();
				data.SetData("Object", WaferItem);
				data.SetData("Station", Station);
				data.SetData("Slot", Slot);
				DragDrop.DoDragDrop(sender as DependencyObject,
							 data,
							 DragDropEffects.Copy | DragDropEffects.Move);
			}
		}

		private void Wafer_Drop(object sender, DragEventArgs e)
		{
			if (sender != null)
			{
                GUI.Slot m_dragSource;

                if (e.Data.GetDataPresent(typeof(GUI.Slot))) //drag wafer from new UI source
                {
                    m_dragSource = (GUI.Slot)e.Data.GetData(typeof(GUI.Slot));              
                }
                else
                {
                    //((Border)sender).BorderThickness = new Thickness(0);

                    var sourceWafer = (WaferInfo)e.Data.GetData("Object");
                    var sourceStation = (ModuleName)e.Data.GetData("Station");
                    var sourceSlot = (int)e.Data.GetData("Slot");

                    m_dragSource = new GUI.Slot();
                    m_dragSource.ModuleID = sourceStation.ToString();
                    m_dragSource.SlotID = sourceSlot;

                    //if (sourceWafer == null)
                    //{
                    //    return;
                    //}

                    //if (sourceStation == ModuleName.Robot && Station == ModuleName.Robot)
                    //{
                    //    return;
                    //}

                    //WaferTransferOption transferOption = null;

                    //if (WaferTransferOptionCommand != null)
                    //{
                    //    transferOption = new WaferTransferOption();
                    //    WaferTransferOptionCommand.Execute(transferOption);
                    //}
                    //var dialog = new WaferTransferDialog(sourceWafer, sourceStation, sourceSlot, Station, Slot, transferOption);
                    //dialog.Owner = Window.GetWindow(this);
                    //if (dialog.ShowDialog() == true)
                    //{
                    //    if (sourceWafer.Status == WaferStatus.Dummy)
                    //    {
                    //        if (WaferTransferCommand != null)
                    //        {
                    //            WaferTransferCommand.Execute(new SorterRecipeTransferTableItem
                    //            {
                    //                SourceStation = sourceStation,
                    //                SourceSlot = sourceSlot,
                    //                DestinationStation = Station,
                    //                DestinationSlot = Slot,
                    //                IsAlign = dialog.Aligner,
                    //                IsReadLaserMarker = dialog.ReadLaserMarker,
                    //                IsReadT7Code = dialog.ReadT7Code,
                    //                AlignAngle = dialog.AlignerAngle
                    //            });
                    //        }
                    //    }
                    //    else
                    //    {
                    //        var moveType = MoveType.Move;
                    //        MoveOption moveOption = 0;
                    //        if (dialog.Aligner)
                    //        {
                    //            moveOption |= MoveOption.Align;
                    //        }
                    //        if (dialog.ReadLaserMarker)
                    //        {
                    //            moveOption |= MoveOption.ReadID;
                    //        }
                    //        if (dialog.ReadT7Code)
                    //        {
                    //            moveOption |= MoveOption.ReadID2;
                    //        }
                    //        var hand = (Hand)dialog.Blade;
                    //        var param = new object[]
                    //        {
                    //        moveType,
                    //        moveOption,
                    //        hand,
                    //        sourceStation,
                    //        sourceSlot,
                    //        Station,
                    //        Slot
                    //        };
                    //        //InvokeClient.Instance.Service.DoOperation(OperationName.MoveWafer.ToString(), param);
                    //    }
                    //}
                }

                GUI.Slot _target = new GUI.Slot();
                _target.ModuleID = Station.ToString();
                _target.SlotID = Slot;

                WaferMoveManager.Instance.TransferWafer(m_dragSource, _target);
            }				
		}

		private void CreateDeleteWafer(string cmd)
		{
			var param = new object[] { Station, Slot, WaferStatus.Normal };
			InvokeClient.Instance.Service.DoOperation(cmd, param);
		}

		private void Border_DragOver(object sender, DragEventArgs e)
		{
			var sourceWafer = (WaferInfo)e.Data.GetData("Object");
			var sourceStation = (ModuleName)e.Data.GetData("Station");
			var sourceSlot = (int)e.Data.GetData("Slot");

			if (!(sourceStation == Station && sourceSlot == Slot))
			{
				((Border)sender).BorderThickness = new Thickness(1);
			}
		}

		private void Border_DragLeave(object sender, DragEventArgs e)
		{
			var sourceWafer = (WaferInfo)e.Data.GetData("Object");
			var sourceStation = (ModuleName)e.Data.GetData("Station");
			var sourceSlot = (int)e.Data.GetData("Slot");

			if (!(sourceStation == Station && sourceSlot == Slot))
			{
				((Border)sender).BorderThickness = new Thickness(0);
			}
		}
	}

	public enum WaferStyle
	{
		Rect,
		Eclipse
	}

	public class WaferTransferOption
	{
		public WaferTransferOption()
		{
			Setting = new WaferTransferOptionSetting();
		}

		public bool Align { get; set; }

		public bool ReadLaserMarker { get; set; }

		public bool ReadT7Code { get; set; }

		public int Blade { get; set; }

		public double AlignerAngle { get; set; }

		public WaferTransferOptionSetting Setting { get; set; }
	}

	public class WaferTransferOptionSetting
	{
		public WaferTransferOptionSetting()
		{
			ShowAlign = true;
			ShowLaserMarker = true;
			ShowT7Code = true;
		}

		public bool ShowAlign { get; set; }

		public bool ShowLaserMarker { get; set; }

		public bool ShowT7Code { get; set; }
	}
}
