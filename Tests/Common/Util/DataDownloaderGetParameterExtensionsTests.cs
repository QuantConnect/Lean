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
 *
*/

using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using System.Globalization;
using Microsoft.CodeAnalysis;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class DataDownloaderGetParameterExtensionsTests
    {
        private MarketHoursDatabase _marketHoursDatabase;
        private IMapFileProvider _mapFileProvider;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _mapFileProvider = TestGlobals.MapFileProvider;
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        }

        [TestCase("../../../Data/equity/usa/hour/spwr.zip", 2, "SPWR", "2005/11/17-2008/09/29,2011/11/17-2024/03/07")]
        [TestCase("../../../Data/equity/usa/hour/meta.zip", 1, "META", "1899/12/30-2024/03/07", Description = "Not presented in mapped files")]
        [TestCase("../../../Data/equity/usa/hour/fb.zip", 2, "FB", "1999/09/29-2003/03/28,2012/05/18-2024/03/07")]
        [TestCase("../../../Data/equity/usa/hour/goog.zip", 2, "GOOG", "2004/08/19-2014/04/02,2014/04/03-2024/03/07")]
        [TestCase("../../../Data/equity/usa/daily/goog.zip", 2, "GOOG", "2004/08/19-2014/04/02,2014/04/03-2024/03/07")]
        [TestCase("../../../Data/equity/usa/daily/spwr.zip", 2, "SPWR", "2005/11/17-2008/09/29,2011/11/17-2024/03/07")]
        [TestCase("../../../Data/equity/usa/minute/spwr/20140401_trade.zip", 1, "SPWR", "2014/04/01-2014/04/01")]
        [TestCase("../../../Data/equity/usa/minute/spwra/20100401_trade.zip", 1, "SPWRA", "2010/04/01-2010/04/01")]
        [TestCase("../../../Data/equity/usa/minute/fb/20000401_trade.zip", 1, "FB", "2000/04/01-2000/04/02")]
        [TestCase("../../../Data/equity/usa/minute/fb/20200401_trade.zip", 1, "FB", "2020/04/01-2000/04/02")]
        [TestCase("../../../Data/equity/usa/minute/xyz/20000401_trade.zip", 1, "XYZ", null)]
        [TestCase("../../../Data/cfd/oanda/daily/xauusd.zip", 1, "XAUUSD", null)]
        [TestCase("../../../Data/option/usa/hour/goog_2014_quote_american.zip", 2, "GOOG", "2014/01/01-2014/04/02,2014/04/03-2015/01/01")]
        [TestCase("../../../Data/option/usa/daily/goog_2014_quote_american.zip", 2, "GOOG", "2014/01/01-2014/04/02,2014/04/03-2015/01/01")]
        [TestCase("../../../Data/indexoption/usa/hour/spx_2014_quote_american.zip", 1, "SPX", "2014/01/01-2015/01/01")]
        [TestCase("../../../Data/indexoption/usa/daily/spx_2014_quote_american.zip", 1, "SPX", "2014/01/01-2015/01/01")]
        [TestCase("../../../Data/crypto/binance/hour/btcusdt_trade.zip", 1, "BTCUSDT", null)]
        [TestCase("../../../Data/cryptofuture/binance/hour/btcusdt_trade.zip", 1, "BTCUSDT", null)]
        [TestCase("../../../Data/futureoption/comex/minute/og/20200428/20200105_quote_american.zip", 1, "GC28J20", "2020/01/05-2020/01/06")]
        [TestCase("../../../Data/option/usa/minute/goog/20151223_trade_american", 1, "GOOG", "2015/12/23-2015/12/24")]
        [TestCase("../../../Data/future/cme/minute/es/20131008_quote.zip", 1, "/ES", "2013/10/08-2013/10/09")]
        public void GetDataDownloaderParam(string pathToFile, int expectedDownloadDataAmount, string expectedTicker, string expectedDateTimeRanges)
        {
            _ = LeanData.TryParsePath(pathToFile, out var symbol, out var parsedDate, out var resolution, out var tickType, out _);

            var startDateTimeUtc = default(DateTime);
            var endDateTimeUtc = new DateTime(2024, 3, 7);

            if (parsedDate != default)
            {
                startDateTimeUtc = parsedDate;

                if (resolution > Resolution.Minute &&
                    (symbol.SecurityType == SecurityType.Option || symbol.SecurityType == SecurityType.IndexOption))
                {
                    endDateTimeUtc = startDateTimeUtc.AddYears(1);
                }
                else
                {
                    endDateTimeUtc = parsedDate.AddDays(1);
                }
            }
            else
            {
                try
                {
                    startDateTimeUtc = symbol.ID.Date;
                }
                catch (InvalidOperationException)
                {
                    startDateTimeUtc = new DateTime(1999, 1, 1);
                }
            }

            MarketHoursDatabase.Entry entry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);

            var getParams = new DataDownloaderGetParameters(symbol, resolution, startDateTimeUtc, endDateTimeUtc, tickType);

            var downloaderDataParameters = getParams.GetDataDownloaderParameterForAllMappedSymbols(_mapFileProvider, entry.ExchangeHours.TimeZone).OrderBy(d => d.StartUtc).ToList();

            Assert.That(downloaderDataParameters.Count, Is.EqualTo(expectedDownloadDataAmount));

            var dateRanges = expectedDateTimeRanges?.Split(',').Select(x => x.Split('-'))
                .ToList(x => (StartDateTime: DateTime.Parse(x[0], CultureInfo.InvariantCulture), EndDateTime: DateTime.Parse(x[1], CultureInfo.InvariantCulture)));

            for (var i = 0; i < downloaderDataParameters.Count; i++)
            {
                var downloaderParameter = downloaderDataParameters[i];

                var rightTicker = downloaderParameter.Symbol.HasUnderlying ? downloaderParameter.Symbol.Underlying.Value : downloaderParameter.Symbol.Value;
                Assert.That(rightTicker, Is.EqualTo(expectedTicker));

                Assert.That(downloaderParameter.Symbol.SecurityType, Is.EqualTo(symbol.SecurityType));

                if (dateRanges != null)
                {
                    Assert.That(downloaderParameter.StartUtc.Date, Is.EqualTo(dateRanges[i].StartDateTime.Date));
                    Assert.That(downloaderParameter.EndUtc.Date, Is.GreaterThanOrEqualTo(dateRanges[i].EndDateTime.Date));
                }
            }
        }
    }
}
