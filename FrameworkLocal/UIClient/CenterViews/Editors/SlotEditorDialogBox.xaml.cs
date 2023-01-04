using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;

namespace MECF.Framework.UI.Client.CenterViews.Editors
{

    /// <summary>
    /// Interaction logic for InputDialogBox.xaml
    /// </summary>
    public partial class SlotEditorDialogBox : Window
    {
        public SlotEditorDialogBox(int waferCount, int trayCount)
        {
            InitializeComponent();
            
            if (waferCount <= 0)
            {
                txtTitleWaferID.Visibility = Visibility.Collapsed;
                txtWaferID.Visibility = Visibility.Collapsed;

                txtTitleRecipe.Visibility = Visibility.Collapsed;
                txtRecipeName.Visibility = Visibility.Collapsed;
                btnSelectRecipe.Visibility = Visibility.Collapsed;
            }

            if (trayCount <= 0)
            {
                txtTitleTrayProcCount.Visibility = Visibility.Collapsed;
                txtTrayProcessCount.Visibility = Visibility.Collapsed;
            }

            DataContext = this;
        }

        public static readonly DependencyProperty ModuleIDProperty = DependencyProperty.Register(
                               "ModuleID", typeof(string), typeof(SlotEditorDialogBox),
                               new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty RecipeNameProperty = DependencyProperty.Register(
                                "RecipeName", typeof(string), typeof(SlotEditorDialogBox),
                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TrayProcessCountProperty = DependencyProperty.Register(
                             "TrayProcessCount", typeof(int), typeof(SlotEditorDialogBox),
                             new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SlotIDProperty = DependencyProperty.Register(
                             "SlotID", typeof(int), typeof(SlotEditorDialogBox),
                             new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty WaferIDProperty = DependencyProperty.Register(
            nameof(WaferID), typeof(string), typeof(SlotEditorDialogBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public string  WaferID
        {
            get => (string )GetValue(WaferIDProperty);
            set => SetValue(WaferIDProperty, value);
        }


        public string ModuleID
        {
            get => (string)GetValue(ModuleIDProperty);
            set => SetValue(ModuleIDProperty, value);
        }

        public int TrayProcessCount
        {
            get => (int)GetValue(TrayProcessCountProperty);
            set => SetValue(TrayProcessCountProperty, value);
        }

        public string RecipeName
        {
            get => (string)GetValue(RecipeNameProperty);
            set => SetValue(RecipeNameProperty, value);
        }

        public int SlotID
        {
            get => (int)GetValue(SlotIDProperty);
            set => SetValue(SlotIDProperty, value);
        }


        private void RecipeSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new RecipeSelectDialogViewModel
                {
                    DisplayName = "Select Recipe"
                };
                var wm = new WindowManager();
                var bret = wm.ShowDialog(dialog);
                if (bret == true)
                {
                    RecipeName = dialog.DialogResult;
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }


        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int processCount = TrayProcessCount;
                Int32.TryParse(txtTrayProcessCount.Text, out processCount);

                var waferId = txtWaferID.Text.Trim();
                if (string.IsNullOrEmpty(waferId))
                    waferId = WaferID;

                InvokeClient.Instance.Service.DoOperation("AlterWaferInfo", ModuleID, SlotID, waferId, RecipeName, processCount);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }


        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            txtWaferID.Text = WaferID;
            txtRecipeName.Text = RecipeName;
            txtTrayProcessCount.Text = TrayProcessCount.ToString();

            base.OnRender(drawingContext);
        }
    }
}
