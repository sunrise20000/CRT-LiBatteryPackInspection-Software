using System;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class PathFileParam : ParamBaseWithGenericValue<string>
    {

        public override string Value
        {
            get => _value;
            set
            {
                _value = value;
                Feedback?.Invoke(this);

                if (string.IsNullOrEmpty(_value))
                {
                    FileName = "";
                }
                else
                {
                    var index = _value.LastIndexOf("\\", StringComparison.Ordinal);
                    FileName = index > -1 ? _value.Substring(index + 1) : _value;
                }

                NotifyOfPropertyChange(nameof(Value));
            }
        }

        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                NotifyOfPropertyChange(nameof(FileName));
            }
        }

        private string _prefixPath;
        public string PrefixPath
        {
            get { return _prefixPath; }
            set
            {
                _prefixPath = value;
                NotifyOfPropertyChange(nameof(PrefixPath));
            }
        }
    }
}
