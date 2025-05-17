using MicaWPF.Controls;
using NodeSwitch.Services;
using NodeSwitch.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace NodeSwitch
{
    public partial class MainWindow : MicaWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            PrivilegeEnableService.EnableSeCreateSymbolicLinkPrivilege();
            DataContext = new MainViewModel();
            TrayIcon.LeftClickCommand = new RelayCommand<object>(o =>
            {
                ShowInTaskbar = true; // Show in taskbar
                TrayIcon.Visibility = Visibility.Collapsed;
                Show(); // Show the window
                Activate(); // Ensure the window is active
                Focus();
                WindowState = WindowState.Normal; // Ensure the window state is Normal
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false; // Hide from taskbar
                Hide(); // Hide the window
                TrayIcon.Visibility = Visibility.Visible;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}