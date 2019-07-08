using GSD.Common.Http;

namespace GSD.UnitTests.Mock
{
    public class MockCacheServerInfo : CacheServerInfo
    {
        public MockCacheServerInfo() : base("https://mock", "mock")
        {
        }
    }
}
