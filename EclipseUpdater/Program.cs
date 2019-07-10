using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;
using EclipseUpdater.Api;

namespace EclipseUpdater
{
    class Program
    {
        private static string idProject;
        private const string idUpdater = ""; // This is the project ID for the updater, do NOT change this 

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

            ConfigHandler.LoadConfig();
            // Load the project ID
            // Remove the set value and use the config value when it's setup
            idProject = "0836bbd7-d9b4-466a-a566-7670bd568e3b"; // ConfigHandler.GetConfigSetting("id");

            //Task.Run(() => UpdateTarget(idProject));

            app.Run(new MainWindow());
        }



        private static async Task UpdateTarget(string id)
        {
            try
            {
                // Check for updates
                string[] urlDownloads = await UpdateHandler.GetUpdateUrls(idProject);
                string pathTemp = Path.Combine(Path.GetTempPath(), "updater_temp");
                string pathTempDownload = Path.Combine(pathTemp, "download");
                string pathTempExtract = Path.Combine(pathTemp, "extract");
                if (urlDownloads.Length != 0)
                {
                    // Delete temp update directory incase one is still hanging around
                    DirectoryHandler.DestroyDirectory(pathTemp);

                    // Create temp directories for updating
                    DirectoryHandler.CreateDirectory(pathTemp);
                    DirectoryHandler.CreateDirectory(pathTempDownload);
                    DirectoryHandler.CreateDirectory(pathTempExtract);


                    // Download each update
                    for (int i = 0; i < urlDownloads.Length; i++)
                    {
                        await UpdateHandler.DownloadUpdate(pathTempDownload, urlDownloads[i]);
                    }


                    // Check for extraction
                    foreach (string file in Directory.EnumerateFiles(pathTempDownload))
                    {
                        ExtractionHandler.ExtractFile(Path.Combine(pathTempDownload, file), pathTempExtract);
                    }


                    string pathCurrent = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    // Rename the updates if it needs updates
                    if (id == idUpdater) // This will be the UPDATERS's ID
                    {
                        // Get the list of directory names from the temp download directory
                        foreach (string dir in Directory.EnumerateDirectories(pathTempExtract))
                        {
                            string pathCurrentDirectory = Path.Combine(pathCurrent, dir);
                            if (File.Exists(pathCurrentDirectory))
                            {
                                FileHandler.MarkForDeletionFile(pathCurrentDirectory);
                            }
                        }

                        // Get the list of file names from the temp download directory
                        foreach (string file in Directory.EnumerateFiles(pathTempExtract))
                        {
                            string pathCurrentFile = Path.Combine(pathCurrent, file);
                            if (File.Exists(pathCurrentFile))
                            {
                                FileHandler.MarkForDeletionFile(pathCurrentFile);
                            }
                        }
                    }


                    // Move the updated files from the temp to exe directory
                    string nameProject = ConfigHandler.GetConfigSetting("name")?.ToString() ?? "";
                    DirectoryHandler.MoveDirectory(pathTempExtract, Path.Combine(pathCurrent, nameProject), true);
                    // Delete temp update directory
                    DirectoryHandler.DestroyDirectory(pathTemp);
                }
            }
            catch
            {

            }
        }

        private static async Task TestReleases() {
            var releasesClient = new ReleasesClient("https://releases.eclipseorigins.com");

            var releases = await releasesClient.GetReleasesAsync(new Guid("0836bbd7-d9b4-466a-a566-7670bd568e3b"), 0, 1);

            if (releases.Items.Count > 0) {
                var latestRelease = releases.Items[0];

                var downloadUrl = latestRelease.GetDownloadUrl();
            }
        }
    }
}
