using GSD.Common;
using GSD.Common.NamedPipes;
using GSD.Common.Tracing;
using GSD.Tests.Should;
using GSD.UnitTests.Category;
using GSD.UnitTests.Mock.Common;
using GSD.UnitTests.Virtual;
using Moq;
using NUnit.Framework;
using System;

namespace GSD.UnitTests.Common
{
    [TestFixture]
    public class GSDLockTests : TestsWithCommonRepo
    {
        private static readonly NamedPipeMessages.LockData DefaultLockData = new NamedPipeMessages.LockData(
            pid: 1234,
            isElevated: false,
            checkAvailabilityOnly: false,
            parsedCommand: "git command",
            gitCommandSessionId: "123");

        [TestCase]
        public void TryAcquireAndReleaseLockForExternalRequestor()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "ReleaseLockHeldByExternalProcess", It.IsAny<EventMetadata>(), Keywords.Telemetry));
            MockPlatform mockPlatform = (MockPlatform)GSDPlatform.Instance;
            GSDLock gvfsLock = this.AcquireDefaultLock(mockPlatform, mockTracer.Object);

            mockPlatform.ActiveProcesses.Remove(DefaultLockData.PID);
            gvfsLock.ReleaseLockHeldByExternalProcess(DefaultLockData.PID);
            this.ValidateLockIsFree(gvfsLock);
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void ReleaseLockHeldByExternalProcess_WhenNoLock()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "ReleaseLockHeldByExternalProcess", It.IsAny<EventMetadata>(), Keywords.Telemetry));
            GSDLock gvfsLock = new GSDLock(mockTracer.Object);
            this.ValidateLockIsFree(gvfsLock);
            gvfsLock.ReleaseLockHeldByExternalProcess(DefaultLockData.PID).ShouldBeFalse();
            this.ValidateLockIsFree(gvfsLock);
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void ReleaseLockHeldByExternalProcess_DifferentPID()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "ReleaseLockHeldByExternalProcess", It.IsAny<EventMetadata>(), Keywords.Telemetry));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            MockPlatform mockPlatform = (MockPlatform)GSDPlatform.Instance;
            GSDLock gvfsLock = this.AcquireDefaultLock(mockPlatform, mockTracer.Object);
            gvfsLock.ReleaseLockHeldByExternalProcess(4321).ShouldBeFalse();
            this.ValidateLockHeld(gvfsLock, DefaultLockData);
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void ReleaseLockHeldByExternalProcess_WhenGSDHasLock()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "TryAcquireLockInternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "ReleaseLockHeldByExternalProcess", It.IsAny<EventMetadata>(), Keywords.Telemetry));
            GSDLock gvfsLock = this.AcquireGSDLock(mockTracer.Object);

            gvfsLock.ReleaseLockHeldByExternalProcess(DefaultLockData.PID).ShouldBeFalse();
            this.ValidateLockHeldByGSD(gvfsLock);
            mockTracer.VerifyAll();
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void ReleaseLockHeldByGSD_WhenNoLock()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            GSDLock gvfsLock = new GSDLock(mockTracer.Object);
            this.ValidateLockIsFree(gvfsLock);
            Assert.Throws<InvalidOperationException>(() => gvfsLock.ReleaseLockHeldByGSD());
            mockTracer.VerifyAll();
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void ReleaseLockHeldByGSD_WhenExternalHasLockShouldThrow()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            MockPlatform mockPlatform = (MockPlatform)GSDPlatform.Instance;
            GSDLock gvfsLock = this.AcquireDefaultLock(mockPlatform, mockTracer.Object);

            Assert.Throws<InvalidOperationException>(() => gvfsLock.ReleaseLockHeldByGSD());
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void TryAcquireLockForGSD()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "TryAcquireLockInternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "ReleaseLockHeldByGSD", It.IsAny<EventMetadata>()));
            GSDLock gvfsLock = this.AcquireGSDLock(mockTracer.Object);

            // Should be able to call again when GSD has the lock
            gvfsLock.TryAcquireLockForGSD().ShouldBeTrue();
            this.ValidateLockHeldByGSD(gvfsLock);

            gvfsLock.ReleaseLockHeldByGSD();
            this.ValidateLockIsFree(gvfsLock);
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void TryAcquireLockForGSD_WhenExternalLock()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "TryAcquireLockInternal", It.IsAny<EventMetadata>()));
            MockPlatform mockPlatform = (MockPlatform)GSDPlatform.Instance;
            GSDLock gvfsLock = this.AcquireDefaultLock(mockPlatform, mockTracer.Object);

            gvfsLock.TryAcquireLockForGSD().ShouldBeFalse();
            mockPlatform.ActiveProcesses.Remove(DefaultLockData.PID);
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void TryAcquireLockForExternalRequestor_WhenGSDLock()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "TryAcquireLockInternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            GSDLock gvfsLock = this.AcquireGSDLock(mockTracer.Object);

            NamedPipeMessages.LockData existingExternalHolder;
            gvfsLock.TryAcquireLockForExternalRequestor(DefaultLockData, out existingExternalHolder).ShouldBeFalse();
            this.ValidateLockHeldByGSD(gvfsLock);
            existingExternalHolder.ShouldBeNull();
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void TryAcquireLockForExternalRequestor_WhenExternalLock()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Verbose, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            MockPlatform mockPlatform = (MockPlatform)GSDPlatform.Instance;
            GSDLock gvfsLock = this.AcquireDefaultLock(mockPlatform, mockTracer.Object);

            NamedPipeMessages.LockData newLockData = new NamedPipeMessages.LockData(4321, false, false, "git new", "123");
            NamedPipeMessages.LockData existingExternalHolder;
            gvfsLock.TryAcquireLockForExternalRequestor(newLockData, out existingExternalHolder).ShouldBeFalse();
            this.ValidateLockHeld(gvfsLock, DefaultLockData);
            this.ValidateExistingExternalHolder(DefaultLockData, existingExternalHolder);
            mockPlatform.ActiveProcesses.Remove(DefaultLockData.PID);
            mockTracer.VerifyAll();
        }

        [TestCase]
        public void TryAcquireLockForExternalRequestor_WhenExternalHolderTerminated()
        {
            Mock<ITracer> mockTracer = new Mock<ITracer>(MockBehavior.Strict);
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "TryAcquireLockExternal", It.IsAny<EventMetadata>()));
            mockTracer.Setup(x => x.RelatedEvent(EventLevel.Informational, "ExternalLockHolderExited", It.IsAny<EventMetadata>(), Keywords.Telemetry));
            mockTracer.Setup(x => x.SetGitCommandSessionId(string.Empty));
            MockPlatform mockPlatform = (MockPlatform)GSDPlatform.Instance;
            GSDLock gvfsLock = this.AcquireDefaultLock(mockPlatform, mockTracer.Object);
            mockPlatform.ActiveProcesses.Remove(DefaultLockData.PID);

            NamedPipeMessages.LockData newLockData = new NamedPipeMessages.LockData(4321, false, false, "git new", "123");
            mockPlatform.ActiveProcesses.Add(newLockData.PID);
            NamedPipeMessages.LockData existingExternalHolder;
            gvfsLock.TryAcquireLockForExternalRequestor(newLockData, out existingExternalHolder).ShouldBeTrue();
            existingExternalHolder.ShouldBeNull();
            this.ValidateLockHeld(gvfsLock, newLockData);
            mockTracer.VerifyAll();
        }

        private GSDLock AcquireDefaultLock(MockPlatform mockPlatform, ITracer mockTracer)
        {
            GSDLock gvfsLock = new GSDLock(mockTracer);
            this.ValidateLockIsFree(gvfsLock);
            NamedPipeMessages.LockData existingExternalHolder;
            gvfsLock.TryAcquireLockForExternalRequestor(DefaultLockData, out existingExternalHolder).ShouldBeTrue();
            existingExternalHolder.ShouldBeNull();
            mockPlatform.ActiveProcesses.Add(DefaultLockData.PID);
            this.ValidateLockHeld(gvfsLock, DefaultLockData);
            return gvfsLock;
        }

        private GSDLock AcquireGSDLock(ITracer mockTracer)
        {
            GSDLock gvfsLock = new GSDLock(mockTracer);
            this.ValidateLockIsFree(gvfsLock);
            gvfsLock.TryAcquireLockForGSD().ShouldBeTrue();
            this.ValidateLockHeldByGSD(gvfsLock);
            return gvfsLock;
        }

        private void ValidateLockIsFree(GSDLock gvfsLock)
        {
            this.ValidateLock(gvfsLock, null, expectedStatus: "Free", expectedGitCommand: null, expectedIsAvailable: true);
        }

        private void ValidateLockHeldByGSD(GSDLock gvfsLock)
        {
            this.ValidateLock(gvfsLock, null, expectedStatus: "Held by GSD.", expectedGitCommand: null, expectedIsAvailable: false);
        }

        private void ValidateLockHeld(GSDLock gvfsLock, NamedPipeMessages.LockData expected)
        {
            this.ValidateLock(gvfsLock, expected, expectedStatus: $"Held by {expected.ParsedCommand} (PID:{expected.PID})", expectedGitCommand: expected.ParsedCommand, expectedIsAvailable: false);
        }

        private void ValidateLock(
            GSDLock gvfsLock,
            NamedPipeMessages.LockData expected,
            string expectedStatus,
            string expectedGitCommand,
            bool expectedIsAvailable)
        {
            gvfsLock.GetStatus().ShouldEqual(expectedStatus);
            NamedPipeMessages.LockData existingHolder;
            gvfsLock.IsLockAvailableForExternalRequestor(out existingHolder).ShouldEqual(expectedIsAvailable);
            this.ValidateExistingExternalHolder(expected, existingHolder);
            gvfsLock.GetLockedGitCommand().ShouldEqual(expectedGitCommand);
            NamedPipeMessages.LockData externalHolder = gvfsLock.GetExternalHolder();
            this.ValidateExistingExternalHolder(expected, externalHolder);
        }

        private void ValidateExistingExternalHolder(NamedPipeMessages.LockData expected, NamedPipeMessages.LockData actual)
        {
            if (actual != null)
            {
                expected.ShouldNotBeNull();
                actual.ShouldNotBeNull();
                actual.PID.ShouldEqual(expected.PID);
                actual.IsElevated.ShouldEqual(expected.IsElevated);
                actual.CheckAvailabilityOnly.ShouldEqual(expected.CheckAvailabilityOnly);
                actual.ParsedCommand.ShouldEqual(expected.ParsedCommand);
                actual.GitCommandSessionId.ShouldEqual(expected.GitCommandSessionId);
            }
            else
            {
                expected.ShouldBeNull();
            }
        }
    }
}
