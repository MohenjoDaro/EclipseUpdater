using System;
using System.Collections.Generic;
using System.Text;

namespace EclipseUpdater.Api
{
    public class ReleaseList
    {
        public int TotalCount { get; set; }
        public List<Release> Items { get; set; }

        public ReleaseList() {
            this.Items = new List<Release>();
        }
    }
}
