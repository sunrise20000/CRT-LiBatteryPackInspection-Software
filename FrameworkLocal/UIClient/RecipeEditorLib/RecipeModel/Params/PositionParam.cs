using System.Collections.Generic;
using System.Linq;
using System.Windows;

using System.Collections.ObjectModel;
using RecipeEditorLib.DGExtension.CustomColumn;


namespace RecipeEditorLib.RecipeModel.Params
{
    public class PositionParam : ParamBaseWithGenericValue<string>
    {
        public override string Value
        {
            get => _value;
            set
            {
                _value = value;
                OptionChanged(value);
                NotifyOfPropertyChange(nameof(Value));
            }
        }
        public ObservableCollection<ComboxColumn.Option> Options { get; set; }

        private void OptionChanged(string value)
        {
            IEnumerable<ComboxColumn.Option> opts = Options.Where(op => op.ControlName == value);

            if (opts.Count() > 0)
            {
                string[] relatedparams = opts.ToList()[0].RelatedParameters.Split(',');
                Parent.ToList().ForEach(param =>
                {
                    if (relatedparams.Contains(param.Name) || param.Name == "Position" || param.Name == "StepNo" || param.Name == "Module")
                        param.Visible = Visibility.Visible;
                    else
                        param.Visible = Visibility.Hidden;
                });
            }
            else
            {
                Parent.ToList().ForEach(param =>
                {
                    if (param.Name == "Position" || param.Name == "StepNo" || param.Name == "Module")
                        param.Visible = Visibility.Visible;
                    else
                        param.Visible = Visibility.Hidden;
                });
            }
        }
    }
}
