using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Webex_Killer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool AppRunning { get; set; }
        private NotificationWindow NotificationToast = new NotificationWindow();
        private DispatcherTimer SnoozeTimer;
        private bool IsSnoozed { get; set; }
        private List<Process> BadProcesses = null;
        private Thread WatcherThread;

        // System Tray
        private System.Windows.Forms.NotifyIcon SystemTrayIcon { get; set; }
        private ContextMenu SystemTrayContextMenu { get; set; }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) 
            { 
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        #region Initialization and Destruction
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;

            AutomaticallyDestroyProcesses.SetBinding(CheckBox.IsCheckedProperty, 
                new Binding("AutoKillProcesses") { Source = Properties.Settings.Default }
            );
            WatchProcessesCheckbox.SetBinding(CheckBox.IsCheckedProperty, 
                new Binding("WatchProcesses") { Source = Properties.Settings.Default }
            );

            PrepareSystemTray();

            // Subscribe to NotificationToast return values
            NotificationToast.Check += (value => ToastActionTaken(value));

            IsSnoozed = false;
        }

        private void PrepareSystemTray()
        {
            // Initialize the System Tray
            SystemTrayContextMenu = this.FindResource("STContextMenu") as ContextMenu;
            SystemTrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = Properties.Resources.WebexKiller,
                Visible = true
            };
            SystemTrayIcon.MouseClick += SystemTrayIcon_MouseClick;
            SystemTrayIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    System.Windows.Forms.MouseEventArgs mea = (System.Windows.Forms.MouseEventArgs)args;
                    if (mea.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                    }
                };
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppRunning = false;

            NotificationToast.Close();
            NotificationToast = null;
            Properties.Settings.Default.Save();

            SystemTrayIcon.Dispose();
        }
        #endregion Initialization and Destruction

        #region Main Logic
        public void StartWatcherLoop()
        {
            AppRunning = true;
            List<Process> Processes = null;

            // atmgr - The meeting
            // ptoneclk - Meetings App
            // WebexMTA - No idea

            int loops = 100;
            while (AppRunning)
            {
                if (loops < 60 || IsSnoozed)
                {
                    loops++;
                    Thread.Sleep(1000);
                    continue;
                }

                Console.WriteLine("Looking for processes.");
                Processes = Process.GetProcesses().ToList();

                BadProcesses = new List<Process>();
                BadProcesses = Processes.Where(x => 
                    x.ProcessName.ToLower() == "atmgr" || 
                    x.ProcessName.ToLower() == "ptoneclk" ||
                    x.ProcessName.ToLower() == "webexmta").ToList();

                if (BadProcesses.Count > 0)
                {
                    if (BadProcesses.Where(x => x.ProcessName.ToLower() == "atmgr").Count() > 0 &&
                        BadProcesses.Where(x => x.ProcessName.ToLower() == "ptoneclk").Count() > 0)
                    {
                        Console.WriteLine("atmgr and ptoneclk are both open");
                    }
                    else if (BadProcesses.Where(x => x.ProcessName.ToLower() == "atmgr").Count() > 0 &&
                             BadProcesses.Where(x => x.ProcessName.ToLower() == "ptoneclk").Count() == 0)
                    {
                        Console.WriteLine("atmgr is open");
                        HandleBadProcesses();
                    }
                    else if (BadProcesses.Where(x => x.ProcessName.ToLower() == "atmgr").Count() == 0 &&
                             BadProcesses.Where(x => x.ProcessName.ToLower() == "ptoneclk").Count() == 0)
                    {
                        Console.WriteLine("atmgr and ptoneclk are both closed");
                        HandleBadProcesses();
                    }
                }

                loops = 1;
                Thread.Sleep(1000);
            }

            SnoozeTimer = null;
        }

        private void HandleBadProcesses()
        {
            if (Properties.Settings.Default.AutoKillProcesses)
            {
                KillBadProcesses();
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                    ShowToast($"{BadProcesses.Count} WebEx process{(BadProcesses.Count == 1 ? " has" : "es have")} been found.")
                );
            }
        }

        private void ShowToast(string message)
        {
            NotificationToast.Show(message);
        }

        private void ToastActionTaken(int returnCode)
        {
            // 0: Regular close
            // 1: Timed out
            // 2: Snooze
            // 3: Kill Processes
            switch (returnCode)
            {
                case 1:
                case 2:
                    SnoozeTimer = new DispatcherTimer();
                    SnoozeTimer.Tick += SnoozeTimer_Tick;
                    SnoozeTimer.Interval = new TimeSpan(1, 0, 0);
                    SnoozeTimer.Start();
                    break;
                case 3:
                    KillBadProcesses();
                    break;
                default:
                    break;
            }
        }

        private void SnoozeTimer_Tick(object sender, EventArgs e)
        {
            SnoozeTimer = null;
            IsSnoozed = false;
        }

        private void KillBadProcesses()
        {
            foreach (Process p in BadProcesses)
            {
                try
                {
                    Console.WriteLine($"Destroying Process: {p.ProcessName}");
                    p.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            BadProcesses = null;
        }
        #endregion Main Logic

        #region System Tray Actions
        private void STKillProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            List<Process> Processes = null;

            Console.WriteLine("Looking for processes.");
            Processes = Process.GetProcesses().ToList();

            BadProcesses = new List<Process>();
            BadProcesses = Processes.Where(x =>
                x.ProcessName.ToLower() == "atmgr" ||
                x.ProcessName.ToLower() == "ptoneclk" ||
                x.ProcessName.ToLower() == "webexmta").ToList();

            KillBadProcesses();
        }

        private void STSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Show();
        }

        private void STQuitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SystemTrayIcon_MouseClick(object sender, EventArgs e)
        {
            System.Windows.Forms.MouseEventArgs me = (System.Windows.Forms.MouseEventArgs)e;
            if (me.Button == System.Windows.Forms.MouseButtons.Right)
            {
                SystemTrayContextMenu.PlacementTarget = sender as Button;
                SystemTrayContextMenu.IsOpen = true;
                this.Activate();
            }
        }
        #endregion System Tray Actions

        #region UI Actions
        private void WatchProcessesCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.WatchProcesses == true)
            {
                WatcherThread = new Thread(() => StartWatcherLoop());
                WatcherThread.Start();
            }
        }

        private void WatchProcessesCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            AppRunning = false;
        }
        #endregion UI Actions
    }
}