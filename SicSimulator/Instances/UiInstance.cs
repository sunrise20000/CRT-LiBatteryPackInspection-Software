using System;
using System.Windows.Media;
using Aitex.Common.Util;
using Aitex.Core.WCF;
using SicSimulator.Views;
using MECF.Framework.UI.Core.Applications;

namespace SicSimulator.Instances
{
    class UiInstance : IUiInstance
    {
        public string LayoutFile => PathManager.GetCfgDir() + "UILayout.xml";

        public string SystemName => "Simulator";

        public bool EnableAccountModule => false;

        public ImageSource MainIcon { get; set; }
        public bool MaxSizeShow { get; } = true;

        public UiInstance()
        {

            //MainIcon = new BitmapImage(new Uri("pack://application:,,,/SicRT;component/Resources/defaultRT.ico"));
        }
    }
}
