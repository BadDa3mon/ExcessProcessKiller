using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Configuration;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Timers;
using System.ComponentModel;
using System.Windows.Media;
using System.Reflection;
using System.IO;
using System.Windows.Threading;

namespace ExcessProcessKiller
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public class NewItem
    {
        public string RAM { get; set; }
        public string Name { get; set; }
    }
    public partial class MainWindow : Window
    {
        public bool isFromFile, isDebug, isFromTime, AutoRun, isAdmin;
        public int time, timeType, interval;
        public string procsPath;
        public Timer timer, progress;
        WindowState lastState;
        public MainWindow()
        {
            InitializeLibrary();
            InitializeComponent();
            InitializeConfig();
            InitializeTimer();
            InitializeProcesses();
        }

        private void InitializeLibrary()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            try
            {
                int last = path.LastIndexOf('\u005c') + 1;
                path = path.Remove(last);
                path += "Hardcodet.Wpf.TaskbarNotification.dll";
                if (File.Exists(path) != true)
                {
                    if (isDebug) { MessageBox.Show("File not exist, i'm created!"); }
                    File.WriteAllBytes(path, Properties.Resources.Hardcodet_Wpf_TaskbarNotification);
                }
                else { if (isDebug) { MessageBox.Show($"{path} is exist!"); } }
            }
            catch (UnauthorizedAccessException) { if (isDebug) { MessageBox.Show($"You don't have permissions to create files along this path: {path}"); } }
            catch (Exception e) { MessageBox.Show($"Error: {e.Message}!\nTry to run the program as administrator", "Debug"); }
            HideLibrary();
        }

        private void HideLibrary()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            int n = path.LastIndexOf("\u005c") + 1;
            path = path.Remove(n);
            path += "Hardcodet.Wpf.TaskbarNotification.dll";
            File.SetAttributes(path, FileAttributes.Hidden);
        }

        private void InitializeTimer()
        {
            Color my = (Color)ColorConverter.ConvertFromString("#FF000000");
            if (isFromTime)
            {
                if (time != -1)
                {
                    if (time == 0) { InitializeConfig(); time_textbox.Text = ""; }
                    else
                    {
                        int convert = 0;
                        switch (timeType)
                        {
                            case 0: convert = 1; break;
                            case 1: convert = 60; break;
                            case 2: convert = 3600; break;
                        }
                        interval = time * convert * 1000;
                        if (timer != null && timer.Enabled) { timer.Interval = interval; }
                        else
                        {
                            timer = new Timer(interval);
                            timer.Elapsed += OnTimedEvent;
                            timer.AutoReset = true;
                            timer.Enabled = true;
                        }
                        my = (Color)ColorConverter.ConvertFromString("#FF000000");
                        InitializeProgressBar();
                    }
                }
                else
                { 
                    if (isDebug) { MessageBox.Show("Time = -1, please replace!", "Debug"); }
                    my = (Color)ColorConverter.ConvertFromString("#FFFF0000");
                }
            }
            else if (timer != null) { timer.Close(); }
            else { if (isDebug) { MessageBox.Show($"Timer is null, isFromTime - {isFromTime}!", "Debug", MessageBoxButton.OK); } }
            time_textbox.Foreground = new SolidColorBrush(my);
        }

        private void InitializeProgressBar()
        {
            if (isFromTime && interval != 0)
            {
                if (progress != null && progress.Enabled)
                {
                    progress.Interval = interval / 10;
                }
                else
                {
                    progress = new Timer(interval / 10);
                    progress.Elapsed += OnProgressEvent;
                    progress.AutoReset = true;
                    progress.Enabled = true;
                }
                timer_progress_bar.IsEnabled = true;
                timer_progress_bar.ToolTip = $"Интервал: {progress.Interval}";
            }
            else
            {
                if (progress != null) { progress.Close(); }
                timer_progress_bar.ToolTip = $"Выключено";
                timer_progress_bar.IsEnabled = false;
                timer_progress_bar.Value = 0;
            }   
        }

        private void OnProgressEvent(object source, ElapsedEventArgs e)
        {
            Action my = new Action(() =>
            {
                if (timer_progress_bar.Value != 10) { timer_progress_bar.Value++; }
                else { timer_progress_bar.Value = 1; }
            });
            this.Dispatcher.Invoke(my);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() => {
                kill_all_button_Click(this, null); });
        }
        private void InitializeConfig()
        {
            var appSettings = ConfigurationManager.AppSettings;
            if (appSettings.Count != 0)
            {
                isFromFile = bool.Parse(appSettings.Get("isFromFile"));
                isFromTime = bool.Parse(appSettings.Get("isFromTime"));
                time = int.Parse(appSettings.Get("time"));
                timeType = int.Parse(appSettings.Get("timeType"));
                procsPath = appSettings.Get("procsPath");
                isDebug = bool.Parse(appSettings.Get("isDebug"));
                AutoRun = bool.Parse(appSettings.Get("AutoRun"));
            }
            else
            {
                InitializePreset("isFromFile", "false");
                InitializePreset("isFromTime", "false");
                InitializePreset("time", "-1");
                InitializePreset("timeType", "1");
                InitializePreset("procsPath", "null");
                InitializePreset("isDebug", "false");
                InitializePreset("AutoRun", "false");
            }
            InitializeElements();
        }

        private void InitializeElements()
        {
            string path;
            if (procsPath != "null")
            {
                path = $"Файл: {procsPath}";
            }
            else { path = ""; }
            file_processes_label.Content = path;
            if (isFromFile) 
            { 
                select_file_button.Visibility = Visibility.Visible;
                file_processes_label.Visibility = Visibility.Visible;
            }
            else 
            { 
                select_file_button.Visibility = Visibility.Hidden;
                file_processes_label.Visibility = Visibility.Hidden;
            }
            time_textbox.MaxLength = 4;
            timer_progress_bar.Maximum = 10;
            from_file_checkbox.IsChecked = isFromFile;
            time_textbox.Text = time.ToString();
            timer_checkbox.IsChecked = isFromTime;
            time_textbox.IsEnabled = isFromTime;
            autorun_checkbox.IsChecked = AutoRun;
            string title = "";
            switch (timeType)
            {
                case 0: title = "сек"; break;
                case 1: title = "мин"; break;
                case 2: title = "час"; break;
            }
            time_type_title.Content = title; time_textbox.ToolTip = $"Время между убийствами процессов (в {title})";
        }

        private void InitializePreset(string key, string value)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save();
        }

        private void InitializeProcesses()
        {
            processes_listview.Items.Clear();
            if (isFromFile && procsPath != "null")
            {
                if (procsPath != null)
                {
                    if (File.Exists(procsPath) != true)
                    {
                        MessageBox.Show($"File {procsPath} not exists! Select .txt file again", "Error!");
                        procsPath = "null";
                        InitializePreset("procsPath", procsPath); InitializeElements();
                        return;
                    }
                    InitializePreset("procsPath", procsPath);
                    string allProcess = File.ReadAllText(procsPath);
                    string[] processes = allProcess.Split(';'); int ind;
                    for (int i = 0; i < processes.Length; i++)
                    {
                        try
                        {
                            Process proc = Process.GetProcessesByName(processes[i])[0];
                            if (proc != null)
                            {
                                ListViewItem item = new ListViewItem();
                                long ram = proc.WorkingSet64 / 1024;
                                ind = i;
                                processes_listview.Items.Add(new NewItem { Name = processes[i], RAM = $"{ram} K" });
                            }
                            proc.Dispose();
                        }
                        catch (IndexOutOfRangeException) { if (isDebug) { MessageBox.Show($"Process with name {processes[i]} not exist!", "Debug!", MessageBoxButton.OK); } }
                    }
                }
                else { if (isDebug) { MessageBox.Show($"Path '{procsPath}' - not exist!", "Error!", MessageBoxButton.OK); } }
                
            }
            else
            {
                Process[] procs = Process.GetProcesses();
                if (procs != null)
                {
                    for (int i = 0; i < procs.Length; i++)
                    {
                        ListViewItem item = new ListViewItem();
                        item.Content = procs[i].ProcessName;
                        long ram = procs[i].WorkingSet64 / 1024;
                        processes_listview.Items.Add(new NewItem { Name = procs[i].ProcessName, RAM = $"{ram} K" });
                    }
                }
            }
            InitializeElements();
            processes_label.Content = $"Процессы: {processes_listview.Items.Count}";
        }

        private void from_file_checkbox_Click(object sender, RoutedEventArgs e)
        {
            isFromFile = from_file_checkbox.IsChecked.Value;
            InitializePreset("isFromFile", isFromFile.ToString());
            InitializeProcesses();
        }

        private void kill_selected_button_Click(object sender, RoutedEventArgs e)
        {
            int n = processes_listview.SelectedItems.Count;
            for (int i = 0; i < n; i++)
            {
                NewItem my = (NewItem)processes_listview.SelectedItems[i];
                string procName = my.Name;
                KillProcessByName(procName);
            }
            InitializeProcesses();
        }

        private void KillProcessByName(string name)
        {
            try
            {
                if (isDebug) { MessageBox.Show($"{name} - to kill!", "Debug", MessageBoxButton.OK); }
                Process[] Procs = Process.GetProcessesByName(name);
                for (int i = 0; i < Procs.Length; i++)
                {
                    Procs[i].Kill();
                }
            }
            catch (Win32Exception) { if (isFromTime != true) { MessageBox.Show("You don't have permission to kill me!", $"{name}"); } }
        }

        private void time_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string reg = @"\b[0-9]{1,}\b"; TextBox newBox = (TextBox)sender;
            if (Regex.IsMatch(newBox.Text, reg) != true) 
            {
                if (newBox.Text == "") { return; }
                MessageBox.Show($"Please, input only digits!", "Error!", MessageBoxButton.OK);
                time_textbox.Clear();
            }
            else 
            { 
                time = int.Parse(newBox.Text);
                InitializePreset("time", time.ToString());
            }
        }

        private void timer_checkbox_Click(object sender, RoutedEventArgs e)
        {
            isFromTime = timer_checkbox.IsChecked.Value;
            InitializePreset("isFromTime", isFromTime.ToString());
            InitializeTimer();
            InitializeElements();
            InitializeProgressBar();
            if (isDebug) { MessageBox.Show($"isFromTime - {isFromTime}!", "Debug", MessageBoxButton.OK); }
        }

        private void time_textbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) 
            {
                if (time_textbox.Text == null || time_textbox.Text == "") { MessageBox.Show("Please enter at least one digit!", "Error!"); }
                else { time = int.Parse(time_textbox.Text); }
                if (isDebug) { MessageBox.Show($"Save time[{time}]!", "Debug", MessageBoxButton.OK); }
                InitializePreset("time", time.ToString());
                Keyboard.ClearFocus();
                InitializeTimer();
            }
        }

        private void update_process_button_Click(object sender, RoutedEventArgs e)
        {
            InitializeProcesses();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else { lastState = WindowState; }
        }

        private void autorun_checkbox_Click(object sender, RoutedEventArgs e)
        {
            InitializePreset("AutoRun", autorun_checkbox.IsChecked.Value.ToString());
            AutoRun = autorun_checkbox.IsChecked.Value;
            try
            {
                if (AutoRun)
                {
                    RegistryKey myKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\u005cMicrosoft\u005cWindows\u005cCurrentVersion\u005cRun", true);
                    string path = Assembly.GetExecutingAssembly().Location;
                    myKey.SetValue("ExcessProcessKiller", path);
                }
                else
                {
                    RegistryKey myKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\u005cMicrosoft\u005cWindows\u005cCurrentVersion\u005cRun", true);
                    myKey.DeleteValue("ExcessProcessKiller");
                }
            }
            catch (UnauthorizedAccessException exc)
            { 
                MessageBox.Show($"Error: {exc.Message}\nTry to run the program as administrator", "Debug");
                if (AutoRun)
                {
                    autorun_checkbox.IsChecked = false; AutoRun = false;
                }
                InitializePreset("AutoRun", AutoRun.ToString());
            }
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Show();
            this.WindowState = lastState;
        }

        private void exit_of_tray_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void time_type_title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label send = (Label)sender; int i = 0;
            string[] titles = { "сек", "мин", "час" };
            switch (send.Content)
            {
                case "сек": i = 1; break;
                case "мин": i = 2; break;
                case "час": i = 0; break;
            }
            send.Content = titles[i];
            timeType = i; InitializePreset("timeType", i.ToString());
            InitializeTimer();
        }

        private void select_file_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Filter = "Текстовые документы(*.txt)|*.txt|Все файлы(*.*)|*.*";
            if (OFD.ShowDialog() == true)
            {
                procsPath = OFD.FileName;
            }
            else { procsPath = "null"; }
            InitializeProcesses();
        }

        private void kill_all_button_Click(object sender, RoutedEventArgs e)
        {
            InitializeProcesses();
            int n = processes_listview.Items.Count;
            for (int i = 0; i < n; i++)
            {
                NewItem my = (NewItem)processes_listview.Items[i];
                string procName = my.Name;
                KillProcessByName(procName);
            }
            InitializeProcesses();
        }
    }
}
