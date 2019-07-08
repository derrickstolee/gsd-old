using GSD.Common;
using GSD.Common.Git;
using GSD.Common.Http;
using GSD.Tests.Should;
using GSD.UnitTests.Mock.Common;
using GSD.UnitTests.Mock.Git;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GSD.UnitTests.Common
{
    [TestFixture]
    public class CacheServerResolverTests
    {
        private const string CacheServerUrl = "https://cache/server";
        private const string CacheServerName = "TestCacheServer";

        [TestCase]
        public void CanGetCacheServerFromNewConfig()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment(CacheServerUrl);
            CacheServerInfo cacheServer = CacheServerResolver.GetCacheServerFromConfig(enlistment);

            cacheServer.Url.ShouldEqual(CacheServerUrl);
            CacheServerResolver.GetUrlFromConfig(enlistment).ShouldEqual(CacheServerUrl);
        }

        [TestCase]
        public void CanGetCacheServerFromOldConfig()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment(null, CacheServerUrl);
            CacheServerInfo cacheServer = CacheServerResolver.GetCacheServerFromConfig(enlistment);

            cacheServer.Url.ShouldEqual(CacheServerUrl);
            CacheServerResolver.GetUrlFromConfig(enlistment).ShouldEqual(CacheServerUrl);
        }

        [TestCase]
        public void CanGetCacheServerWithNoConfig()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment();

            this.ValidateIsNone(enlistment, CacheServerResolver.GetCacheServerFromConfig(enlistment));
            CacheServerResolver.GetUrlFromConfig(enlistment).ShouldEqual(enlistment.RepoUrl);
        }

        [TestCase]
        public void CanResolveUrlForKnownName()
        {
            CacheServerResolver resolver = this.CreateResolver();

            CacheServerInfo resolvedCacheServer;
            string error;
            resolver.TryResolveUrlFromRemote(CacheServerName, this.CreateGSDConfig(), out resolvedCacheServer, out error);

            resolvedCacheServer.Url.ShouldEqual(CacheServerUrl);
            resolvedCacheServer.Name.ShouldEqual(CacheServerName);
        }

        [TestCase]
        public void CanResolveNameFromKnownUrl()
        {
            CacheServerResolver resolver = this.CreateResolver();
            CacheServerInfo resolvedCacheServer = resolver.ResolveNameFromRemote(CacheServerUrl, this.CreateGSDConfig());

            resolvedCacheServer.Url.ShouldEqual(CacheServerUrl);
            resolvedCacheServer.Name.ShouldEqual(CacheServerName);
        }

        [TestCase]
        public void CanResolveNameFromCustomUrl()
        {
            const string CustomUrl = "https://not/a/known/cache/server";

            CacheServerResolver resolver = this.CreateResolver();
            CacheServerInfo resolvedCacheServer = resolver.ResolveNameFromRemote(CustomUrl, this.CreateGSDConfig());

            resolvedCacheServer.Url.ShouldEqual(CustomUrl);
            resolvedCacheServer.Name.ShouldEqual(CacheServerInfo.ReservedNames.UserDefined);
        }

        [TestCase]
        public void CanResolveUrlAsRepoUrl()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment();
            CacheServerResolver resolver = this.CreateResolver(enlistment);

            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl, this.CreateGSDConfig()));
            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl + "/", this.CreateGSDConfig()));
            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl + "//", this.CreateGSDConfig()));
            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl.ToUpper(), this.CreateGSDConfig()));
            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl.ToUpper() + "/", this.CreateGSDConfig()));
            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl.ToLower(), this.CreateGSDConfig()));
            this.ValidateIsNone(enlistment, resolver.ResolveNameFromRemote(enlistment.RepoUrl.ToLower() + "/", this.CreateGSDConfig()));
        }

        [TestCase]
        public void CanParseUrl()
        {
            CacheServerResolver resolver = new CacheServerResolver(new MockTracer(), this.CreateEnlistment());
            CacheServerInfo parsedCacheServer = resolver.ParseUrlOrFriendlyName(CacheServerUrl);

            parsedCacheServer.Url.ShouldEqual(CacheServerUrl);
            parsedCacheServer.Name.ShouldEqual(CacheServerInfo.ReservedNames.UserDefined);
        }

        [TestCase]
        public void CanParseName()
        {
            CacheServerResolver resolver = new CacheServerResolver(new MockTracer(), this.CreateEnlistment());
            CacheServerInfo parsedCacheServer = resolver.ParseUrlOrFriendlyName(CacheServerName);

            parsedCacheServer.Url.ShouldEqual(null);
            parsedCacheServer.Name.ShouldEqual(CacheServerName);
        }

        [TestCase]
        public void CanParseAndResolveDefault()
        {
            CacheServerResolver resolver = this.CreateResolver();

            CacheServerInfo parsedCacheServer = resolver.ParseUrlOrFriendlyName(null);
            parsedCacheServer.Url.ShouldEqual(null);
            parsedCacheServer.Name.ShouldEqual(CacheServerInfo.ReservedNames.Default);

            CacheServerInfo resolvedCacheServer;
            string error;
            resolver.TryResolveUrlFromRemote(parsedCacheServer.Name, this.CreateGSDConfig(), out resolvedCacheServer, out error);

            resolvedCacheServer.Url.ShouldEqual(CacheServerUrl);
            resolvedCacheServer.Name.ShouldEqual(CacheServerName);
        }

        [TestCase]
        public void CanParseAndResolveNoCacheServer()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment();
            CacheServerResolver resolver = this.CreateResolver(enlistment);

            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(CacheServerInfo.ReservedNames.None));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl + "/"));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl + "//"));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl.ToUpper()));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl.ToUpper() + "/"));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl.ToLower()));
            this.ValidateIsNone(enlistment, resolver.ParseUrlOrFriendlyName(enlistment.RepoUrl.ToLower() + "/"));

            CacheServerInfo resolvedCacheServer;
            string error;
            resolver.TryResolveUrlFromRemote(CacheServerInfo.ReservedNames.None, this.CreateGSDConfig(), out resolvedCacheServer, out error)
                .ShouldEqual(false, "Should not succeed in resolving the name 'None'");

            resolvedCacheServer.ShouldEqual(null);
            error.ShouldNotBeNull();
        }

        [TestCase]
        public void CanParseAndResolveDefaultWhenServerAdvertisesNullListOfCacheServers()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment();
            CacheServerResolver resolver = this.CreateResolver(enlistment);

            CacheServerInfo resolvedCacheServer;
            string error;
            resolver.TryResolveUrlFromRemote(CacheServerInfo.ReservedNames.Default, this.CreateDefaultDeserializedGSDConfig(), out resolvedCacheServer, out error)
                .ShouldEqual(true);

            this.ValidateIsNone(enlistment, resolvedCacheServer);
        }

        [TestCase]
        public void CanParseAndResolveOtherWhenServerAdvertisesNullListOfCacheServers()
        {
            MockGSDEnlistment enlistment = this.CreateEnlistment();
            CacheServerResolver resolver = this.CreateResolver(enlistment);

            CacheServerInfo resolvedCacheServer;
            string error;
            resolver.TryResolveUrlFromRemote(CacheServerInfo.ReservedNames.None, this.CreateDefaultDeserializedGSDConfig(), out resolvedCacheServer, out error)
                .ShouldEqual(false, "Should not succeed in resolving the name 'None'");

            resolvedCacheServer.ShouldEqual(null);
            error.ShouldNotBeNull();
        }

        private void ValidateIsNone(Enlistment enlistment, CacheServerInfo cacheServer)
        {
            cacheServer.Url.ShouldEqual(enlistment.RepoUrl);
            cacheServer.Name.ShouldEqual(CacheServerInfo.ReservedNames.None);
        }

        private MockGSDEnlistment CreateEnlistment(string newConfigValue = null, string oldConfigValue = null)
        {
            MockGitProcess gitProcess = new MockGitProcess();
            gitProcess.SetExpectedCommandResult(
                "config --local gvfs.cache-server",
                () => new GitProcess.Result(newConfigValue ?? string.Empty, string.Empty, newConfigValue != null ? GitProcess.Result.SuccessCode : GitProcess.Result.GenericFailureCode));
            gitProcess.SetExpectedCommandResult(
                "config gvfs.mock:..repourl.cache-server-url",
                () => new GitProcess.Result(oldConfigValue ?? string.Empty, string.Empty, oldConfigValue != null ? GitProcess.Result.SuccessCode : GitProcess.Result.GenericFailureCode));

            return new MockGSDEnlistment(gitProcess);
        }

        private ServerGSDConfig CreateGSDConfig()
        {
            return new ServerGSDConfig
            {
                CacheServers = new[]
                {
                    new CacheServerInfo(CacheServerUrl, CacheServerName, globalDefault: true),
                }
            };
        }

        private ServerGSDConfig CreateDefaultDeserializedGSDConfig()
        {
            return JsonConvert.DeserializeObject<ServerGSDConfig>("{}");
        }

        private CacheServerResolver CreateResolver(MockGSDEnlistment enlistment = null)
        {
            enlistment = enlistment ?? this.CreateEnlistment();
            return new CacheServerResolver(new MockTracer(), enlistment);
        }
    }
}
