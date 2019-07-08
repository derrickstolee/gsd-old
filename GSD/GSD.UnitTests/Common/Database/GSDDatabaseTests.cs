using GSD.Common.Database;
using GSD.Tests.Should;
using GSD.UnitTests.Category;
using GSD.UnitTests.Mock.FileSystem;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace GSD.UnitTests.Common.Database
{
    [TestFixture]
    public class GSDDatabaseTests
    {
        [TestCase]
        public void ConstructorTest()
        {
            this.TestGSDDatabase(null);
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void ConstructorThrowsGSDDatabaseException()
        {
            GSDDatabaseException ex = Assert.Throws<GSDDatabaseException>(() => this.TestGSDDatabase(null, throwException: true));
            ex.Message.ShouldEqual("GSDDatabase constructor threw exception setting up connection pool and initializing");
            ex.InnerException.Message.ShouldEqual("Error");
        }

        [TestCase]
        public void DisposeTest()
        {
            this.TestGSDDatabase(database => database.Dispose());
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void GetConnectionAfterDisposeShouldThrowException()
        {
            this.TestGSDDatabase(database =>
                {
                    database.Dispose();
                    IGSDConnectionPool connectionPool = database;
                    Assert.Throws<ObjectDisposedException>(() => connectionPool.GetConnection());
                });
        }

        [TestCase]
        public void GetConnectionMoreThanInPoolTest()
        {
            this.TestGSDDatabase(database =>
            {
                IGSDConnectionPool connectionPool = database;
                using (IDbConnection pooledConnection1 = connectionPool.GetConnection())
                using (IDbConnection pooledConnection2 = connectionPool.GetConnection())
                {
                    pooledConnection1.Equals(pooledConnection2).ShouldBeFalse();
                }
            });
        }

        private void TestGSDDatabase(Action<GSDDatabase> testCode, bool throwException = false)
        {
            MockFileSystem fileSystem = new MockFileSystem(new MockDirectory("GSDDatabaseTests", null, null));

            Mock<IDbCommand> mockCommand = new Mock<IDbCommand>(MockBehavior.Strict);
            mockCommand.SetupSet(x => x.CommandText = "PRAGMA journal_mode=WAL;");
            mockCommand.SetupSet(x => x.CommandText = "PRAGMA cache_size=-40000;");
            mockCommand.SetupSet(x => x.CommandText = "PRAGMA synchronous=NORMAL;");
            mockCommand.SetupSet(x => x.CommandText = "PRAGMA user_version;");
            mockCommand.Setup(x => x.ExecuteNonQuery()).Returns(1);
            mockCommand.Setup(x => x.ExecuteScalar()).Returns(1);
            mockCommand.Setup(x => x.Dispose());

            Mock<IDbCommand> mockCommand2 = new Mock<IDbCommand>(MockBehavior.Strict);
            mockCommand2.SetupSet(x => x.CommandText = "CREATE TABLE IF NOT EXISTS [Placeholder] (path TEXT PRIMARY KEY COLLATE NOCASE, pathType TINYINT NOT NULL, sha char(40) ) WITHOUT ROWID;");
            if (throwException)
            {
                mockCommand2.Setup(x => x.ExecuteNonQuery()).Throws(new Exception("Error"));
            }
            else
            {
                mockCommand2.Setup(x => x.ExecuteNonQuery()).Returns(1);
            }

            mockCommand2.Setup(x => x.Dispose());

            List<Mock<IDbConnection>> mockConnections = new List<Mock<IDbConnection>>();
            Mock<IDbConnection> mockConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockConnection.SetupSequence(x => x.CreateCommand())
                          .Returns(mockCommand.Object)
                          .Returns(mockCommand2.Object);
            mockConnection.Setup(x => x.Dispose());
            mockConnections.Add(mockConnection);

            Mock<IDbConnectionFactory> mockConnectionFactory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
            bool firstConnection = true;
            string databasePath = Path.Combine("mock:root", ".mockvfsforgit", "databases", "VFSForGit.sqlite");
            mockConnectionFactory.Setup(x => x.OpenNewConnection(databasePath)).Returns(() =>
            {
                if (firstConnection)
                {
                    firstConnection = false;
                    return mockConnection.Object;
                }
                else
                {
                    Mock<IDbConnection> newMockConnection = new Mock<IDbConnection>(MockBehavior.Strict);
                    newMockConnection.Setup(x => x.Dispose());
                    mockConnections.Add(newMockConnection);
                    return newMockConnection.Object;
                }
            });

            using (GSDDatabase database = new GSDDatabase(fileSystem, "mock:root", mockConnectionFactory.Object, initialPooledConnections: 1))
            {
                testCode?.Invoke(database);
            }

            mockCommand.Verify(x => x.Dispose(), Times.Once);
            mockCommand2.Verify(x => x.Dispose(), Times.Once);
            mockConnections.ForEach(connection => connection.Verify(x => x.Dispose(), Times.Once));

            mockCommand.VerifyAll();
            mockCommand2.VerifyAll();
            mockConnections.ForEach(connection => connection.VerifyAll());
            mockConnectionFactory.VerifyAll();
        }
    }
}
