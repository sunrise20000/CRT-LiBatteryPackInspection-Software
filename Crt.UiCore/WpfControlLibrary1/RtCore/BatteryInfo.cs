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
    public class BatteryInfo : INotifyPropertyChanged
    {
        #region

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
