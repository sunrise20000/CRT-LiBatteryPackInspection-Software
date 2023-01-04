using System;
using System.Windows.Controls;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Core
{
    public class BusyIndicateableUiViewModelBase : UiViewModelBase
    {
        #region Variables

        private bool _isBusy;
        private string _busyIndicatorMessage;

        #endregion

        #region Properties

        /// <summary>
        /// 返回ViewModel绑定的视图对象。
        /// </summary>
        public UserControl View { get; private set; }

        /// <summary>
        /// 设置或返回忙信息。
        /// </summary>
        public string BusyIndicatorContent
        {
            get => _busyIndicatorMessage;
            set
            {
                _busyIndicatorMessage = value;
                NotifyOfPropertyChange(nameof(BusyIndicatorContent));
            }
        }

        /// <summary>
        /// 设置或返回视图是否正忙。
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                NotifyOfPropertyChange(nameof(IsBusy));
            }
        }

        #endregion

        #region Methods

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            View = (UserControl)_view;
        }

        /// <summary>
        /// 取消操作。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void Cancel()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
