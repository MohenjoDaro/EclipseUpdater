using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Input;

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

        int progress;

        public int Progress {
            get => progress;
            set => this.RaiseAndSetIfChanged(ref progress, value);
        }

        public MainWindowViewModel(Guid projectId) {
            this.projectId = projectId; // new Guid("0836bbd7-d9b4-466a-a566-7670bd568e3b");
            this.GameName = ConfigHandler.ConfigFile.Project.Name;
            this.Description = ConfigHandler.ConfigFile.Project.Description;

            this.UpdateCommand = ReactiveCommand.Create(UpdateCommandCallback);
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
                    DirectoryHandler.DestroyDirectory(pathTemp);

                    // Create temp directories for updating
                    DirectoryHandler.CreateDirectory(pathTemp);
                    DirectoryHandler.CreateDirectory(pathTempDownload);
                    DirectoryHandler.CreateDirectory(pathTempExtract);


                    // Download each update
                    for (int i = 0; i < urlDownloads.Length; i++) {
                        await UpdateHandler.DownloadUpdate(pathTempDownload, urlDownloads[i]);
                    }


                    // Check for extraction
                    foreach (string file in Directory.EnumerateFiles(pathTempDownload)) {
                        ExtractionHandler.ExtractFile(Path.Combine(pathTempDownload, file), pathTempExtract);
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
                    DirectoryHandler.MoveDirectory(pathTempExtract, Path.Combine(pathCurrent, nameProject), true);
                    // Delete temp update directory
                    DirectoryHandler.DestroyDirectory(pathTemp);
                }
            } catch {

            }
        }
    }
}
