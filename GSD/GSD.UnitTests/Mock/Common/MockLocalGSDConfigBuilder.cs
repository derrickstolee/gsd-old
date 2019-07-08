using GSD.Common;
using System.Collections.Generic;

namespace GSD.UnitTests.Mock.Common
{
    public class MockLocalGSDConfigBuilder
    {
        private string defaultRing;
        private string defaultUpgradeFeedUrl;
        private string defaultUpgradeFeedPackageName;
        private string defaultOrgServerUrl;

        private Dictionary<string, string> entries;

        public MockLocalGSDConfigBuilder(
            string defaultRing,
            string defaultUpgradeFeedUrl,
            string defaultUpgradeFeedPackageName,
            string defaultOrgServerUrl)
        {
            this.defaultRing = defaultRing;
            this.defaultUpgradeFeedUrl = defaultUpgradeFeedUrl;
            this.defaultUpgradeFeedPackageName = defaultUpgradeFeedPackageName;
            this.defaultOrgServerUrl = defaultOrgServerUrl;
            this.entries = new Dictionary<string, string>();
        }

        public MockLocalGSDConfigBuilder WithUpgradeRing(string value = null)
        {
            return this.With(GSDConstants.LocalGSDConfig.UpgradeRing, value ?? this.defaultRing);
        }

        public MockLocalGSDConfigBuilder WithNoUpgradeRing()
        {
            return this.WithNo(GSDConstants.LocalGSDConfig.UpgradeRing);
        }

        public MockLocalGSDConfigBuilder WithUpgradeFeedPackageName(string value = null)
        {
            return this.With(GSDConstants.LocalGSDConfig.UpgradeFeedPackageName, value ?? this.defaultUpgradeFeedPackageName);
        }

        public MockLocalGSDConfigBuilder WithNoUpgradeFeedPackageName()
        {
            return this.WithNo(GSDConstants.LocalGSDConfig.UpgradeFeedPackageName);
        }

        public MockLocalGSDConfigBuilder WithUpgradeFeedUrl(string value = null)
        {
            return this.With(GSDConstants.LocalGSDConfig.UpgradeFeedUrl, value ?? this.defaultUpgradeFeedUrl);
        }

        public MockLocalGSDConfigBuilder WithNoUpgradeFeedUrl()
        {
            return this.WithNo(GSDConstants.LocalGSDConfig.UpgradeFeedUrl);
        }

        public MockLocalGSDConfigBuilder WithOrgInfoServerUrl(string value = null)
        {
            return this.With(GSDConstants.LocalGSDConfig.OrgInfoServerUrl, value ?? this.defaultUpgradeFeedUrl);
        }

        public MockLocalGSDConfigBuilder WithNoOrgInfoServerUrl()
        {
            return this.WithNo(GSDConstants.LocalGSDConfig.OrgInfoServerUrl);
        }

        public MockLocalGSDConfig Build()
        {
            MockLocalGSDConfig gvfsConfig = new MockLocalGSDConfig();
            foreach (KeyValuePair<string, string> kvp in this.entries)
            {
                gvfsConfig.TrySetConfig(kvp.Key, kvp.Value, out _);
            }

            return gvfsConfig;
        }

        private MockLocalGSDConfigBuilder With(string key, string value)
        {
            this.entries.Add(key, value);
            return this;
        }

        private MockLocalGSDConfigBuilder WithNo(string key)
        {
            this.entries.Remove(key);
            return this;
        }
    }
}
