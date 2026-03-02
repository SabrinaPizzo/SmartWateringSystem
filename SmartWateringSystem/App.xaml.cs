using System.Windows;

namespace SmartWateringSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            foreach (var service in Bootstrapper.DataServices)
            {
                service.StopPumpAsync().Wait();
            }

            base.OnExit(e);
        }
    }
}
