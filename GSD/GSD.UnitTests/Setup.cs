using GSD.Common;
using GSD.UnitTests.Mock.Common;
using NUnit.Framework;

namespace GSD.UnitTests
{
    [SetUpFixture]
    public class Setup
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            GSDPlatform.Register(new MockPlatform());
        }
    }
}
