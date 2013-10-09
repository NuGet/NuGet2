using System.ComponentModel;

namespace NuGet.WebMatrix.Data
{
    /// <summary>
    /// An entry in a VirtualizingList
    /// </summary>
    public class VirtualizingListEntry : INotifyPropertyChanged
    {
        private object _item;

        public object Item
        {
            get
            {
                return this._item;
            }

            set
            {
                if (this._item != value)
                {
                    this._item = value;

                    var handler = this.PropertyChanged;
                    if (handler != null)
                    {
                        handler(this, new PropertyChangedEventArgs("Item"));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            if (Item != null)
            {
                return Item.ToString();
            }

            return base.ToString();
        }
    }
}
