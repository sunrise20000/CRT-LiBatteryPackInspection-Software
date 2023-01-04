using System.Windows.Controls;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using DataGridBeginningEditEventArgs = ExtendedGrid.Microsoft.Windows.Controls.DataGridBeginningEditEventArgs;

namespace SicUI.Models.PMs
{
    /// <summary>
    /// PMProcessView.xaml 的交互逻辑
    /// </summary>
    public partial class PMProcessView : UserControl
    {
        public PMProcessView()
        {
            InitializeComponent(); 
        }

        private void DgCustom_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // 禁止编辑不显示数值的Cell，或已经完成工艺的Step
            if (e.Row.Item is RecipeStep step)
            {
                var param = step[e.Column.DisplayIndex];
                if (e.Column.IsReadOnly || param.IsHideValue || param.Parent.IsProcessed)
                    e.Cancel = true;
            }
        }
    }
}
