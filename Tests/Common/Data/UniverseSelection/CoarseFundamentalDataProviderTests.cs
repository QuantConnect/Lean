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
using System.Text;
using NUnit.Framework;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class CoarseFundamentalDataProviderTests
    {
        private static readonly DateTime Date = new DateTime(2014, 03, 26);
        private const string CoarseLine = "SPY R735QTJ8XC9X,SPY,537.46,5483955,3490219402,True,0.5,0.25";

        [Test]
        public void FallsBackToBackupUniverseFileInLiveModeWhenExpectedFileIsNotAvailable()
        {
            var dataProvider = new BackupCoarseFileDataProvider(coarseFileAvailable: false, backupCoarseFileAvailable: true);
            var provider = CreateProvider(dataProvider, liveMode: true);

            var price = provider.Get<decimal>(Date, Symbols.SPY.ID, FundamentalProperty.Value);

            Assert.AreEqual(537.46m, price);
            Assert.AreEqual(1, dataProvider.CoarseFileRequests);
            Assert.AreEqual(1, dataProvider.BackupCoarseFileRequests);

            // the file contents are cached for the date, no further fetches
            var priceFactor = provider.Get<decimal>(Date, Symbols.SPY.ID, FundamentalProperty.PriceFactor);
            Assert.AreEqual(0.5m, priceFactor);
            Assert.AreEqual(1, dataProvider.CoarseFileRequests);
            Assert.AreEqual(1, dataProvider.BackupCoarseFileRequests);
        }

        [Test]
        public void DoesNotFallBackToBackupUniverseFileWhenExpectedFileIsAvailable()
        {
            var dataProvider = new BackupCoarseFileDataProvider(coarseFileAvailable: true, backupCoarseFileAvailable: true);
            var provider = CreateProvider(dataProvider, liveMode: true);

            var price = provider.Get<decimal>(Date, Symbols.SPY.ID, FundamentalProperty.Value);

            Assert.AreEqual(537.46m, price);
            Assert.AreEqual(1, dataProvider.CoarseFileRequests);
            Assert.AreEqual(0, dataProvider.BackupCoarseFileRequests);
        }

        [Test]
        public void DoesNotFallBackToBackupUniverseFileWhenNotInLiveMode()
        {
            var dataProvider = new BackupCoarseFileDataProvider(coarseFileAvailable: false, backupCoarseFileAvailable: true);
            var provider = CreateProvider(dataProvider, liveMode: false);

            var price = provider.Get<decimal>(Date, Symbols.SPY.ID, FundamentalProperty.Value);

            Assert.AreEqual(decimal.Zero, price);
            Assert.AreEqual(1, dataProvider.CoarseFileRequests);
            Assert.AreEqual(0, dataProvider.BackupCoarseFileRequests);
        }

        [Test]
        public void ReturnsDefaultsInLiveModeWhenNeitherTheExpectedNorTheBackupUniverseFilesAreAvailable()
        {
            var dataProvider = new BackupCoarseFileDataProvider(coarseFileAvailable: false, backupCoarseFileAvailable: false);
            var provider = CreateProvider(dataProvider, liveMode: true);

            var price = provider.Get<decimal>(Date, Symbols.SPY.ID, FundamentalProperty.Value);

            Assert.AreEqual(decimal.Zero, price);
            Assert.AreEqual(1, dataProvider.CoarseFileRequests);
            Assert.AreEqual(1, dataProvider.BackupCoarseFileRequests);
        }

        // tuesday: the requested date file. Saturday and sunday: forward fills friday's file
        [TestCase("2014-03-25", 544.99, 1)]
        [TestCase("2014-03-29", 536.86, 2)]
        [TestCase("2014-03-30", 536.86, 2)]
        public void ForwardFillsPreviousTradingDayDataWhenThereIsNoDataForTheRequestedDate(string dateStr, decimal expectedPrice, int expectedFetches)
        {
            var dataProvider = new CountingDataProvider(TestGlobals.DataProvider);
            var provider = CreateProvider(dataProvider, liveMode: false);
            var date = DateTime.ParseExact(dateStr, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var price = provider.Get<decimal>(date, Symbols.AAPL.ID, FundamentalProperty.Value);

            Assert.AreEqual(expectedPrice, price);
            Assert.AreEqual(expectedFetches, dataProvider.Requests);
        }

        [Test]
        public void DoesNotForwardFillBeyondThePreviousTradingDay()
        {
            // sunday, and there is no data for the previous trading day (friday 21st) either
            var dataProvider = new CountingDataProvider(TestGlobals.DataProvider);
            var provider = CreateProvider(dataProvider, liveMode: false);

            var price = provider.Get<decimal>(new DateTime(2014, 03, 23), Symbols.AAPL.ID, FundamentalProperty.Value);

            Assert.AreEqual(decimal.Zero, price);
            Assert.AreEqual(2, dataProvider.Requests);
        }

        [Test]
        public void DoesNotReloadAnAlreadyLoadedFile()
        {
            var dataProvider = new CountingDataProvider(TestGlobals.DataProvider);
            var provider = CreateProvider(dataProvider, liveMode: false);

            // saturday: forward fills friday's file
            Assert.AreEqual(536.86m, provider.Get<decimal>(new DateTime(2014, 03, 29), Symbols.AAPL.ID, FundamentalProperty.Value));
            Assert.AreEqual(2, dataProvider.Requests);

            // sunday: friday's file is already loaded
            Assert.AreEqual(536.86m, provider.Get<decimal>(new DateTime(2014, 03, 30), Symbols.AAPL.ID, FundamentalProperty.Value));
            Assert.AreEqual(3, dataProvider.Requests);

            // monday: the requested date file
            Assert.AreEqual(536.74m, provider.Get<decimal>(new DateTime(2014, 03, 31), Symbols.AAPL.ID, FundamentalProperty.Value));
            Assert.AreEqual(4, dataProvider.Requests);
        }

        private static CoarseFundamentalDataProvider CreateProvider(IDataProvider dataProvider, bool liveMode)
        {
            var provider = new CoarseFundamentalDataProvider();
            provider.Initialize(dataProvider, liveMode);
            return provider;
        }

        private class CountingDataProvider : IDataProvider
        {
            private readonly IDataProvider _dataProvider;

            public int Requests { get; private set; }

#pragma warning disable 0067 // the event is never used
            public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;
#pragma warning restore 0067

            public CountingDataProvider(IDataProvider dataProvider)
            {
                _dataProvider = dataProvider;
            }

            public Stream Fetch(string key)
            {
                Requests++;
                return _dataProvider.Fetch(key);
            }
        }

        private class BackupCoarseFileDataProvider : IDataProvider
        {
            private readonly string _coarseFilePath = Path.Combine(Globals.DataFolder, "equity", "usa", "fundamental", "coarse", $"{Date:yyyyMMdd}.csv");
            private readonly bool _coarseFileAvailable;
            private readonly bool _backupCoarseFileAvailable;

            public int CoarseFileRequests { get; private set; }

            public int BackupCoarseFileRequests { get; private set; }

#pragma warning disable 0067 // the event is never used
            public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;
#pragma warning restore 0067

            public BackupCoarseFileDataProvider(bool coarseFileAvailable, bool backupCoarseFileAvailable)
            {
                _coarseFileAvailable = coarseFileAvailable;
                _backupCoarseFileAvailable = backupCoarseFileAvailable;
            }

            public Stream Fetch(string key)
            {
                if (key == _coarseFilePath)
                {
                    CoarseFileRequests++;
                    return _coarseFileAvailable ? new MemoryStream(Encoding.UTF8.GetBytes(CoarseLine)) : null;
                }

                if (key == _coarseFilePath + ".backup")
                {
                    BackupCoarseFileRequests++;
                    return _backupCoarseFileAvailable ? new MemoryStream(Encoding.UTF8.GetBytes(CoarseLine)) : null;
                }

                return null;
            }
        }
    }
}
