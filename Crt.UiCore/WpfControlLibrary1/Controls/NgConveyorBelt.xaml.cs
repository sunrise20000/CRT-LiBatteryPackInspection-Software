using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using Crt.UiCore.Controls.Base;
using Crt.UiCore.RtCore;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for NgConveyorBelt.xaml
    /// </summary>
    public partial class NgConveyorBelt : BatteryCarrierBase
    {
        public enum Positions
        {
            /// <summary>
            /// 皮带线回待料位置
            /// </summary>
            Standby,

            /// <summary>
            /// 新增或移除一节电池
            /// </summary>
            AddOrDequeue,  //Add和Remove时皮带线的滚动方向相同，仅电池的显示状态不同， 因此使用同一动画。

            /// <summary>
            /// 从入料口弹出一节电池
            /// </summary>
            Pop
        }

        /// <summary>
        /// 皮带线电池容量。
        /// </summary>
        public const int BATTERY_CAPACITY = 6;


        public NgConveyorBelt()
        {
            InitializeComponent();

            var battInfoList = new List<BatteryInfo>();
            var shadowBattInfoList = new List<BatteryInfo>();
            for (var i = 0; i < BATTERY_CAPACITY; i++)
            {
                battInfoList.Add(new BatteryInfo());
                shadowBattInfoList.Add(new BatteryInfo());
            }

            BatterySlots = new ReadOnlyCollection<BatteryInfo>(battInfoList);
            ShadowBatterySlots = new ReadOnlyCollection<BatteryInfo>(shadowBattInfoList);
        }

        #region Properties

        /// <summary>
        /// 返回电池槽位的信息。
        /// </summary>
        public ReadOnlyCollection<BatteryInfo> BatterySlots { get; }
        
        /// <summary>
        /// 用于动画的电池槽位信息。
        /// </summary>
        public ReadOnlyCollection<BatteryInfo> ShadowBatterySlots { get; }

        #endregion

        #region Methods

        private void AddBatteryInfo(ReadOnlyCollection<BatteryInfo> targetSlotsList, BatteryInfo info)
        {
            // 如果最后一个槽位有电池，则抛异常。
            if (targetSlotsList[BATTERY_CAPACITY - 1].HasBattery)
                throw new InvalidOperationException("no empty slot to add battery.");

            // 整体后移一个槽位
            for (var i = BATTERY_CAPACITY - 1; i > 0; i--)
                targetSlotsList[i - 1].TransferInfoTo(targetSlotsList[i]);

            // 如果没有给定电池信息，则创建一个空槽位。
            if (info == null)
                info = new BatteryInfo();
            
            info.TransferInfoTo(targetSlotsList[0]);
        }

        private BatteryInfo DequeueBatteryInfo(ReadOnlyCollection<BatteryInfo> targetSlotsList)
        {
            // 如果所有槽位均没有电池，则抛异常。
            if (targetSlotsList.FirstOrDefault(s => s.HasBattery) == null)
                throw new InvalidOperationException("no battery in any slot.");

            // 保存第一个槽位的电池信息。
            var battInfo = new BatteryInfo();
            targetSlotsList.Last().TransferInfoTo(battInfo);

            // 整体前移一个槽位。
            for (var i = BATTERY_CAPACITY - 1; i > 0; i--)
                targetSlotsList[i - 1].TransferInfoTo(targetSlotsList[i]);

            // 清楚最后一个槽位电池信息。
            targetSlotsList[0].ClearInfo();

            return battInfo;
        }

        private BatteryInfo PopBatteryInfo(ReadOnlyCollection<BatteryInfo> targetSlotsList)
        {
            // 如果所有槽位均没有电池，则抛异常。
            if (targetSlotsList.FirstOrDefault(s => s.HasBattery) == null)
                throw new InvalidOperationException("no battery in any slot.");

            // 保存第一个槽位的电池信息。
            var battInfo = new BatteryInfo();
            targetSlotsList[0].TransferInfoTo(battInfo);

            // 整体前移一个槽位。
            for (var i = 0; i < BATTERY_CAPACITY - 1; i++)
                targetSlotsList[i + 1].TransferInfoTo(targetSlotsList[i]);

            // 清楚最后一个槽位电池信息。
            targetSlotsList.Last().ClearInfo();

            return battInfo;
        }
        

        protected override void BuildStoryboard()
        {
            Storyboards = new Dictionary<int, Storyboard>
            {
                { (int)Positions.Standby, FindResource("BeltToStandbyStoryboard") as Storyboard },
                { (int)Positions.AddOrDequeue, FindResource("BeltAddOrRemoveBatteryStoryboard") as Storyboard },
                { (int)Positions.Pop, FindResource("BeltPopBatteryStoryboard") as Storyboard }
            };
        }

        public void AddBattery(BatteryInfo info)
        {
            if (AnimationBusy)
                throw new InvalidOperationException("last operation is running.");
            
            // 当动画完成后，更新电池槽位状态。
            DoOnAnimationDone(()=>
            {
                // 更新静态显示的电池槽位。
                AddBatteryInfo(BatterySlots, info);
                
                CurrentPosition = (int)Positions.Standby;
                
                // 隐藏影子槽位
                BatterySlotsItemsControl.Visibility = Visibility.Visible;
                ShadowBatterySlotsItemsControl.Visibility = Visibility.Collapsed;
            });

            // 更新影子槽位信息，用于动画显示
            AddBatteryInfo(ShadowBatterySlots, (BatteryInfo)info.Clone());

            // 显示影子槽位用于动画显示
            ShadowBatterySlotsItemsControl.Margin = new Thickness(4, 2, 0, 0);
            BatterySlotsItemsControl.Visibility = Visibility.Collapsed;
            ShadowBatterySlotsItemsControl.Visibility = Visibility.Visible;
            
            
            CurrentPosition = (int)Positions.AddOrDequeue;
        }

        public BatteryInfo DequeueBattery()
        {
            if (AnimationBusy)
                throw new InvalidOperationException("last operation is running.");

            var info = new BatteryInfo();
            
            // 当动画完成后，更新电池槽位状态。
            DoOnAnimationDone(()=>
            {
                // 更新静态显示的电池槽位。
                info = DequeueBatteryInfo(BatterySlots);
                CurrentPosition = (int)Positions.Standby;

                // 隐藏影子槽位
                BatterySlotsItemsControl.Visibility = Visibility.Visible;
                ShadowBatterySlotsItemsControl.Visibility = Visibility.Collapsed;
                DequeueBatteryInfo(ShadowBatterySlots);
            });

            ShadowBatterySlotsItemsControl.Margin = new Thickness(86, 2, 0, 0);
            BatterySlotsItemsControl.Visibility = Visibility.Collapsed;
            ShadowBatterySlotsItemsControl.Visibility = Visibility.Visible;
           
            CurrentPosition = (int)Positions.AddOrDequeue;

            return info;
        }

        public BatteryInfo PopBattery()
        {
            if (AnimationBusy)
                throw new InvalidOperationException("last operation is running.");

            var info = new BatteryInfo();

            // 当动画完成后，更新电池槽位状态。
            DoOnAnimationDone(() =>
            {
                // 更新静态显示的电池槽位。
                info = PopBatteryInfo(BatterySlots);
                
                CurrentPosition = (int)Positions.Standby;

                // 隐藏影子槽位
                BatterySlotsItemsControl.Visibility = Visibility.Visible;
                ShadowBatterySlotsItemsControl.Visibility = Visibility.Collapsed;
                PopBatteryInfo(ShadowBatterySlots);
                
            });

            BatterySlotsItemsControl.Visibility = Visibility.Collapsed;
            ShadowBatterySlotsItemsControl.Visibility = Visibility.Visible;
            ShadowBatterySlotsItemsControl.Margin = new Thickness(86, 2, 0, 0);

            CurrentPosition = (int)Positions.Pop;

            return info;

        }

        /// <summary>
        /// 清除NG皮带线的电池信息。
        /// </summary>
        public void ClearBattery()
        {
            CurrentPosition = (int)Positions.Standby;
            for (var i = 0; i < BATTERY_CAPACITY; i++)
            {
                BatterySlots[i].ClearInfo();
                ShadowBatterySlots[i].ClearInfo();
            }
        }

        #endregion



    }
}
