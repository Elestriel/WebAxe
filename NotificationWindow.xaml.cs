using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Webex_Killer
{
    /// <summary>
    /// Interaction logic for Notification.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public event Action<int> Check;
        private DispatcherTimer ToastTimer;
        private int TimeRemaining { get; set; }

        public NotificationWindow()
        {
            InitializeComponent();
        }

        public void Show(string MessageText)
        {
            Console.WriteLine("Showing NotificatioNWindow");

            TimeRemaining = 15;

            ToastTimer = new DispatcherTimer();
            ToastTimer.Tick += ToastTimer_Tick;
            ToastTimer.Interval = new TimeSpan(0, 0, 1);
            ToastTimer.Start();

            Rect desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 10;
            Top = desktopWorkingArea.Bottom - Height - 10;
            
            NotificationText.Content = MessageText;
            Topmost = true;
            Show();
        }

        public void Kill()
        {
            ToastTimer.Stop();
            ToastTimer = null;
        }

        public void HideMe()
        {
            ToastTimer = null;
            Hide();
        }

        private void ToastTimer_Tick(object sender, EventArgs e)
        {
            if (TimeRemaining <= 0)
            {
                Check(1);
                HideMe();
            }
            else
            {
                CountdownText.Content = $"Snoozing in {TimeRemaining} Second{(TimeRemaining == 1 ? "" : "s")}";
                TimeRemaining--;
            }

        }

        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            Check(2);
            HideMe();
        }

        private void KillButton_Click(object sender, RoutedEventArgs e)
        {
            Check(3);
            HideMe();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }
    }
}