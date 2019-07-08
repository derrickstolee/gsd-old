using GSD.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GSD.Common
{
    public class ServerGSDConfig
    {
        public IEnumerable<VersionRange> AllowedGSDClientVersions { get; set; }

        public IEnumerable<CacheServerInfo> CacheServers { get; set; } = Enumerable.Empty<CacheServerInfo>();

        public class VersionRange
        {
            public Version Min { get; set; }
            public Version Max { get; set; }
        }
    }
}