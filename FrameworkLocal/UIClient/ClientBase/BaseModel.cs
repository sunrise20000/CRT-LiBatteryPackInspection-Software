using Caliburn.Micro.Core;
using IEventAggregator = Caliburn.Micro.Core.IEventAggregator;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class BaseModel : Screen
    {
        protected readonly IEventAggregator _eventAggregator;

        public BaseModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator?.Subscribe(this);
        }

        public PageID Page { get; set; }
 
        private int permission = 0;
        private int token = 0;

        public int Permission
        {
            get { return this.permission; }
            set
            {
                if (this.permission != value)
                {
                    this.permission = value;
                    this.NotifyOfPropertyChange("Permission");
                }
            }
        }

        public bool HasToken { get { return this.token > 0 ? true : false; } }

        public int Token
        {
            get { return this.token; }
            set
            {
                if (this.token != value)
                {
                    this.token = value;
                    OnTokenChanged(this.token);
                    this.NotifyOfPropertyChange("Token");
                }
            }
        }

        protected virtual void OnTokenChanged(int nNewToken) { }

        protected override void OnActivate()
        {
            base.OnActivate();
         }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
         }
    }
}
