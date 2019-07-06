using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EclipseUpdater.Api
{
    public interface IReleasesClient
    {
        Task<ReleaseList> GetReleasesAsync(Guid projectId, int offset, int count, int channelId = 0, bool includePrerelease = true);
    }
}
