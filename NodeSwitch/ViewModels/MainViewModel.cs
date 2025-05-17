using NodeSwitch.Models;
using NodeSwitch.Services.NvmManagerApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NodeSwitch.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _installedSearchText = default!;
        public string InstalledSearchText
        {
            get => _installedSearchText;
            set
            {
                if (_installedSearchText != value)
                {
                    _installedSearchText = value;
                    OnPropertyChanged(nameof(InstalledSearchText));
                    FilteredInstalledVersions?.Refresh();
                }
            }
        }

        private string _availableSearchText = default!;
        public string AvailableSearchText
        {
            get => _availableSearchText;
            set
            {
                if (_availableSearchText != value)
                {
                    _availableSearchText = value;
                    OnPropertyChanged(nameof(AvailableSearchText));
                    FilteredAvailableVersions?.Refresh();
                }
            }
        }

        public ICollectionView FilteredInstalledVersions { get; }
        public ICollectionView FilteredAvailableVersions { get; }

        public ObservableCollection<NodeVersion> InstalledVersions { get; } = new ObservableCollection<NodeVersion>();
        public ObservableCollection<string> AvailableVersions { get; } = new ObservableCollection<string>();

        private bool _isInstalling;
        public bool IsInstalling
        {
            get => _isInstalling;
            set { _isInstalling = value; OnPropertyChanged(nameof(IsInstalling)); }
        }

        public ICommand InstallCommand { get; }
        public ICommand UseCommand { get; }
        public ICommand UninstallCommand { get; }

        public MainViewModel()
        {
            InstallCommand = new RelayCommand<string>(async (version) => await InstallVersionAsync(version), CanInstall);
            UseCommand = new RelayCommand<string>(UseVersion, CanUseVersion);
            UninstallCommand = new RelayCommand<string>((v) => UninstallVersion(v));

            FilteredInstalledVersions = CollectionViewSource.GetDefaultView(InstalledVersions);
            FilteredInstalledVersions.Filter = InstalledVersionsFilter;

            FilteredAvailableVersions = CollectionViewSource.GetDefaultView(AvailableVersions);
            FilteredAvailableVersions.Filter = AvailableVersionsFilter;

            InstalledSearchText = "";
            AvailableSearchText = "";

            LoadData();
        }

        private async void LoadData()
        {
            InstalledVersions.Clear();
            var installed = NvmService.ListInstalledVersions();

            string activeVersion = NvmService.GetActiveVersion();

            foreach (var v in installed)
            {
                InstalledVersions.Add(new NodeVersion
                {
                    Version = v,
                    IsInstalled = true,
                    IsActive = (v == activeVersion)
                });
            }

            AvailableVersions.Clear();
            var available = await NvmService.ListAvailableVersionsAsync(
                InstalledVersions
                    .Select(v => v.Version.Replace("v", ""))
                    .ToList());
            foreach (var v in available)
                AvailableVersions.Add(v);

            FilteredInstalledVersions.Refresh();
            FilteredAvailableVersions.Refresh();
        }

        private bool InstalledVersionsFilter(object obj)
        {
            if (string.IsNullOrWhiteSpace(InstalledSearchText))
                return true;
            if (obj is NodeVersion version)
                return version.Version?.IndexOf(InstalledSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            return false;
        }

        private bool AvailableVersionsFilter(object obj)
        {
            if (string.IsNullOrWhiteSpace(AvailableSearchText))
                return true;
            if (obj is string version)
                return version.IndexOf(AvailableSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            return false;
        }

        private bool CanInstall(string version)
        {
            return !IsInstalling;
        }

        private async Task InstallVersionAsync(string version)
        {
            IsInstalling = true;
            try
            {
                await NvmService.InstallNodeVersionAsync(version);

                MessageBox.Show($"Version {version} installed successfully.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed: {ex.Message}");
            }
            finally
            {
                IsInstalling = false;
                OnPropertyChanged(nameof(InstallCommand)); // To re-evaluate CanExecute
            }
        }

        private bool CanUseVersion(string version)
        {
            // Only allow if not already active
            return !InstalledVersions.Any(v => v.Version == version && v.IsActive);
        }

        private void UseVersion(string version)
        {
            foreach (var v in InstalledVersions)
                v.IsActive = v.Version == version;
            OnPropertyChanged(nameof(InstalledVersions));
            NvmService.UseNodeVersion(version);
            LoadData();
            MessageBox.Show($"Switched to node {version}");
        }

        private void UninstallVersion(string version)
        {
            NvmService.UninstallNodeVersion(version);
            LoadData();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
