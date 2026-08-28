using System;
using System.Windows;
using OpticalTransfer.UI;

namespace OpticalTransfer
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application app = new Application();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }
}
