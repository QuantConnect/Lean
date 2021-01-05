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
using System.Globalization;
using System.IO;
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class LocalDiskFactorFileProviderTests
    {
        [Test]
        public void RetrievesFromDisk()
        {
            var provider = new LocalDiskFactorFileProvider();
            var factorFile = provider.Get(Symbols.SPY);
            Assert.IsNotNull(factorFile);
        }

        [Test]
        public void CachesValueAndReturnsSameReference()
        {
            var provider = new LocalDiskFactorFileProvider();
            var factorFile1 = provider.Get(Symbols.SPY);
            var factorFile2 = provider.Get(Symbols.SPY);
            Assert.IsTrue(ReferenceEquals(factorFile1, factorFile2));
        }

        [Test]
        public void ReturnsNullForNotFound()
        {
            var provider = new LocalDiskFactorFileProvider();
            var factorFile = provider.Get(Symbol.Create("not-a-ticker", SecurityType.Equity, QuantConnect.Market.USA));
            Assert.IsNull(factorFile);
        }

        [Test, Ignore("This test is meant to be run manually")]
        public void FindsFactorFilesWithErrors()
        {
            var provider = new LocalDiskFactorFileProvider();
            var factorFileFolder = Path.Combine(Globals.DataFolder, "equity", QuantConnect.Market.USA, "factor_files");

            foreach (var fileName in Directory.EnumerateFiles(factorFileFolder))
            {
                var ticker = Path.GetFileNameWithoutExtension(fileName).ToUpper(CultureInfo.InvariantCulture);
                var symbol = Symbol.Create(ticker, SecurityType.Equity, QuantConnect.Market.USA);

                try
                {
                    provider.Get(symbol);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(ticker + ": " + exception.Message);
                }
            }
        }
    }
}