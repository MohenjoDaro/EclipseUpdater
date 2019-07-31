using System;
using System.IO;
using System.Threading.Tasks;
using EclipseUpdater.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Flurl.Http;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

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
    }



    public class Config
    {
        public class ProjectData
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
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

        public static void LoadConfig()
        {
            try {
                if (File.Exists(cfgPath))
                {
                    ConfigFile = JsonConvert.DeserializeObject<Config>(File.ReadAllText(cfgPath));
                }
                else
                {
                    ConfigFile = new Config
                    {
                        Project = new Config.ProjectData
                        {
                            ID = Guid.Empty,
                            Name = "",
                            Description = ""
                        },
                        Version = new Config.VersionData
                        {
                            LocalVersion = "",
                            ID = -1,
                            UpdateDate = new DateTime()
                        }
                    };

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

        public static async Task<string> GetLatestVersion(Guid projectId)
        {
            try
            {
                var releasesClient = new ReleasesClient("https://releases.eclipseorigins.com");

                var releaseLocal = await releasesClient.GetReleasesAsync(projectId, 0, 1);
                return releaseLocal.Items[0].Version;
            } catch {
                return "";
            }
        }

        // Returns a string array of urls to download files
        public static async Task<string[]> GetUpdateUrls(Guid projectId)
        {
            try
            {
                var releasesClient = new ReleasesClient("https://releases.eclipseorigins.com");

                DateTime dateUpdate = ConfigHandler.ConfigFile.Version.UpdateDate;

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

        // Downloads a file from a given link
        public async static Task<bool> DownloadUpdate(string pathTarget, string url)
        {
            try
            {
                var path = await url.DownloadFileAsync(pathTarget);
                return true;
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("Error: " + e);
                return false;
            }
        }
    }



    class ExtractionHandler
    {
        public static void ExtractFile(string pathTarget, string pathExtractTo)
        {
            switch (Path.GetExtension(pathTarget))
            {
                case ".zip":
                    ExtractZip(pathTarget, pathExtractTo);
                    break;
                case ".gz":
                    ExtractGZip(pathTarget, pathExtractTo);
                    break;
                case ".tar":
                    ExtractTar(pathTarget, pathExtractTo);
                    break;
                case ".rar":
                    ExtractRar(pathTarget, pathExtractTo);
                    break;
            }
            //ICSharpCode.SharpZipLib
        }

        private static void ExtractZip(string pathTarget, string pathExtractTo, string password = "")
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead(pathTarget);
                zf = new ZipFile(fs);
                if (!String.IsNullOrEmpty(password))
                {
                    zf.Password = password;     // AES encrypted entries are handled automatically
                }
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    String fullZipToPath = Path.Combine(pathExtractTo, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
        }

        // Code from https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples
        private static void ExtractGZip(string pathTarget, string pathExtractTo)
        {
            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (Stream fs = new FileStream(pathTarget, FileMode.Open, FileAccess.Read))
            {
                using (GZipInputStream gzipStream = new GZipInputStream(fs))
                {
                    // Change this to your needs
                    string fnOut = Path.Combine(pathExtractTo, Path.GetFileNameWithoutExtension(pathTarget));

                    using (FileStream fsOut = File.Create(fnOut))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }
        }

        // Code from https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples
        private static void ExtractTar(string pathTarget, string pathExtractTo)
        {
            Stream stream = File.OpenRead(pathTarget);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(stream);
            tarArchive.ExtractContents(pathExtractTo);
            tarArchive.Close();

            stream.Close();
        }

        // Code from https://github.com/adamhathcock/sharpcompress/blob/master/USAGE.md
        private static void ExtractRar(string pathTarget, string pathExtractTo)
        {
            using (var archive = RarArchive.Open(pathTarget))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(pathExtractTo, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }
        
    }
}