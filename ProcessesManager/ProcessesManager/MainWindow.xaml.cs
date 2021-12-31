﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using ApplicationWindow = System.Windows.Application;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace ProcessesManager
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    class Schedule
    {
        public TimeSpan from;
        public TimeSpan to;
        public int duration;
        public int interupt;
        public int sum;
        public Schedule(string entry)
        {
            //"F07:30 T11:30 D60 I20 S150", "F12:30 T5:30 D60 I20 S150", "F19:30 T23:30 D60 I20 S150"

            string[] subEntry = entry.Split(' ');

            string timeFrom = subEntry[0].Substring(1);
            string[] timeFromEntry = timeFrom.Split(':');
            from = new TimeSpan(Int32.Parse(timeFromEntry[0]), Int32.Parse(timeFromEntry[1]), 0);

            string timeTo = subEntry[1].Substring(1);
            string[] timeToEntry = timeTo.Split(':');
            to = new TimeSpan(Int32.Parse(timeToEntry[0]), Int32.Parse(timeToEntry[1]), 0);

            if (!Int32.TryParse(subEntry[2].Substring(1), out duration))
            {
                duration = 0;
            }
            if (!Int32.TryParse(subEntry[3].Substring(1), out interupt))
            {
                duration = 0;
            }
            if (!Int32.TryParse(subEntry[4].Substring(1), out sum))
            {
                duration = 0;
            }
        }

        public void setFrom(string from_string)
        {
            string[] timeEntry = from_string.Split(':');
            from = new TimeSpan(Int32.Parse(timeEntry[0]), Int32.Parse(timeEntry[1]), 0);
        }

        public void setTo(string to_string)
        {
            string[] timeEntry = to_string.Split(':');
            from = new TimeSpan(Int32.Parse(timeEntry[0]), Int32.Parse(timeEntry[1]), 0);
        }

        public string ToEntry()
        {
            return 'F' + from.ToString(@"hh\:mm") + ' ' + 'T' + to.ToString(@"hh\:mm") + ' ' + 'D' + duration.ToString() + ' ' + 'I' + interupt.ToString() + ' ' + 'S' + sum.ToString();
        }

    }


    public partial class MainWindow : Window
    {
        private string[] defaultSchedule =
            {
            "F07:30 T11:30 D0060 I0020 S0150", "F12:30 T17:30 D0060 I0020 S0150", "F18:00 T23:30 D0003 I0020 S0150"
        };
        private string[] todaySchedule;
        private string todayPath;
        private string oneDrivePath = Environment.GetEnvironmentVariable("OneDriveConsumer") + @"\" + "management";
        private int pass_try = 3;
        
        private bool isLogin = false;
        private bool firstLogin = false;

        private TimeSpan LoggedInTime = new TimeSpan();
        private TimeSpan TimeLeft = new TimeSpan();
        private int stage = 0;
        private bool isScheduleChanged = false;
        private List<Schedule> _schedule = new List<Schedule>();

        #region hook key board
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string logName = "Log_";
        private static string imgName = "Log_";
        private static string logExtendtion = ".txt";
        private static string imgExtendtion = ".jpg";

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Delegate a LowLevelKeyboardProc to use user32.dll
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Set hook into all current process
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        /// <summary>
        /// Every time the OS call back pressed key. Catch them 
        /// then cal the CallNextHookEx to wait for the next key
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                WriteLog(vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Write pressed key into log.txt file
        /// </summary>
        /// <param name="vkCode"></param>
        static void WriteLog(int vkCode)
        {
            Console.WriteLine((Keys)vkCode);
            string logNameToWrite = logName + DateTime.Now.ToLongDateString() + logExtendtion;
            StreamWriter sw = new StreamWriter(logNameToWrite, true);
            sw.Write((Keys)vkCode);
            sw.Close();
        }

        /// <summary>
        /// Start hook key board and hide the key logger
        /// Key logger only show again if pressed right Hot key
        /// </summary>
        static void HookKeyboard()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        #endregion
        public MainWindow()
        {
            InitializeComponent();

            if (!Directory.Exists(oneDrivePath))
            {
                Directory.CreateDirectory(oneDrivePath);
            }
            DateTime localDate = DateTime.Now;
            todayPath = oneDrivePath + @"\" + localDate.ToString("dd-MM-yyyy");
            if (!Directory.Exists(todayPath))
            {
                Directory.CreateDirectory(todayPath);
                Directory.CreateDirectory(todayPath + @"\" + "capture");
            }
            if (!File.Exists(todayPath + @"\schedule.txt"))
            {
                MakeDefaultScheduleTask();
                todaySchedule = defaultSchedule;
            }
            else
            {
                todaySchedule = File.ReadAllLines(todayPath + @"\schedule.txt");
                for (var i = 0; i < todaySchedule.Length; i++)
                {
                    Schedule time = new Schedule(todaySchedule[i]);
                    _schedule.Add(time);
                }
            }
            loginTimer();
        }

        private void AskAgain()
        {
            isLogin = false;
            this.Show();
            loginTimer();
            //Thread.Sleep(5000);
            //var loginWindow = new LoginWindow();
            //loginWindow.ShowDialog();
            //var result = loginWindow.DialogResult;
            //Thread.Sleep(2000);
            //loginWindow.Close();
            /*if (result==true)
            {
                if(loginWindow.PwTextBox.Text == "123")
                {
                }
            }*/
            //Thread.Sleep(5000);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(1000);
            SendKeys.SendWait("{PRTSC}");
            System.Drawing.Image myImage = Clipboard.GetImage();
            myImage.Save("D:\\thang.jpg");

        }

        private void Capture()
        {
            SendKeys.SendWait("{PRTSC}");
            System.Drawing.Image myImage = Clipboard.GetImage();
            string img = DateTime.Now.TimeOfDay.ToString(@"hh\hmm\mss") + imgExtendtion;
            myImage.Save(todayPath + @"\" + "capture" + $@"\{img}");
        }
        private int count = 0;
        private int CheckTimeLeft()
        {
           
            if (isScheduleChanged)
            {
                //Nếu file Schedule.txt có sự thay đổi thì cập nhật danh sách _schedule
                _schedule.Clear();
                for (var i=0; i<todaySchedule.Length;i++)
                {
                    Schedule time = new Schedule(todaySchedule[i]);
                    _schedule.Add(time);
                }
            }

            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            TimeSpan currentDuration = new TimeSpan(0, _schedule[stage].duration, 0);
            TimeLeft = (LoggedInTime + currentDuration) - currentTime;
            time_left.Content = $"{TimeLeft.Minutes}-{TimeLeft.Seconds}--{count}";
            if (TimeLeft.Minutes <= 1)
            {
                MessageBox.Show("This computer will be shutdowned in 1 min");
                var psi = new ProcessStartInfo("shutdown", "/s /t 600");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
                return 1;
            }
            return 0;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {

            if (PassTextBlock.Text == "")
            {
                MessageBox.Show("Please enter your password");
            }
            else if (PassTextBlock.Text == "hdhc")
            {
                //this.Hide();
                isLogin = true;

                //lưu lại thời điểm đăng nhập vào hệ thống
                LoggedInTime = DateTime.Now.TimeOfDay;

                for (var i = 0; i < todaySchedule.Length; i++)
                {
                    Schedule time = new Schedule(todaySchedule[i]);
                    _schedule.Add(time);
                    if (LoggedInTime >= time.from && LoggedInTime <= time.to)
                    {
                        //Xác định thời điểm đăng nhập đang ở giai đoạn nào 
                        stage = i;
                    }
                }

                if (!firstLogin)
                {
                    runInWatchChildren();
                }
                firstLogin = true;
            }
            else if (PassTextBlock.Text == "hdhp")
            {
                runInWatchParrent();
            }
            else
            {
                pass_try--;
                if (pass_try > 0)
                {
                    MessageBox.Show($"Wrong password, {pass_try} tries(try) left.");
                }
                else
                {
                    MessageBox.Show($"Your computer will shutdown now. Please try again after 10 minutes");
                }

            } 
        }

        // main method 
        public void runInWatchParrent()
        {
            //============================Tiến trình yêu cầu đăng nhập lại sau 60 phút
            Thread askAgain = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(60000*30);
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        AskAgain();
                    });
                }
            });

            askAgain.IsBackground = true;
            askAgain.Start();
        }

        private void runInWatchChildren()
        {
            //=================Tiến trình chụp màn hình
            Thread capTure = new Thread(() =>
            {
                while (true)
                {
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        Capture();
                    });
                    Thread.Sleep(5000);
                }
            });
            capTure.IsBackground = true;
            capTure.Start();

            //===================Tiến trình hook keyboard
            Thread keyLogger = new Thread(() =>
            {
                ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                    HookKeyboard();
                });
            });
            keyLogger.IsBackground = true;
            //keyLogger.Start();

            //==================Tiến trình kiểm tra thời gian sử dùng còn lại của trẻ
            int breakLoops = 0;
            Thread checkLeftTime = new Thread(() =>
            {
                while (true)
                {
                    ApplicationWindow.Current.Dispatcher.Invoke((Action)delegate {
                        breakLoops = CheckTimeLeft();
                    });
                    if (breakLoops == 1)
                        break;
                }
            });
            checkLeftTime.IsBackground = true;
            checkLeftTime.Start();


            //======================Tiến trình kiểm tra thay đổi ở file Schedule.txt
            // test schedule.txt change notification
            Thread CheckScheduleChange = new Thread(() =>
            {
                // Create a new FileSystemWatcher and set its properties.
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = todayPath;
                /* Watch for changes in LastAccess and LastWrite times, and 
                   the renaming of files or directories. */
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                watcher.Filter = "*.txt";

                // Add event handlers.
                watcher.Changed += new FileSystemEventHandler(OnChanged);

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            });
            CheckScheduleChange.IsBackground = true;
            //CheckScheduleChange.Start();
        }

        public void loginTimer()
        {
            Thread timer = new Thread(() =>
            {
                int curr_time = 60000*10;
                while (curr_time>0)
                {
                    curr_time -= 1000;
                    this.Dispatcher.Invoke(()=> {
                        lb_timer.Content = $"You have {curr_time/1000} second left to enter password";
                    });
                    
                    Thread.Sleep(1000);
                }
                if (!isLogin)
                {
                    MessageBox.Show("bummmmmm... shut down");
                }
            });
            timer.IsBackground = true;
            timer.Start();
        }
        // utility method
        public async Task MakeDefaultScheduleTask()
        {
            await File.WriteAllLinesAsync(todayPath + @"\schedule.txt", defaultSchedule);
        }

        public async Task updateScheduleTask()
        {
            if (todaySchedule.Length > 0)
            {
                await File.WriteAllLinesAsync(todayPath + @"\schedule.txt", todaySchedule);
            }
            else
            {
                return;
            }
        }

        public void OnChanged(object source, FileSystemEventArgs e)
        {
            todaySchedule = File.ReadAllLines(todayPath + @"\schedule.txt");
            MessageBox.Show("Schedule change");
        }
    }
}
