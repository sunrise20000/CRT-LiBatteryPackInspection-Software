using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SicUI.Controls
{
    /// <summary>
    /// Reactor.xaml 的交互逻辑
    /// </summary>
    public partial class Reactor : UserControl
    {
        public static readonly DependencyProperty IsVisyProperty = DependencyProperty.Register(
                    "IsVisy", typeof(Visibility), typeof(Reactor),
                    new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

        public Visibility IsVisy
        {
            get
            {
                return (Visibility)this.GetValue(IsVisyProperty);
            }
            set
            {
                this.SetValue(IsVisyProperty, value);
            }
        }

        public static readonly DependencyProperty UpStatusProperty = DependencyProperty.Register(
                   "UpStatus", typeof(string), typeof(Reactor),
                   new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public string UpStatus
        {
            get
            {
                return (string)this.GetValue(UpStatusProperty);
            }
            set
            {
                this.SetValue(UpStatusProperty, value);
            }
        }
        public Reactor()
        {
            InitializeComponent();
        }
    }
}
