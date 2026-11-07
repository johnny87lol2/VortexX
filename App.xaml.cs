using System.Windows;
using System.Windows.Threading;

namespace VortexX
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            DispatcherUnhandledException += OnUnhandled;
        }

        void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "VortexX");
            e.Handled = true;
        }
    }
}
