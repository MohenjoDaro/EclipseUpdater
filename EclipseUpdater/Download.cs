using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Util;

namespace EclipseUpdater
{
    class Download
    {


        // Downloads a file from a given link
        public async static Task<bool> DownloadUpdate(string pathTarget, string url, MainWindowViewModel gui)
        {
            try
            {
                FlurlClient client = new FlurlClient{ BaseUrl = url };
                var response = await url.GetAsync();

                response.EnsureSuccessStatusCode();
                long? totalBytes = response.Content.Headers.ContentLength;

                using (var streamFile = await response.Content.ReadAsStreamAsync())
                {
                    long totalBytesRead = 0L;
                    byte[] buffer = new byte[1024]; // 8192
                    bool isComplete = false;

                    // Flurl code to get file name
                    var header = response.Content.Headers.ContentDisposition;
                    string pathFile = (header == null) ? "" : string.Join("_", (header.FileNameStar ?? header.FileName)?.StripQuotes().Split(Path.GetInvalidFileNameChars()));
                    
                    using (FileStream fileStream = new FileStream(Path.Combine(pathTarget, pathFile), FileMode.Create, FileAccess.Write, FileShare.None, 1024, true))
                    {
                        do
                        {
                            int bytesRead = await streamFile.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                isComplete = true;
                                gui.Progress = 100;
                                continue;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;

                            gui.Progress = GetProgress(totalBytesRead, totalBytes);
                        }
                        while (!isComplete);
                    }
                }

                return true;
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("Error: " + e);
                return false;
            }
        }

        private static int GetProgress(long totalBytesRead, long? totalBytes)
        {
            try
            {
                int percent = Convert.ToInt32((totalBytesRead * 100) / ((totalBytes == null) ? 0 : totalBytes));
                return percent;
            } catch {
                return 0;
            }
        }
    }
}
