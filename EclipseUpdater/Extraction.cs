using System;
using System.IO;
using System.Threading.Tasks;
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
    class ExtractionHandler
    {
        public static bool ExtractFile(string pathTarget, string pathExtractTo, MainWindowViewModel gui)
        {
            gui.Progress = 0;
            string ext = Path.GetExtension(pathTarget);
            switch (ext)
            {
                case ".zip":
                    ExtractZip(pathTarget, pathExtractTo, gui); // ICSharpCode
                    break;
                case ".gz":
                    ExtractGZip(pathTarget, pathExtractTo, gui); // ICSharpCode
                    break;
                case ".tar":
                    ExtractTar(pathTarget, pathExtractTo, gui); // ICSharpCode
                    break;
                case ".rar":
                    ExtractRar(pathTarget, pathExtractTo, gui); // SharpCompress
                    break;
            }

            gui.Progress = 100;
            return true;
            //ICSharpCode.SharpZipLib
        }

        private static void ExtractZip(string pathTarget, string pathExtractTo, MainWindowViewModel gui, string password = "")
        {
            /*
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead(pathTarget);
                zf = new ZipFile(fs);
                if (!String.IsNullOrEmpty(password))
                {
                    zf.Password = password;     // AES encrypted entries are handled automatically
                }

                long totalSize = zf.Count;
                long totalRead = 0;

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

                    totalRead += 1;
                    gui.Progress = MainWindowViewModel.GetProgress(totalRead, totalSize);
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
            */

            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (Stream fs = new FileStream(pathTarget, FileMode.Open, FileAccess.Read))
            {
                long totalSize = fs.Length;
                long totalRead = 0;

                ZipInputStream zipInputStream = new ZipInputStream(fs);
                ZipEntry zipEntry = zipInputStream.GetNextEntry();
                while (zipEntry != null)
                {
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum

                    // Manipulate the output filename here as desired.
                    String fullZipToPath = Path.Combine(pathExtractTo, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Skip directory entry
                    string fileName = Path.GetFileName(fullZipToPath);
                    if (fileName.Length == 0)
                    {
                        zipEntry = zipInputStream.GetNextEntry();
                        continue;
                    }

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipInputStream, streamWriter, buffer);
                    }

                    totalRead += zipEntry.CompressedSize;
                    gui.Progress = MainWindowViewModel.GetProgress(totalRead, totalSize);

                    zipEntry = zipInputStream.GetNextEntry();
                }

                /*
                using (ZipInputStream zipStream = new ZipInputStream(fs))
                {
                    // Change this to your needs
                    string fnOut = Path.Combine(pathExtractTo, Path.GetFileNameWithoutExtension(pathTarget));

                    using (FileStream fsOut = File.Create(fnOut))
                    {
                        StreamUtils.Copy(zipStream, fsOut, dataBuffer);

                        int bytesRead = await fsOut.ReadAsync(buffer, 0, buffer.Length);
                        totalRead += bytesRead;
                    }
                    gui.Progress = MainWindowViewModel.GetProgress(totalRead, totalSize);
                }
                */
            }
        }

        // Code from https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples
        private static void ExtractGZip(string pathTarget, string pathExtractTo, MainWindowViewModel gui)
        {
            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];
            string pathTempExtractTo = Path.Combine(pathExtractTo, "temp");

            using (Stream fs = new FileStream(pathTarget, FileMode.Open, FileAccess.Read))
            {
                GZipInputStream gzip = new GZipInputStream(fs);
                using (GZipInputStream gzipStream = gzip)
                {
                    // Extract for further extraction
                    if (!Directory.Exists(pathTempExtractTo)) { DirectoryHandler.CreateDirectory(pathTempExtractTo); }
                    using (FileStream fsOut = File.Create(Path.Combine(pathTempExtractTo, Path.GetFileNameWithoutExtension(pathTarget))))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }

            // Extract the file again if needed (it should be needed)
            ExtractFile(Path.Combine(pathTempExtractTo, Path.GetFileNameWithoutExtension(pathTarget)), pathExtractTo, gui);
            // Delete that file to prevent it from being extracted again
            DirectoryHandler.DestroyDirectory(pathTempExtractTo);
        }

        // Code from https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples
        private static void ExtractTar(string pathTarget, string pathExtractTo, MainWindowViewModel gui)
        {
            /*
            Stream stream = File.OpenRead(pathTarget);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(stream);
            tarArchive.ExtractContents(pathExtractTo);
            tarArchive.Close();

            stream.Close();
            */


            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (Stream fs = new FileStream(pathTarget, FileMode.Open, FileAccess.Read))
            {
                long totalSize = fs.Length;
                long totalRead = 0;

                TarInputStream tarInputStream = new TarInputStream(fs);
                TarEntry tarEntry = tarInputStream.GetNextEntry();
                while (tarEntry != null)
                {
                    String entryFileName = tarEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum

                    // Manipulate the output filename here as desired.
                    String fullTarToPath = Path.Combine(pathExtractTo, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullTarToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Skip directory entry
                    string fileName = Path.GetFileName(fullTarToPath);
                    if (fileName.Length == 0)
                    {
                        tarEntry = tarInputStream.GetNextEntry();
                        continue;
                    }

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullTarToPath))
                    {
                        StreamUtils.Copy(tarInputStream, streamWriter, buffer);
                    }

                    totalRead += tarEntry.Size;
                    gui.Progress = MainWindowViewModel.GetProgress(totalRead, totalSize);

                    tarEntry = tarInputStream.GetNextEntry();
                }
            }
        }

        // Code from https://github.com/adamhathcock/sharpcompress/blob/master/USAGE.md
        private static void ExtractRar(string pathTarget, string pathExtractTo, MainWindowViewModel gui)
        {
            using (var archive = RarArchive.Open(pathTarget))
            {
                // Calculate the total extraction size.
                long totalSize = archive.TotalUncompressSize; //.Entries.Where(e => !e.IsDirectory).Sum(e => e.Size);
                long totalRead = 0;

                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(pathExtractTo, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });

                    // Track each individual extraction.
                    totalRead += entry.Size;
                    gui.Progress = MainWindowViewModel.GetProgress(totalRead, totalSize);
                }
            }
        }

    }
}
