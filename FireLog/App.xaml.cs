using System.Windows;
using Serilog;

namespace FireLog
{
    public partial class App : Application
    {
        public static Guid SessionId { get; } = Guid.NewGuid();
        public static string RunMode { get; set; } = "Unknown";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File("logs/firelog-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
#if DEBUG
                .WriteTo.Debug() 
#endif
                .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine("SERILOG: " + msg));
            Log.Information("App started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("App exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.DispatcherUnhandledException += (s, ex) =>
            {
                Log.Fatal(ex.Exception, "UI thread unhandled exception");
                ex.Handled = true; 
                MessageBox.Show("Unexpected error. The app will continue, logs recorded.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                Log.Fatal(ex.ExceptionObject as Exception, "AppDomain unhandled exception");
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                Log.Fatal(ex.Exception, "TaskScheduler unobserved exception");
                ex.SetObserved();
            };

            Log.Information("App started");
        }

    }
}
