using MECF.Framework.Common.Fsm;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.Core
{
    public abstract class OfflineTimeoutNotifiableModuleBase : ModuleFsmDevice
    {
        #region Variables

        private CancellationTokenSource _notifyModuleOfflineCancellation;

        #endregion

        #region Constructors

        public OfflineTimeoutNotifiableModuleBase()
        {
            IsStayOfflineTimeout = false;
            _notifyModuleOfflineCancellation = new CancellationTokenSource();
        }

        #endregion

        #region MyRegion

        

        #endregion
        public bool IsStayOfflineTimeout { get; protected set; }

        #region Methods

        public override bool Initialize()
        {
            OP.Subscribe($"{Module}.SetOnline", (string cmd, object[] args) => PutOnline());
            OP.Subscribe($"{Module}.SetOffline", (string cmd, object[] args) => PutOffline());
            
            DATA.Subscribe($"{Module}.IsOfflineTimeout", ()=> IsStayOfflineTimeout);
            
            return base.Initialize();
        }

        protected virtual bool PutOnline()
        {
            IsStayOfflineTimeout = false;
            IsOnline = true;
            _notifyModuleOfflineCancellation.Cancel();
            return true;
        }

        protected virtual bool PutOffline()
        {
            // 是否在Auto模式，仅Auto模式启用超时机制
            var sta = DATA.Poll("Rt.Status").ToString();
            
            IsOnline = false;
            IsStayOfflineTimeout = false;

            if (sta != "AutoRunning" && sta != "AutoIdle")
                return true;
            
            // 如果是Auto模式，启用超时机制。
            var timeoutSec = SC.GetValue<int>("System.ModuleOfflineTimeout");
            if (timeoutSec <= 0)
                timeoutSec = 0;

            if (timeoutSec > 0)
            {
                _notifyModuleOfflineCancellation?.Dispose();
                _notifyModuleOfflineCancellation = new CancellationTokenSource();
                NotifyModuleOfflineTask(timeoutSec * 1000);
            }

            return true;
        }

        private Task NotifyModuleOfflineTask(int delayTime)
        {
            var t1 = Task.Run(async () =>
            {
                await Task.Delay(delayTime, _notifyModuleOfflineCancellation.Token).ContinueWith(
                    x =>
                    {
                        IsStayOfflineTimeout = true;
                        EV.PostWarningLog(Module, $"{Module} offline timeout");
                    },
                    _notifyModuleOfflineCancellation.Token);
            });
            return t1;
        }
        
        public void InvokeOffline()
        {
            PutOffline();
        }

        public void InvokeOnline()
        {
            PutOnline();
        }
        
        #endregion
    }
}
