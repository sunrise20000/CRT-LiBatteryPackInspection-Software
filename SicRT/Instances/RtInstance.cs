using System;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SicRT.Instances;
using MECF.Framework.RT.Core.Applications;

namespace SorterRT.Modules
{
    internal class RtInstance : IRtInstance
    {

        #region Variables

        /// <summary>
        /// CVD系统名称
        /// </summary>
        public const string SYSTEM_NAME = "Sic";

        /// <summary>
        /// CVD 数据库名称
        /// </summary>
        public const string DATABASE_NAME = "sicdb";

        /// <summary>
        /// 
        /// </summary>
        public const string DEVICE_MODEL_FILE_NAME = "DeviceModelSic.xml";

        /// <summary>
        /// 获取当前Assembly版本。
        /// </summary>
        public static readonly Version SystemVersion;

        #endregion
       
        #region Constructors


        public RtInstance()
        {
            TrayIcon = new BitmapImage(
                new Uri("pack://application:,,,/MECF.Framework.UI.Core;component/Resources/Logos/MyLogoTray.ico"));
            
            Loader = new ToolLoader();
        }


        static RtInstance()
        {
            // get assembly version from 
            SystemVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        #endregion

        #region Properties

        public string SystemName => SYSTEM_NAME;

        public bool EnableNotifyIcon => true;

        public bool KeepRunningAfterUnknownException => false;

        public ImageSource TrayIcon { get; }

        public bool DefaultShowBackendWindow => false;

        public IRtLoader Loader { get; }

        public string DatabaseName => DATABASE_NAME;


        #endregion

    }
}
