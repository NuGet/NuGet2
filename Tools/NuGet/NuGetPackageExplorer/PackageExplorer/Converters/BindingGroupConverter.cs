using System;
using System.Collections.Generic;
using System.Windows.Data;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {
    public class BindingGroupConverter : IValueConverter {
        private static readonly Dictionary<BindingGroup, IBindingGroup> _bindingGroupCache = 
            new Dictionary<BindingGroup, IBindingGroup>();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            BindingGroup group = (BindingGroup)value;
            if (group == null) {
                return group;
            }

            IBindingGroup result;
            if (!_bindingGroupCache.TryGetValue(group, out result)) {
                result = new BindingGroupWrapper(group);
                _bindingGroupCache.Add(group, result);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}