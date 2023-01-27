using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            for (var i = 0; i < BATTERY_CAPACITY; i++)
                battInfoList.Add(new BatteryInfo());

            BatterySlots = new ReadOnlyCollection<BatteryInfo>(battInfoList);
        }

        #region Properties

        /// <summary>
        /// 返回电池槽位的信息。
        /// </summary>
        public ReadOnlyCollection<BatteryInfo> BatterySlots { get; }

        #endregion

        #region Methods

        private void AddBatteryInfo(BatteryInfo info)
        {
            // 如果最后一个槽位有电池，则抛异常。
            if (BatterySlots[BATTERY_CAPACITY - 1].HasBattery)
                throw new InvalidOperationException("no empty slot to add battery.");

            // 整体后移一个槽位
            for (var i = BATTERY_CAPACITY - 1; i > 0; i--)
                BatterySlots[i - 1].TransferInfoTo(BatterySlots[i]);

            // 如果没有给定电池信息，则创建一个空槽位。
            if (info == null)
                info = new BatteryInfo();
            
            info.TransferInfoTo(BatterySlots[0]);
        }

        private BatteryInfo DequeueBatteryInfo()
        {
            // 如果所有槽位均没有电池，则抛异常。
            if (BatterySlots.FirstOrDefault(s => s.HasBattery) == null)
                throw new InvalidOperationException("no battery in any slot.");

            // 保存第一个槽位的电池信息。
            var battInfo = new BatteryInfo();
            BatterySlots.Last().TransferInfoTo(battInfo);

            // 整体前移一个槽位。
            for (var i = BATTERY_CAPACITY - 1; i > 0; i--)
                BatterySlots[i - 1].TransferInfoTo(BatterySlots[i]);

            // 清楚最后一个槽位电池信息。
            BatterySlots[0].ClearInfo();

            return battInfo;
        }

        private BatteryInfo PopBatteryInfo()
        {
            // 如果所有槽位均没有电池，则抛异常。
            if (BatterySlots.FirstOrDefault(s => s.HasBattery) == null)
                throw new InvalidOperationException("no battery in any slot.");

            // 保存第一个槽位的电池信息。
            var battInfo = new BatteryInfo();
            BatterySlots[0].TransferInfoTo(battInfo);

            // 整体前移一个槽位。
            for (var i = 0; i < BATTERY_CAPACITY - 1; i++)
                BatterySlots[i + 1].TransferInfoTo(BatterySlots[i]);

            // 清楚最后一个槽位电池信息。
            BatterySlots.Last().ClearInfo();

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
            // 当动画完成后，更新电池槽位状态。
            DoOnAnimationDone(()=>
            {
                CurrentPosition = (int)Positions.Standby;
                AddBatteryInfo(info);
                
            });
            CurrentPosition = (int)Positions.AddOrDequeue;
        }

        public BatteryInfo DequeueBattery()
        {
            var info = new BatteryInfo();
            
            // 当动画完成后，更新电池槽位状态。
            DoOnAnimationDone(()=>
            {
                CurrentPosition = (int)Positions.Standby;
                info = DequeueBatteryInfo();
                
            });
            CurrentPosition = (int)Positions.AddOrDequeue;

            return info;
        }

        public BatteryInfo PopBattery()
        {
            var info = new BatteryInfo();

            // 当动画完成后，更新电池槽位状态。
            DoOnAnimationDone(() =>
            {
                CurrentPosition = (int)Positions.Standby;
                info = PopBatteryInfo();
                
            });
            CurrentPosition = (int)Positions.Pop;

            return info;

        }

        #endregion



    }
}
