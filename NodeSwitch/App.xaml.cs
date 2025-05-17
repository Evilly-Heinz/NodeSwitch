using System.IO;
using System.Windows;
using NodeSwitch.Logging;
using NvmManagerApp.Services;

namespace NodeSwitch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup file logger
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "nvm", "nvmservice.log"
            );
            NvmService.Logger = new FileLogger(logPath);

            // ...existing startup code...
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
