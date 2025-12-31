using System;
using System.Windows;
using HotelPOS.Wpf.Data;

namespace HotelPOS.Wpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                SQLitePCL.Batteries_V2.Init();
                SqliteDb.EnsureDatabase();

                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Startup error");
                Shutdown(-1);
            }
        }
    }
}
