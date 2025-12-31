using System.Windows;
using HotelPOS.Wpf.ViewModels;
using MahApps.Metro.Controls;

namespace HotelPOS.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
