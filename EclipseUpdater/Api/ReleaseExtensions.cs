using System;
using System.Collections.Generic;
using System.Text;

namespace EclipseUpdater.Api
{
    public static class ReleaseExtensions
    {
        public static string GetDownloadUrl(this Release release) {
            switch (release.DownloadMode) {
                case DownloadMode.External: {
                        return release.DownloadUrl;
                    }
                case DownloadMode.Upload: {
                        return $"https://files.eclipseorigins.com/download/{release.DownloadReference}";
                    }
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
