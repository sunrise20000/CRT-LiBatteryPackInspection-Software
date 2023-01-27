using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Crt.UiCore.RtCore
{
    /// <summary>
    /// 电池信息。
    /// </summary>
    [Serializable]
    [DataContract]
    public class BatteryInfo : INotifyPropertyChanged, ICloneable
    {

        #region Constructors

        public BatteryInfo()
        {
            
        }

        public BatteryInfo(PublicModuleNames module, string sn)
        {
           SetInfo(module, sn);
        }

        #endregion

        #region Properties

        private PublicModuleNames _currentModule;

        /// <summary>
        /// 设置或返回当前电池所在的模组。
        /// </summary>
        [DataMember]
        public PublicModuleNames CurrentModule
        {
            get => _currentModule;
            set => SetField(ref _currentModule, value);
        }

        private string _batterySn;

        /// <summary>
        /// 设置或返回电池条码。
        /// </summary>
        [DataMember]
        public string BatterySn
        {
            get => _batterySn;
            set => SetField(ref _batterySn, value);
        }

        private bool _hasBattery;

        /// <summary>
        /// 设置或返回是否有电池。
        /// </summary>
        [DataMember]
        public bool HasBattery
        {
            get => _hasBattery;
            set => SetField(ref _hasBattery, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 清除电池信息。
        /// </summary>
        public void ClearInfo()
        {
            HasBattery = false;
            BatterySn = string.Empty;
        }

        /// <summary>
        /// 设置电池信息。
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="sn"></param>
        public void SetInfo(PublicModuleNames moduleName, string sn)
        {
            CurrentModule = moduleName;
            BatterySn = sn;
            HasBattery = true;
        }

        /// <summary>
        /// 将当前电池信息传递到目标信息对象。
        /// </summary>
        /// <param name="targetInfo">待传递的目标信息对象。</param>
        /// <param name="isClearMe">是否清除我的信息。</param>
        public void TransferInfoTo(BatteryInfo targetInfo, bool isClearMe = true)
        {
            targetInfo.CurrentModule = CurrentModule;
            targetInfo.BatterySn = BatterySn;
            targetInfo.HasBattery = HasBattery;
            
            if(isClearMe)
                ClearInfo();
        }
        
        public object Clone()
        {
            var info = new BatteryInfo();
            TransferInfoTo(info, false);
            return info;
        }

        public override string ToString()
        {
            return $"{CurrentModule} - {BatterySn}";
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        
    }
}
