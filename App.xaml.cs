using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MSFSCacheManager
{
    public partial class App : Application
    {
        public App()
        {
            // Catch WPF UI thread exceptions
            DispatcherUnhandledException +=
                App_DispatcherUnhandledException;

            // Catch non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;

            // Catch unobserved Task exceptions
            TaskScheduler.UnobservedTaskException +=
                TaskScheduler_UnobservedTaskException;
        }

        // ---------------------------------------------------------
        // WPF UI THREAD EXCEPTIONS
        // ---------------------------------------------------------

        private void App_DispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(
                "WPF Dispatcher Exception",
                e.Exception);

            MessageBox.Show(
                "MSFS Cache Manager encountered an unexpected error.\n\n" +
                "A crash report has been created in your Documents folder:\n\n" +
                "MSFSCacheManager\\Logs\\crash_log.txt",
                "MSFS Cache Manager Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }


        // ---------------------------------------------------------
        // UNHANDLED APPLICATION EXCEPTIONS
        // ---------------------------------------------------------

        private void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            Exception exception =
                e.ExceptionObject as Exception
                ?? new Exception(
                    e.ExceptionObject?.ToString()
                    ?? "Unknown exception");

            LogCrash(
                "Unhandled Application Exception",
                exception);
        }


        // ---------------------------------------------------------
        // UNOBSERVED TASK EXCEPTIONS
        // ---------------------------------------------------------

        private void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            LogCrash(
                "Unobserved Task Exception",
                e.Exception);

            e.SetObserved();
        }


        // ---------------------------------------------------------
        // WRITE CRASH LOG
        // ---------------------------------------------------------

        private static void LogCrash(
            string crashType,
            Exception exception)
        {
            try
            {
                string documents =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments);

                string logFolder =
                    Path.Combine(
                        documents,
                        "MSFSCacheManager",
                        "Logs");

                Directory.CreateDirectory(
                    logFolder);

                string logFile =
                    Path.Combine(
                        logFolder,
                        "crash_log.txt");

                string crashReport =
                    "========================================\n" +
                    "MSFS CACHE MANAGER - CRASH REPORT\n" +
                    "========================================\n\n" +
                    $"Date: {DateTime.Now}\n" +
                    $"Crash Type: {crashType}\n\n" +
                    $"Exception Type:\n{exception.GetType().FullName}\n\n" +
                    $"Message:\n{exception.Message}\n\n" +
                    $"Stack Trace:\n{exception.StackTrace}\n\n" +
                    $"Full Exception:\n{exception}\n\n" +
                    "========================================\n\n";

                File.AppendAllText(
                    logFile,
                    crashReport);
            }
            catch
            {
                // Never allow the crash logger itself
                // to cause another application crash.
            }
        }
    }
}