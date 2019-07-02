using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System_Programming_Final_Task.Helper
{
    public class Notification
    {
        private readonly NotifyIcon _notifyIcon;

        public Notification()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            _notifyIcon.BalloonTipClosed += (s, e) => _notifyIcon.Visible = false;
        }

        public void ShowNotification(string HeaderMessage, string MainMessage, ToolTipIcon Icon, int Timeout = 3000)
        {
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(0, HeaderMessage, MainMessage, Icon);
            Thread.Sleep(Timeout);
            _notifyIcon.Dispose();
        }

    }
}
