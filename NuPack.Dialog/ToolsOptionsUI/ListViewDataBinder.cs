using System;
using System.Collections;
using System.Windows.Forms;

namespace NuGet.Dialog.ToolsOptionsUI {

    internal class ListViewDataBinder<T> {

        private ListView _listView;
        private Func<T, string[]> _itemBuilder;
        private Action<T, ListViewItem> _onItemBound;

        public ListViewDataBinder(ListView listView, Func<T, string[]> itemBuilder, Action<T, ListViewItem> onItemBound) {
            _listView = listView;
            _itemBuilder = itemBuilder;
            _onItemBound = onItemBound;
        }

        public object DataSource {
            get;
            set;
        }

        public void Bind() {
            _listView.Items.Clear();
            var list = (IEnumerable)DataSource;
            foreach (T o in list) {
                var item = _listView.Items.Add(new ListViewItem(_itemBuilder(o)));
                item.Tag = o;
                if (_onItemBound != null) {
                    _onItemBound(o, item);
                }
            };
        }
    }
}
