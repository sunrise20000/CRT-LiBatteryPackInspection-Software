using System;
using System.Collections.Generic;
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
            AddOrRemove,  //Add和Remove时皮带线的滚动方向相同，仅电池的显示状态不同， 因此使用同一动画。

            /// <summary>
            /// 从入料口弹出一节电池
            /// </summary>
            Pop
        }

        /// <summary>
        /// 皮带线电池容量。
        /// </summary>
        private const int BATTERY_CAPACITY = 6;
        
        /// <summary>
        /// 电池槽位，维护电池信息。
        /// </summary>
        private readonly List<BatteryInfo> _batterySlots = new List<BatteryInfo>();



        public NgConveyorBelt()
        {
            InitializeComponent();

            for (var i = 0; i < BATTERY_CAPACITY; i++)
                _batterySlots.Add(new BatteryInfo() { HasBattery = true});
        }

        #region Properties

        /// <summary>
        /// 返回电池槽位的信息。
        /// </summary>
        public List<BatteryInfo> BatterySlots => _batterySlots;

        #endregion

        #region Methods

        protected override void BuildStoryboard()
        {
            Storyboards = new Dictionary<int, Storyboard>
            {
                { (int)Positions.Standby, FindResource("BeltToStandbyStoryboard") as Storyboard },
                { (int)Positions.AddOrRemove, FindResource("BeltAddOrRemoveBatteryStoryboard") as Storyboard },
                { (int)Positions.Pop, FindResource("BeltPopBatteryStoryboard") as Storyboard }
            };
        }

        public void AddBattery()
        {
            CurrentPosition = (int)Positions.AddOrRemove;
            var slot = BatterySlots.FirstOrDefault(s => s.HasBattery == false);
            if (slot == null)
                throw new InvalidOperationException("Slots are full.");

            slot.HasBattery = true;
            
        }

        public void RemoveBattery()
        {
            CurrentPosition = (int)Positions.AddOrRemove;
        }

        public void PopBattery()
        {
            CurrentPosition = (int)Positions.Pop;
        }

        #endregion



    }
}
