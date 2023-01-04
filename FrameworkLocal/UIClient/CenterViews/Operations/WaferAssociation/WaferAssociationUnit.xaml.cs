using System.Globalization;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ExtendedGrid.Microsoft.Windows.Controls;
using MECF.Framework.UI.Client.ClientBase;


namespace MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation
{

    #region Converters

    internal class WaferInfoToPjWaferListContent : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: waferStatus:int
            // values[1]: sequence
            // values[2]: wafer id

            if (values.Length == 3 && values[0] is int waferStatus && values[1] is string seqName &&
                values[2] is string waferId)
            {
                if (waferStatus == (int)WaferStatus.Empty)
                    return null;

                // 显示的Wafer ID不要超过7个字符
                var shortWaferId = waferId.Length <= 10
                    ? waferId
                    : $"{waferId.Substring(0, 3)}...{waferId.Substring(waferId.Length - 7, 7)}";
                return $"[{shortWaferId}] {seqName}";
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class WaferInfoToPjWaferListTooltip : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: waferStatus:int
            // values[1]: sequence
            // values[2]: wafer id

            if (values.Length == 3 && values[0] is int waferStatus && values[1] is string seqName &&
                values[2] is string waferId)
            {
                if (waferStatus == (int)WaferStatus.Empty)
                    return null;
                
                return $"Wafer ID.:{waferId}\r\nSequence: {seqName}";
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion




    /// <summary>
    /// Interaction logic for WaferAssociationUnit.xaml
    /// </summary>
    public partial class WaferAssociationUnit : UserControl
    {
        public WaferAssociationUnit()
        {
            InitializeComponent();
        }

        public WaferAssociationInfo WAInfo
        {
            get { return (WaferAssociationInfo)GetValue(WAInfoProperty); }
            set { SetValue(WAInfoProperty, value); }
        }
        public static readonly DependencyProperty WAInfoProperty = DependencyProperty.Register("WAInfo", typeof(WaferAssociationInfo), typeof(WaferAssociationUnit), new UIPropertyMetadata(null));
    }
}
