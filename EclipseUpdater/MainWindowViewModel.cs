using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;

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

        public MainWindowViewModel(Guid projectId)
        {
            this.projectId = projectId; // new Guid("0836bbd7-d9b4-466a-a566-7670bd568e3b");
            this.GameName = ConfigHandler.ConfigFile.Project.Name;
            this.Description = ConfigHandler.ConfigFile.Project.Description;

            this.UpdateCommand = ReactiveCommand.Create(UpdateCommandCallback);

            //this.UpdateStatus = "Checking for Updates...";
            GuiUpdateVersion(projectId);
        }

        private async void GuiUpdateVersion(Guid projectId)
        {
            this.LocalVersion = "Local: " + ConfigHandler.ConfigFile.Version.LocalVersion;

            string versionCurrent = await UpdateHandler.GetLatestVersion(projectId);
            this.LatestVersion = "Latest: " + versionCurrent;

            //this.UpdateStatus = (ConfigHandler.ConfigFile.Version.LocalVersion == versionCurrent) ? "Launch!" : "Update!";
        }

        private async void UpdateCommandCallback() {
            try {
                // Check for updates
                string[] urlDownloads = await UpdateHandler.GetUpdateUrls(projectId);
                string pathTemp = Path.Combine(Path.GetTempPath(), "updater_temp");
                string pathTempDownload = Path.Combine(pathTemp, "download");
                string pathTempExtract = Path.Combine(pathTemp, "extract");

                if (urlDownloads.Length != 0) {
                    // Delete temp update directory incase one is still hanging around
//                    DirectoryHandler.DestroyDirectory(pathTemp);

                    // Create temp directories for updating
                    DirectoryHandler.CreateDirectory(pathTemp);
                    DirectoryHandler.CreateDirectory(pathTempDownload);
                    DirectoryHandler.CreateDirectory(pathTempExtract);


                    // Download each update
                    for (int i = 0; i < urlDownloads.Length; i++) {
//                        await Download.DownloadUpdate(Path.Combine(pathTempDownload), i, urlDownloads[i], this);
                    }


                    // Check for extraction
                    foreach (string file in Directory.EnumerateFiles(pathTempDownload)) {
                        await Task.Run(() => ExtractionHandler.ExtractFile(Path.Combine(pathTempDownload, file), pathTempExtract, this));
                    }


                    string pathCurrent = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    // Rename the updates if it needs updates
                    if (projectId == Constants.UpdaterId) // This will be the UPDATERS's ID
                    {
                        // Get the list of directory names from the temp download directory
                        foreach (string dir in Directory.EnumerateDirectories(pathTempExtract)) {
                            string pathCurrentDirectory = Path.Combine(pathCurrent, dir);
                            if (File.Exists(pathCurrentDirectory)) {
                                FileHandler.MarkForDeletionFile(pathCurrentDirectory);
                            }
                        }

                        // Get the list of file names from the temp download directory
                        foreach (string file in Directory.EnumerateFiles(pathTempExtract)) {
                            string pathCurrentFile = Path.Combine(pathCurrent, file);
                            if (File.Exists(pathCurrentFile)) {
                                FileHandler.MarkForDeletionFile(pathCurrentFile);
                            }
                        }
                    }


                    // Move the updated files from the temp to exe directory
                    string nameProject = ConfigHandler.ConfigFile.Project.Name;
                    await Task.Run(() => DirectoryHandler.MoveDirectory(pathTempExtract, Path.Combine(pathCurrent, nameProject), true));
                    // Delete temp update directory
//                    DirectoryHandler.DestroyDirectory(pathTemp);


                    // Update the version to the latest
                    /*ConfigHandler.ConfigFile.Version.LocalVersion = await UpdateHandler.GetLatestVersion(projectId);
                    ConfigHandler.ConfigFile.Version.UpdateDate = DateTime.UtcNow;
                    ConfigHandler.SaveConfig();
                    GuiUpdateVersion(projectId);*/
                }
            } catch {

            }
        }
    }
}
