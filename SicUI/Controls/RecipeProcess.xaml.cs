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
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class RecipeProcess : UserControl
    {
        public RecipeProcess()
        {
            InitializeComponent();
        }
       
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(string), typeof(RecipeProcess),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public string DeviceData
        {
            get
            {
                return (string)this.GetValue(DeviceDataProperty);
            }
            set
            {
               
                this.SetValue(DeviceDataProperty, value);
            }
        }


        public static readonly DependencyProperty RecipeDataProperty = DependencyProperty.Register(
                      "RecipeData", typeof(float[]), typeof(RecipeProcess),
                      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public float[] RecipeData
        {
            get
            {
                return (float[])this.GetValue(RecipeDataProperty);
            }
            set
            {

                this.SetValue(RecipeDataProperty, value);
            }
        }


        public static readonly DependencyProperty heightProperty = DependencyProperty.Register(
                       "height", typeof(double), typeof(RecipeProcess));

        public double height
        {
            get
            {
                return (double)this.GetValue(heightProperty);
            }
            set
            {
                this.SetValue(heightProperty, value);
            }
        }

        public static readonly DependencyProperty widthProperty = DependencyProperty.Register(
                      "width", typeof(double), typeof(RecipeProcess));
                     // new FrameworkPropertyMetadata(null , FrameworkPropertyMetadataOptions.AffectsRender));

        public double width
        {
            get
            {
                return (double)this.GetValue(widthProperty);
            }
            set
            {
                this.SetValue(widthProperty, value);
            }
        }

        //public static readonly DependencyProperty RowNumProperty = DependencyProperty.Register(
        //               "RowNum", typeof(int), typeof(UserControl1),
        //               new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        //public int RowNum
        //{
        //    get
        //    {
        //        return (int)this.GetValue(DeviceDataProperty);
        //    }
        //    set
        //    {
        //        this.SetValue(DeviceDataProperty, value);
        //    }
        //}
        //public static readonly DependencyProperty ColumnNumProperty = DependencyProperty.Register(
        //              "ColumnNum", typeof(int), typeof(UserControl1),
        //              new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        //public int ColumnNum
        //{
        //    get
        //    {
        //        return (int)this.GetValue(DeviceDataProperty);
        //    }
        //    set
        //    {
        //        this.SetValue(DeviceDataProperty, value);
        //    }
        //}

        //public static readonly DependencyProperty RowSpanNumProperty = DependencyProperty.Register(
        //              "RowSpan", typeof(int), typeof(UserControl1),
        //              new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        //public int RowSpan
        //{
        //    get
        //    {
        //        return (int)this.GetValue(DeviceDataProperty);
        //    }
        //    set
        //    {
        //        this.SetValue(DeviceDataProperty, value);
        //    }
        //}
    }
}
