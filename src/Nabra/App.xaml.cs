using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Nabra.Services;
using Nabra.Views;

namespace Nabra
{
    public partial class App : Application
    {
        private const string MutexName = @"Local\Nabra.SingleInstance.7F3C2A91";
        private const string ShowEventName = @"Local\Nabra.ShowWindow.7F3C2A91";

        private Mutex? _mutex;
        private EventWaitHandle? _showEvent;
        private MainWindow? _main;

        internal AppController Controller { get; private set; } = null!;

        internal bool IsExiting { get; private set; }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, args) =>
            {
                LogException("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    LogException("AppDomainUnhandledException", ex);
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogException("UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // Already running: ask the first instance to show its window, then quit quietly.
                try
                {
                    using (var existing = EventWaitHandle.OpenExisting(ShowEventName))
                    {
                        existing.Set();
                    }
                }
                catch (Exception)
                {
                }
                Shutdown();
                return;
            }

            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            var waiter = new Thread(() =>
            {
                try
                {
                    while (_showEvent.WaitOne())
                    {
                        Dispatcher.BeginInvoke(new Action(() => ShowMainWindow("Home", false)));
                    }
                }
                catch (Exception)
                {
                    // The event was disposed while the app was shutting down.
                }
            })
            {
                IsBackground = true,
                Name = "NabraShowWindowWaiter",
            };
            waiter.Start();

            Controller = new AppController(this);
            Controller.Start();

            bool startMinimized = e.Args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));
            if (!startMinimized) ShowMainWindow("Home", false);
        }

        internal void ShowMainWindow(string page, bool beginHotkeyChange)
        {
            if (IsExiting) return;

            if (_main == null)
            {
                _main = new MainWindow(Controller);
                _main.Closed += (s, args) => _main = null;
            }

            if (!_main.IsVisible) _main.Show();
            if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
            _main.Activate();
            _main.NavigateTo(page);

            if (beginHotkeyChange)
            {
                var window = _main;
                Dispatcher.BeginInvoke(new Action(() => window.BeginHotkeyChange()));
            }
        }

        internal void ExitApplication()
        {
            if (IsExiting) return;
            IsExiting = true;

            try { Controller.Dispose(); } catch (Exception) { }
            try { _showEvent?.Dispose(); } catch (Exception) { }
            try { _mutex?.ReleaseMutex(); } catch (Exception) { }
            Shutdown();
        }

        private static void LogException(string source, Exception ex)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nabra");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "app.log");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex}\r\n";
                File.AppendAllText(path, entry);
            }
            catch
            {
            }
        }
    }
}
