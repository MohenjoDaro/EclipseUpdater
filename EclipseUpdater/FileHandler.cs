using System;
using System.IO;
using System.Threading.Tasks;
using EclipseUpdater.Api;
using Newtonsoft.Json;

namespace EclipseUpdater
{
    class DirectoryHandler
    {
        public static bool CreateDirectory(string pathTarget)
        {
            try {
                Directory.CreateDirectory(pathTarget);
                return true;
            } catch {
                return false;
            }
        }

        public static bool MoveDirectory(string pathTarget, string pathDestination, bool onlyChildren = false)
        {
            try {
                string pathNewDestination = pathDestination;
                if (!onlyChildren) { pathNewDestination = Path.Combine(pathNewDestination, Path.GetDirectoryName(pathTarget)); }


                foreach (string dir in Directory.EnumerateDirectories(pathTarget))
                {
                    //Directory.Move(dir, pathDestination);
                    string nameDirectory = new DirectoryInfo(dir).Name;
                    string pathTemp = Path.Combine(pathNewDestination, nameDirectory);
                    Directory.CreateDirectory(pathTemp);

                    DirectoryHandler.MoveDirectory(dir, pathTemp, true);
                }

                foreach (string file in Directory.EnumerateFiles(pathTarget))
                {
                    string nameFile = new FileInfo(file).Name;
                    FileHandler.CopyFile(file, Path.Combine(pathNewDestination, nameFile));
                }

                return true;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Error: " + e);
                return false;
            }
            catch (IOException e)
            {
                Console.WriteLine("Error: " + e);
                return false;
            }
        }

        public static bool RenameDirectory(string pathTarget, string nameDirectory)
        {
            try {
                Directory.Move(pathTarget, nameDirectory);
                return true;
            } catch {
                return false;
            }
        }

        public static bool MarkForDeletionDirectory(string pathTarget)
        {
            try {
                RenameDirectory(pathTarget, pathTarget + ".del");
                return true;
            } catch {
                return false;
            }
        }

        public static bool DestroyDirectory(string pathTarget)
        {
            try {
                Directory.Delete(pathTarget, true);
                return true;
            } catch {
                return false;
            }
        }

        public static bool DestroyMarkedForDeletion(string pathTarget)
        {
            try {
                foreach (string dir in Directory.EnumerateDirectories(pathTarget, "*.del"))
                {
                    DestroyDirectory(Path.Combine(pathTarget, dir));
                }

                foreach (string file in Directory.EnumerateFiles(pathTarget, "*.del"))
                {
                    FileHandler.DestroyFile(Path.Combine(pathTarget, file));
                }

                return true;
            } catch {
                return false;
            }
        }
    }

    class FileHandler
    {
        public static bool CopyFile(string pathTarget, string pathDestination)
        {
            try
            {
                File.Copy(pathTarget, pathDestination, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool MoveFile(string pathTarget, string pathDestination)
        {
            try {
                File.Move(pathTarget, pathDestination);
                return true;
            } catch {
                return false;
            }
        }

        public static bool RenameFile(string pathTarget, string nameFile)
        {
            try {
                File.Move(pathTarget, Path.Combine(Path.GetDirectoryName(pathTarget), nameFile));
                return true;
            } catch {
                return false;
            }
        }

        public static bool MarkForDeletionFile(string pathTarget)
        {
            try {
                RenameFile(pathTarget, pathTarget + ".del");
                return true;
            } catch {
                return false;
            }
        }

        public static bool DestroyFile(string pathTarget)
        {
            try {
                File.Delete(pathTarget);
                return true;
            } catch {
                return false;
            }
        }

        public static bool RunFile(string pathTarget)
        {
            try
            {
                System.Diagnostics.Process.Start(pathTarget);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }



    public class Config
    {
        public class ProjectData
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

            public class LaunchData
            {
                public string[] Path { get; set; }
            }
            public LaunchData Launch = new LaunchData(); // { get; set; }
        }

        public class VersionData
        {
            public string LocalVersion { get; set; }
            public int ID { get; set; }
            public DateTime UpdateDate { get; set; }
        }

        public ProjectData Project = new ProjectData(); // { get; set; }
        public VersionData Version = new VersionData(); // { get; set; }
    }

    class ConfigHandler
    {
        private static readonly string cfgPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "config.json");
        public static Config ConfigFile { get; set; }

        private static void CreateConfig()
        {
            try
            {
                ConfigFile = new Config
                {
                    Project = new Config.ProjectData
                    {
                        ID = Guid.Empty,
                        Name = "",
                        Description = "",
                        Launch = new Config.ProjectData.LaunchData()
                        {
                            Path = new string[] { "" }
                        }
                    },
                    Version = new Config.VersionData
                    {
                        LocalVersion = "",
                        ID = -1,
                        UpdateDate = new DateTime()
                    }
                };
            } catch {

            }
        }

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(cfgPath))
                {
                    ConfigFile = JsonConvert.DeserializeObject<Config>(File.ReadAllText(cfgPath));
                }
                else
                {
                    CreateConfig();

                    SaveConfig();
                }
            } catch {

            }
        }
        
        public static void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(ConfigFile, Formatting.Indented);
                File.WriteAllText(cfgPath, json);
            }
            catch
            {
                
            }
        }
    }



    class UpdateHandler
    {
        private const int countPage = 25;

        public static async Task<bool> CheckForUpdates(Guid projectId, string version)
        {
            try
            {
                string versionCurrent = await UpdateHandler.GetLatestVersion(projectId);
                return (versionCurrent == version) ? false : true;
            } catch {
                return false;
            }
        }

        public static async Task<string> GetLatestVersion(Guid projectId)
        {
            try
            {
                var releasesClient = new ReleasesClient("https://releases.eclipseorigins.com");

                var releaseLocal = await releasesClient.GetReleasesAsync(projectId, 0, 1);
                return (releaseLocal.Items.Count == 0) ? "" : releaseLocal.Items[0].Version;
            } catch {
                return "";
            }
        }

        // Returns a string array of urls to download files
        public static async Task<string[]> GetUpdateUrls(Guid projectId, DateTime dateUpdate)
        {
            try
            {
                var releasesClient = new ReleasesClient("https://releases.eclipseorigins.com");

                var releaseNewest = await releasesClient.GetReleasesAsync(projectId, 0, 1, minimumDate: dateUpdate);
                int cntReleases = releaseNewest.TotalCount;

                string[] urlReleases = new string[cntReleases];

                // Check if the versions match
                if (releaseNewest.Items[0].Id == ConfigHandler.ConfigFile.Version.ID) { return null; }

                // Get each page of updates
                int i = 0;
                while (i < cntReleases)
                {
                    var releases = await releasesClient.GetReleasesAsync(projectId, 0, 1);

                    for (int r = 0; r < releases.Items.Count; ++r)
                    {
                        urlReleases[r + i] = releases.Items[r].GetDownloadUrl();
                    }

                    i += countPage;
                }

                return urlReleases;
            }
            catch
            {
                return null;
            }
        }
    }
}