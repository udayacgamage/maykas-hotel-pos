using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelPOS.Wpf.Models
{
    public sealed class BillItem : INotifyPropertyChanged
    {
        private string _itemName = "";
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value ?? ""; OnPropertyChanged(); }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value < 0 ? 0 : value;
                OnPropertyChanged();
            }
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPropertyChanged(); }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
