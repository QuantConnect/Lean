/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.IO;
using Moq;
using NUnit.Framework;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class BackupUniverseFileDataProviderTests
    {
        private const string Key = "universes/20250815.csv";
        private const string BackupKey = "universes/20250815.csv.backup";

        [Test]
        public void ReturnsTheExpectedFileWithoutTouchingTheBackupFile()
        {
            var dataProvider = new Mock<IDataProvider>();
            dataProvider.Setup(dp => dp.Fetch(Key)).Returns(() => new MemoryStream());
            var backupDataProvider = new BackupUniverseFileDataProvider(dataProvider.Object);

            using var stream = backupDataProvider.Fetch(Key);

            Assert.IsNotNull(stream);
            dataProvider.Verify(dp => dp.Fetch(Key), Times.Once);
            dataProvider.Verify(dp => dp.Fetch(BackupKey), Times.Never);
        }

        [Test]
        public void FallsBackToTheBackupFileWhenTheExpectedFileIsNotAvailable()
        {
            var dataProvider = new Mock<IDataProvider>();
            dataProvider.Setup(dp => dp.Fetch(BackupKey)).Returns(() => new MemoryStream());
            var backupDataProvider = new BackupUniverseFileDataProvider(dataProvider.Object);

            using var stream = backupDataProvider.Fetch(Key);

            Assert.IsNotNull(stream);
            dataProvider.Verify(dp => dp.Fetch(Key), Times.Once);
            dataProvider.Verify(dp => dp.Fetch(BackupKey), Times.Once);
        }

        [Test]
        public void ReturnsNullWhenNeitherTheExpectedNorTheBackupFilesAreAvailable()
        {
            var dataProvider = new Mock<IDataProvider>();
            var backupDataProvider = new BackupUniverseFileDataProvider(dataProvider.Object);

            using var stream = backupDataProvider.Fetch(Key);

            Assert.IsNull(stream);
            dataProvider.Verify(dp => dp.Fetch(Key), Times.Once);
            dataProvider.Verify(dp => dp.Fetch(BackupKey), Times.Once);
        }

        [Test]
        public void UsesTheDataProviderSetAfterConstruction()
        {
            var dataProvider = new Mock<IDataProvider>();
            dataProvider.Setup(dp => dp.Fetch(BackupKey)).Returns(() => new MemoryStream());
            var backupDataProvider = new BackupUniverseFileDataProvider();
            backupDataProvider.SetDataProvider(dataProvider.Object);

            using var stream = backupDataProvider.Fetch(Key);

            Assert.IsNotNull(stream);
            dataProvider.Verify(dp => dp.Fetch(BackupKey), Times.Once);
        }

        [Test]
        public void ForwardsNewDataRequestEventsToTheWrappedDataProvider()
        {
            var dataProvider = new Mock<IDataProvider>();
            var backupDataProvider = new BackupUniverseFileDataProvider(dataProvider.Object);

            var raised = 0;
            EventHandler<DataProviderNewDataRequestEventArgs> handler = (_, _) => raised++;
            backupDataProvider.NewDataRequest += handler;
            dataProvider.Raise(dp => dp.NewDataRequest += null, new DataProviderNewDataRequestEventArgs(Key, true, ""));
            Assert.AreEqual(1, raised);

            backupDataProvider.NewDataRequest -= handler;
            dataProvider.Raise(dp => dp.NewDataRequest += null, new DataProviderNewDataRequestEventArgs(Key, true, ""));
            Assert.AreEqual(1, raised);
        }
    }
}
