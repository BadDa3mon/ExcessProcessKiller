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
using System.Resources;
using System.IO;

namespace ExcessProcessKiller
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isFromFile, isDebug, isFromTime;
        public int time;
        public string procsPath;
        public Timer timer;
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
            catch (IOException e) { if (isDebug) { MessageBox.Show($"Exception: {e.Message}"); } }
        }

        private void InitializeTimer()
        {
            Color my = new Color();
            if (isFromTime)
            {
                if (time != -1)
                {
                    if (time == 0) { InitializeConfig(); }
                    else
                    {
                        timer = new Timer(time * 1000 * 60);
                        timer.Elapsed += OnTimedEvent;
                        timer.AutoReset = true;
                        timer.Enabled = true;
                        my = (Color)ColorConverter.ConvertFromString("#FF000000");
                    }
                }
                else
                { 
                    if (isDebug) { MessageBox.Show("Time = -1, please replace!", "Debug"); }
                    my = (Color)ColorConverter.ConvertFromString("#FFFF0000");
                }
            }
            else if (timer != null) { timer.Dispose(); }
            else { if (isDebug) { MessageBox.Show($"Timer is null, isFromTime - {isFromTime}!", "Debug", MessageBoxButton.OK); } }
            time_textbox.Foreground = new SolidColorBrush(my);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            int n = processes_listview.Items.Count;
            for (int i = 0; i < n; i++)
            {
                string procName = processes_listview.Items[i].ToString();
                KillProcessByName(procName);
            }
        }

        private void InitializeConfig()
        {
            var appSettings = ConfigurationManager.AppSettings;
            if (appSettings.Count != 0)
            {
                isFromFile = bool.Parse(appSettings.Get("isFromFile"));
                isFromTime = bool.Parse(appSettings.Get("isFromTime"));
                time = int.Parse(appSettings.Get("time"));
                procsPath = appSettings.Get("procsPath");
                isDebug = bool.Parse(appSettings.Get("isDebug"));
            }
            else
            {
                InitializePreset("isFromFile", "false");
                InitializePreset("isFromTime", "false");
                InitializePreset("time", "-1");
                InitializePreset("procsPath", "null");
                InitializePreset("isDebug", "false");
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
            from_file_checkbox.IsChecked = isFromFile;
            time_textbox.Text = time.ToString();
            timer_checkbox.IsChecked = isFromTime;
            time_textbox.IsEnabled = isFromTime;
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
                if (procsPath != "null" && procsPath != null)
                {
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
                                item.Content = processes[i];
                                ind = i;
                                processes_listview.Items.Add(item);
                            }
                        }
                        catch (IndexOutOfRangeException) { if (isDebug) { MessageBox.Show($"Process with name {processes[i]} not exist!", "Debug!", MessageBoxButton.OK); } }
                    }
                }
                else { if (isDebug) { MessageBox.Show($"{procsPath} - not exist!", "Debug", MessageBoxButton.OK); } }
                
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
                        processes_listview.Items.Add(item);
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
                string procName = processes_listview.SelectedItems[i].ToString();
                KillProcessByName(procName);
            }
            InitializeProcesses();
        }

        private void KillProcessByName(string name)
        {
            try
            {
                name = name.Replace("System.Windows.Controls.ListViewItem: ", "");
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
                time_textbox.Clear();
                MessageBox.Show($"Please, input only digits!", "Debug", MessageBoxButton.OK);
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
            if (isDebug) { MessageBox.Show($"isFromTime - {isFromTime}!", "Debug", MessageBoxButton.OK); }
        }

        private void time_textbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) 
            {
                time = int.Parse(time_textbox.Text);
                if (isDebug) { MessageBox.Show($"Save time[{time}]!", "Debug", MessageBoxButton.OK); }
                InitializePreset("time", time.ToString());
            }
            InitializeTimer();
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

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Show();
            this.WindowState = lastState;
        }

        private void exit_of_tray_Click(object sender, RoutedEventArgs e)
        {
            //Close();
            App.Current.Shutdown();
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
            int n = processes_listview.Items.Count;
            for (int i = 0; i < n; i++)
            {
                string procName = processes_listview.Items[i].ToString();
                KillProcessByName(procName);
            }
            InitializeProcesses();
        }
    }
}
