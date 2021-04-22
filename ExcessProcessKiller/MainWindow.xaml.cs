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
using System.Configuration;
using Microsoft.Win32;
using System.Diagnostics;

namespace ExcessProcessKiller
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isFromFile, isDebug;
        public string procsPath;
        public MainWindow()
        {
            InitializeComponent();
            InitializeConfig();
            InitializeProcesses();
        }

        private void InitializeConfig()
        {
            var appSettings = ConfigurationManager.AppSettings;
            if (appSettings.Count != 0)
            {
                isFromFile = bool.Parse(appSettings.Get("isFromFile"));
                procsPath = appSettings.Get("procsPath");
                isDebug = bool.Parse(appSettings.Get("isDebug"));
            }
            else
            {
                InitializePreset("isFromFile", "false");
                InitializePreset("procsPath", "null");
                InitializePreset("isDebug", "true");
            }
            InitializeElements();
            from_file_checkbox.IsChecked = isFromFile;
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
                if (procsPath != "null")
                {
                    InitializePreset("procsPath", procsPath);
                    string allProcess = System.IO.File.ReadAllText(procsPath);
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
                        catch (IndexOutOfRangeException) { if (isDebug) { MessageBox.Show($"Process with name {processes[i]} not exist!", "Error!", MessageBoxButton.OK); } }
                    }
                }
                else { if (isDebug) { MessageBox.Show($"{procsPath} - not exist!", "Error!", MessageBoxButton.OK); } }
                
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
        }

        private void KillProcessByName(string name)
        {
            name = name.Replace("System.Windows.Controls.ListViewItem: ", "");
            MessageBox.Show($"{name} - to kill!", "Info!", MessageBoxButton.OK);
        }

        private void kill_all_button_Click(object sender, RoutedEventArgs e)
        {
            int n = processes_listview.Items.Count;
            for (int i = 0; i < n; i++)
            {
                string procName = processes_listview.Items[i].ToString();
                KillProcessByName(procName);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
    }
}
