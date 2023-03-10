using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.CenterViews.Controls
{
    /// <summary>
    /// AITSCRow.xaml 的交互逻辑
    /// </summary>
    public partial class ScStringRow : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(ScStringRow),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }



        public static readonly DependencyProperty NameWidthProperty = DependencyProperty.Register(
            "NameWidth", typeof(int), typeof(ScStringRow),
            new FrameworkPropertyMetadata(180, FrameworkPropertyMetadataOptions.AffectsRender));
        public int NameWidth
        {
            get
            {
                return (int)this.GetValue(NameWidthProperty);
            }
            set
            {
                this.SetValue(NameWidthProperty, value);
            }
        }

        public static readonly DependencyProperty FeedbackWidthProperty = DependencyProperty.Register(
            "FeedbackWidth", typeof(int), typeof(ScStringRow),
            new FrameworkPropertyMetadata(90, FrameworkPropertyMetadataOptions.AffectsRender));
        public int FeedbackWidth
        {
            get
            {
                return (int)this.GetValue(FeedbackWidthProperty);
            }
            set
            {
                this.SetValue(FeedbackWidthProperty, value);
            }
        }

        public static readonly DependencyProperty SetPointWidthProperty = DependencyProperty.Register(
            "SetPointWidth", typeof(int), typeof(ScStringRow),
            new FrameworkPropertyMetadata(90, FrameworkPropertyMetadataOptions.AffectsRender));
        public int SetPointWidth
        {
            get
            {
                return (int)this.GetValue(SetPointWidthProperty);
            }
            set
            {
                this.SetValue(SetPointWidthProperty, value);
            }
        }

        public static readonly DependencyProperty CommandWidthProperty = DependencyProperty.Register(
            "CommandWidth", typeof(int), typeof(ScStringRow),
            new FrameworkPropertyMetadata(90, FrameworkPropertyMetadataOptions.AffectsRender));
        public int CommandWidth
        {
            get
            {
                return (int)this.GetValue(CommandWidthProperty);
            }
            set
            {
                this.SetValue(CommandWidthProperty, value);
            }
        }

        public static readonly DependencyProperty ScIdProperty = DependencyProperty.Register(
            "ScId", typeof(string), typeof(ScStringRow),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScId
        {
            get
            {
                return (string)this.GetValue(ScIdProperty);
            }
            set
            {
                this.SetValue(ScIdProperty, value);
            }
        }

        public static readonly DependencyProperty ScNameProperty = DependencyProperty.Register(
            "ScName", typeof(string), typeof(ScStringRow),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScName
        {
            get
            {
                return (string)this.GetValue(ScNameProperty);
            }
            set
            {
                this.SetValue(ScNameProperty, value);
            }
        }

        public static readonly DependencyProperty ScFeedbackProperty = DependencyProperty.Register(
            "ScFeedback", typeof(string), typeof(ScStringRow),
            new FrameworkPropertyMetadata("127.0.0.1", FrameworkPropertyMetadataOptions.AffectsRender));
        public double ScFeedback
        {
            get
            {
                return (double)this.GetValue(ScFeedbackProperty);
            }
            set
            {
                this.SetValue(ScFeedbackProperty, value);
            }
        }

        public static readonly DependencyProperty ScSetPointProperty = DependencyProperty.Register(
            "ScSetPoint", typeof(string), typeof(ScStringRow),
            new FrameworkPropertyMetadata("127.0.0.1", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScSetPoint
        {
            get
            {
                return (string)this.GetValue(ScSetPointProperty);
            }
            set
            {
                this.SetValue(ScSetPointProperty, value);
            }
        }


        public ScStringRow()
        {
            InitializeComponent();
        }
 

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Command.Execute(new object[] { ScId, ScSetPoint.ToString()});
        }
    }
}
