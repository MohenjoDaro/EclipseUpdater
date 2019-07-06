using System;
using System.IO;
using Avalonia;
using Avalonia.Logging.Serilog;

namespace EclipseUpdater
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            // Delete any files marked for deletion before starting the app
            DirectoryHandler.DestroyMarkedForDeletion(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

            app.Run(new MainWindow());
        }



        private static void UpdateTarget()
        {
            int idProject = 0;

            // Check for updates
            string[] urlDownloads = UpdateHandler.CheckForUpdate(idProject);
            string pathTemp = Path.Combine(Path.GetTempPath(), "updater_temp");
            if (urlDownloads.Length != 0)
            {
                // Create temp directory for download
                if (Directory.Exists(pathTemp))
                {
                    DirectoryHandler.CreateDirectory(pathTemp);
                }

                // Download each update
                for (int i = 0; i < urlDownloads.Length; i++)
                {
                    UpdateHandler.DownloadUpdate(pathTemp, idProject, urlDownloads[i]);
                }
            }

            // Check for extraction
            // extraction call goes here

            string pathCurrent = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            // Rename the updates if it needs updates
            if (idProject == 0)
            {
                // Get the list of directory names from the temp download directory
                foreach (string currDirectory in Directory.EnumerateDirectories(pathTemp))
                {
                    string pathCurrentDirectory = Path.Combine(pathCurrent, currDirectory);
                    if (File.Exists(pathCurrentDirectory))
                    {
                        FileHandler.MarkForDeletionFile(pathCurrentDirectory);
                    }
                }

                // Get the list of file names from the temp download directory
                foreach (string currFile in Directory.EnumerateFiles(pathTemp))
                {
                    string pathCurrentFile = Path.Combine(pathCurrent, currFile);
                    if (File.Exists(pathCurrentFile))
                    {
                        FileHandler.MarkForDeletionFile(pathCurrentFile);
                    }
                }
            }

            // Move the updated files from the temp to exe directory
            DirectoryHandler.MoveDirectory(pathTemp, pathCurrent, true);
            // Delete temp download directory
            DirectoryHandler.DestroyDirectory(pathTemp);
        }
    }
}
