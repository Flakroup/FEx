using FEx.Agnostics.BaseObjects;

namespace FEx.MVVM.Controls
{
    public class CheckedListItem : NotifyPropertyChanged
    {
        private bool _isChecked;
        private string _name;
        private bool _isEnabled;
        private object _tooltip;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public object ToolTip
        {
            get => _tooltip;
            set => SetProperty(ref _tooltip, value);
        }

        public CheckedListItem()
        {
        }

        public CheckedListItem(string name, bool isEnabled = true, bool isChecked = false, object toolTip = null)
        {
            _tooltip = toolTip;
            _isChecked = isChecked;
            _name = name;
            _isEnabled = isEnabled;
        }
    }
}