using System;
using System.Collections.Generic;
using System.Text;

namespace EclipseUpdater.Api
{
    public class Release
    {
        public int Id { get; set; }
        public string Version { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool Prerelease { get; set; }
        public DownloadMode DownloadMode { get; set; }
        public string DownloadUrl { get; set; }
        public Guid DownloadReference { get; set; }
        public string CommitHash { get; set; }
        public ChannelDetails Channel { get; set; }
        public ChangelogEntry Changelog { get; set; }
    }
}
