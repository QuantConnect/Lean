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
using QuantConnect.Util;
using System.Globalization;
using Microsoft.CodeAnalysis;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class DataDownloaderGetParameterExtensionsTests
    {
        private readonly IMapFileProvider _mapFileProvider = Composer.Instance.GetPart<IMapFileProvider>();

        [TestCase("../../../Data/equity/usa/hour/spwr.zip", 2, "SPWR", "2005/11/17-2008/09/29,2011/11/16-2024/03/07")]
        [TestCase("../../../Data/equity/usa/hour/meta.zip", 1, "META", "1899/12/30-2024/03/07", Description = "Not presented in mapped files")]
        [TestCase("../../../Data/equity/usa/hour/fb.zip", 2, "FB", "1999/09/29-2003/03/28,2012/05/18-2022/06/08")]
        [TestCase("../../../Data/equity/usa/hour/goog.zip", 2, "GOOG", "2004/08/19-2014/04/02,2014/04/02-2024/03/07")]
        [TestCase("../../../Data/equity/usa/daily/goog.zip", 2, "GOOG", "2004/08/19-2014/04/02,2014/04/02-2024/03/07")]
        [TestCase("../../../Data/equity/usa/daily/spwr.zip", 2, "SPWR", "2005/11/17-2008/09/29,2011/11/16-2024/03/07")]
        [TestCase("../../../Data/equity/usa/minute/spwr/20140401_trade.zip", 1, "SPWR", "4/1/2014-4/2/2014")]
        [TestCase("../../../Data/equity/usa/minute/fb/20000401_trade.zip", 1, "FB", "2000/04/01-2000/04/02")]
        [TestCase("../../../Data/equity/usa/minute/xyz/20000401_trade.zip", 1, "XYZ", null)]
        [TestCase("../../../Data/cfd/oanda/daily/xauusd.zip", 1, "XAUUSD", null)]
        [TestCase("../../../Data/option/usa/daily/goog_2015_quote_american.zip", 2, "GOOG", "2004/08/19-2014/04/02,2014/04/02-2024/03/07")]
        public void GetDataDownloaderParam(string pathToFile, int expectedDownloadDataAmount, string expectedTicker, string expectedDateTimeRanges)
        {
            _ = LeanData.TryParsePath(pathToFile, out var symbol, out var parsedDate, out var resolution, out var tickType, out _);

            var startDateTimeUtc = default(DateTime);
            var endDateTimeUtc = new DateTime(2024, 3, 7);

            if (parsedDate != default)
            {
                startDateTimeUtc = parsedDate;
                endDateTimeUtc = parsedDate.AddDays(1);
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

            var downloaderDataParameters = _mapFileProvider.GetAllTickerFromMapFiles(symbol, resolution, startDateTimeUtc, endDateTimeUtc, tickType).OrderBy(d => d.StartUtc).ToList();

            Assert.That(downloaderDataParameters.Count, Is.EqualTo(expectedDownloadDataAmount));

            var dateRanges = expectedDateTimeRanges?.Split(',').Select(x => x.Split('-'))
                .ToList(x => (StartDateTime: DateTime.Parse(x[0], CultureInfo.InvariantCulture), EndDateTime: DateTime.Parse(x[1], CultureInfo.InvariantCulture)));

            for (var i = 0; i < downloaderDataParameters.Count; i++)
            {
                var downloaderParameter = downloaderDataParameters[i];

                Assert.That(downloaderParameter.Symbol.Value, Is.EqualTo(expectedTicker));

                if (dateRanges != null)
                {
                    Assert.That(downloaderParameter.StartUtc, Is.EqualTo(dateRanges[i].StartDateTime));
                    Assert.That(downloaderParameter.EndUtc, Is.GreaterThanOrEqualTo(dateRanges[i].EndDateTime));
                }
            }
        }
    }
}
