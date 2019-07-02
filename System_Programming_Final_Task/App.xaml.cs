using System.Threading;
using System.Windows;
using System_Programming_Final_Task.Helper;

namespace System_Programming_Final_Task
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "MUTEX";
        private bool _firstApplicationInstance;
        private Mutex _mutexApp;
        public Notification notification = new Notification();
        public App()
        {
            if (!IsApplicationFirstInstance())
            {
                notification.ShowNotification("Open Instance Error","There is open instance of this app, please close it before", System.Windows.Forms.ToolTipIcon.Warning,5000);
                Application.Current.Shutdown();
            }
        }

        private bool IsApplicationFirstInstance()
        {
            if (_mutexApp == null)
            {
                _mutexApp = new Mutex(true, MutexName, out _firstApplicationInstance);
            }
            return _firstApplicationInstance;
        }
    }
}
