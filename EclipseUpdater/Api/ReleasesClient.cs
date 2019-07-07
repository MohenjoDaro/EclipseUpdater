using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace EclipseUpdater.Api
{
    public class ReleasesClient : IReleasesClient
    {
        private readonly string baseUri;

        public ReleasesClient(string baseUri) {
            this.baseUri = baseUri;
        }

        public Task<ReleaseList> GetReleasesAsync(Guid projectId, int offset, int count, int channelId = 0, bool includePrerelease = true, DateTime? minimumDate = null) {
            return baseUri.AppendPathSegments("api", "v1", "releases", projectId)
                          .SetQueryParams(new
                          {
                              Offset = offset,
                              Count = count,
                              ChannelId = channelId,
                              IncludePrerelease = includePrerelease,
                              MinimumDate = minimumDate
                          })
                          .GetJsonAsync<ReleaseList>();
        }
    }
}
