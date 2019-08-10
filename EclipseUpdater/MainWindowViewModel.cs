using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace EclipseUpdater
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly Guid projectId;

        public ICommand UpdateCommand { get; }

        string gameName;
        public string GameName {
            get => gameName;
            set => this.RaiseAndSetIfChanged(ref gameName, value);
        }

        string description;
        public string Description {
            get => description;
            set => this.RaiseAndSetIfChanged(ref description, value);
        }

        string versionLocal;
        public string LocalVersion
        {
            get => versionLocal;
            set => this.RaiseAndSetIfChanged(ref versionLocal, value);
        }

        string versionLatest;
        public string LatestVersion
        {
            get => versionLatest;
            set => this.RaiseAndSetIfChanged(ref versionLatest, value);
        }

        int progress;
        public int Progress {
            get => progress;
            set => this.RaiseAndSetIfChanged(ref progress, value);
        }
        public static int GetProgress(long totalBytesRead, long? totalBytes)
        {
            try {
                int percent = Convert.ToInt32((totalBytesRead * 100) / ((totalBytes == null) ? 0 : totalBytes));
                if (percent > 100) { percent = 100; }
                return percent;
            } catch { return 0; }
        }

        string progressText;
        public string ProgressText
        {
            get => progressText;
            set => this.RaiseAndSetIfChanged(ref progressText, value);
        }

        private bool isUpdated = false;


        public MainWindowViewModel(Guid projectId)
        {
            this.projectId = projectId; // new Guid("0836bbd7-d9b4-466a-a566-7670bd568e3b");
            this.GameName = ConfigHandler.ConfigFile.Project.Name;
            this.Description = ConfigHandler.ConfigFile.Project.Description;

            this.UpdateCommand = ReactiveCommand.Create(UpdateCommandCallback);
            GuiUpdateVersion(projectId);

            CheckForSelfUpdates();
        }

        private async void GuiUpdateVersion(Guid projectId)
        {
            this.LocalVersion = "Local: " + ConfigHandler.ConfigFile.Version.LocalVersion;

            this.ProgressText = "Checking for Updates...";

            string versionCurrent = await UpdateHandler.GetLatestVersion(projectId);
            this.LatestVersion = "Latest: " + versionCurrent;

            isUpdated = (versionCurrent == ConfigHandler.ConfigFile.Version.LocalVersion);

            this.ProgressText = (isUpdated) ? "Up to Date!" : "New Update(s) Available!";
            
        }

        private async void CheckForSelfUpdates()
        {
            Guid idUpdater = Constants.UpdaterId;
            if (await UpdateHandler.CheckForUpdates(idUpdater, Constants.UpdaterVersion))
            {
                var response = await MessageBox.Show(
                    "New Version!",
                    "There is a new version of the updater avaiable." + Environment.NewLine + "Would you like to download it now?",
                    new string[] { "Update!", "Dismiss" });

                switch (response)
                {
                    case 0:
                        string pathSelf = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
                        await UpdateProgram(idUpdater, Constants.UpdaterDate);
                        FileHandler.RunFile(pathSelf);
                        Process.GetCurrentProcess().Kill();
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task UpdateCommandCallback() {
            if (isUpdated)
            {
                LaunchPrograms();
            } else {
                await UpdateProgram(projectId, ConfigHandler.ConfigFile.Version.UpdateDate);
            }
        }

        private async Task UpdateProgram(Guid projectId, DateTime dateUpdate)
        {
            try
            {
                // Check for updates
                string[] urlDownloads = await UpdateHandler.GetUpdateUrls(projectId, dateUpdate);
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
                        string progressText = "Downloading file " + (i + 1) + "/" + urlDownloads.Length;
                        await Download.DownloadUpdate(Path.Combine(pathTempDownload), i, urlDownloads[i], this, progressText);
                    }


                    // Check for extraction
                    var files = from file in Directory.EnumerateFiles(pathTempDownload) select file;
                    int curFile = 1;
                    foreach (string file in files)
                    {
                        string progressText = "Extracting file " + curFile + "/" + files.Count();
                        await Task.Run(() => ExtractionHandler.ExtractFile(Path.Combine(pathTempDownload, file), pathTempExtract, this, progressText));
                        ++curFile;
                    }


                    // Check if we're updating the Updater itself
                    // Rename any new files so they can be updated if we are
                    // These files will be deleted after the updater is restarted
                    string pathCurrent = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    if (projectId == Constants.UpdaterId) // This will be the UPDATERS's ID
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
                            string pathCurrentFile = Path.Combine(pathCurrent, Path.GetFileName(file));
                            if (File.Exists(pathCurrentFile))
                            {
                                FileHandler.MarkForDeletionFile(pathCurrentFile);
                            }
                        }

                        // Move the updated files from the temp to exe directory
                        await Task.Run(() => DirectoryHandler.MoveDirectory(pathTempExtract, pathCurrent, true));
                        // Delete temp update directory
                        DirectoryHandler.DestroyDirectory(pathTemp);
                    }
                    else
                    {
                        // Move the updated files from the temp to exe directory
                        this.ProgressText = "Moving Files...";
                        string nameProject = ConfigHandler.ConfigFile.Project.Name;
                        await Task.Run(() => DirectoryHandler.MoveDirectory(pathTempExtract, Path.Combine(pathCurrent, nameProject), true));
                        // Delete temp update directory
                        this.ProgressText = "Deleting Temp Files...";
                        DirectoryHandler.DestroyDirectory(pathTemp);

                        this.ProgressText = "";

                        // Update the version to the latest
                        ConfigHandler.ConfigFile.Version.LocalVersion = await UpdateHandler.GetLatestVersion(projectId);
                        ConfigHandler.ConfigFile.Version.UpdateDate = DateTime.UtcNow;
                        ConfigHandler.SaveConfig();
                        GuiUpdateVersion(projectId);
                    }

                }
            }
            catch
            {

            }
        }

        private void LaunchPrograms()
        {
            string pathCurrent = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string[] paths = ConfigHandler.ConfigFile.Project.Launch.Path;
            for (int i = 0; i < paths.Length; ++i)
            {
                // Split the path in the file up to prevent path issues
                FileHandler.RunFile(Path.Combine(pathCurrent, Path.Combine(paths[i].Split("/"))));
            }
        }
    }
}
