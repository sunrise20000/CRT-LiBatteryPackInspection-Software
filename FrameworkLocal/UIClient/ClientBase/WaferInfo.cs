using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferInfo : PropertyChangedBase
    {
        private int _WaferStatus = 0;   // WaferStatus.Empty;
        public int WaferStatus
        {
            get { return _WaferStatus; }
            set { _WaferStatus = value; NotifyOfPropertyChange("WaferStatus"); }
        }

        private int _WaferTrayStatus = 0;
        public int WaferTrayStatus
        {
            get { return _WaferTrayStatus; }
            set { _WaferTrayStatus = value; NotifyOfPropertyChange("WaferTrayStatus"); }
        }

        private int _TrayProcessCount = 0;
        public int TrayProcessCount
        {
            get { return _TrayProcessCount; }
            set { _TrayProcessCount = value; NotifyOfPropertyChange("TrayProcessCount"); }
        }

        private string _RecipeName;
        public string RecipeName
        {
            get { return _RecipeName; }
            set { _RecipeName = value; NotifyOfPropertyChange("RecipeName"); }
        }

        private bool _isTrayExhausted;
        public bool IsTrayExhausted
        {
            get { return _isTrayExhausted; }
            set { _isTrayExhausted = value; NotifyOfPropertyChange("IsTrayExhausted"); }
        }

        /// <summary>
        /// SlotID start from 0
        /// </summary>
        private int _slotID;
        public int SlotID
        {
            get { return _slotID; }
            set { _slotID = value; NotifyOfPropertyChange("SlotID"); }
        }

        /// <summary>
        /// SlotIndex start from 1
        /// </summary>
        private int _slotIndex;
        public int SlotIndex
        {
            get { return _slotIndex; }
            set { _slotIndex = value; NotifyOfPropertyChange("SlotIndex"); }    
        }

        private string _moduleID;
        public string ModuleID
        {
            get { return _moduleID; }
            set { _moduleID = value; NotifyOfPropertyChange("ModuleID"); }
        }

        private string _waferid;
        public string WaferID
        {
            get { return _waferid; }
            set { _waferid = value; NotifyOfPropertyChange("WaferID"); }
        }

        private string _sourceName;
        public string SourceName
        {
            get { return _sourceName; }
            set { _sourceName = value; NotifyOfPropertyChange("SourceName"); }
        }

        private string _lotID;
        public string LotID
        {
            get { return _lotID; }
            set { _lotID = value; NotifyOfPropertyChange("LotID"); }
        }

        private string _sequenceName = string.Empty;
        public string SequenceName
        {
            get { return _sequenceName; }
            set { _sequenceName = value; NotifyOfPropertyChange("SequenceName"); }
        }

        private string _originName = string.Empty;
        public string OriginName
        {
            get { return _originName; }
            set { _originName = value; NotifyOfPropertyChange("OriginName"); }
        }

        private ToolTip _toolTip;
        public ToolTip ToolTip
        {
            get { return _toolTip; }
            set { _toolTip = value; NotifyOfPropertyChange("ToolTip"); }
        }

        private bool isChecked;
        public bool IsChecked
        {
            get
            { return isChecked; }
            set
            {
                isChecked = value;
                NotifyOfPropertyChange("IsChecked");
            }
        }

        public Visibility IsVisibility
        {
            get
            { return _WaferStatus > 0 ? Visibility.Visible : Visibility.Hidden; }
        }
    }
}
