using GSD.FunctionalTests.Tools;
using NUnit.Framework;
using System.Collections.Generic;

namespace GSD.FunctionalTests.Tests.MultiEnlistmentTests
{
    public class TestsWithMultiEnlistment
    {
        private List<GSDFunctionalTestEnlistment> enlistmentsToDelete = new List<GSDFunctionalTestEnlistment>();

        [TearDown]
        public void DeleteEnlistments()
        {
            foreach (GSDFunctionalTestEnlistment enlistment in this.enlistmentsToDelete)
            {
                enlistment.UnmountAndDeleteAll();
            }

            this.OnTearDownEnlistmentsDeleted();

            this.enlistmentsToDelete.Clear();
        }

        /// <summary>
        /// Can be overridden for custom [TearDown] steps that occur after the test enlistements have been unmounted and deleted
        /// </summary>
        protected virtual void OnTearDownEnlistmentsDeleted()
        {
        }

        protected GSDFunctionalTestEnlistment CreateNewEnlistment(
            string localCacheRoot = null,
            string branch = null,
            bool skipPrefetch = false)
        {
            GSDFunctionalTestEnlistment output = GSDFunctionalTestEnlistment.CloneAndMount(
                GSDTestConfig.PathToGSD,
                branch,
                localCacheRoot,
                skipPrefetch);
            this.enlistmentsToDelete.Add(output);
            return output;
        }
    }
}
