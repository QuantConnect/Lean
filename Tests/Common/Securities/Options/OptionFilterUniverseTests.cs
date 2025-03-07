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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptionFilterUniverseTests
    {
        private static string TestOptionUniverseFile = @"
#symbol_id,symbol_value,open,high,low,close,volume,open_interest,implied_volatility,delta,gamma,vega,theta,rho\n
SPX 31,SPX,5488.47998046875,5523.64013671875,5451.1201171875,5460.47998046875,7199220000,,,,,,,
SPX Z3DLU7UBV6SU|SPX 31,SPX   260618C05400000,780.3000,853.9000,709.6000,767.7500,0,135,0.1637928,0.6382026,0.0002890,26.5721377,-0.5042690,55.5035521
SPX Z8DSNBS7V966|SPX 31,SPX   261218C05400000,893.1400,907.7100,893.1400,907.5400,37,1039,0.1701839,0.6420671,0.0002447,28.9774913,-0.4608812,67.5259867
SPX ZIC7DCXNPVLA|SPX 31,SPX   271217C05400000,1073.0000,1073.0000,1073.0000,1073.0000,0,889,0.1839256,0.6456981,0.0001858,32.6109403,-0.3963479,88.5870185
SPX ZSAM3E33KI0E|SPX 31,SPX   281215C05400000,1248.0000,1248.0000,1248.0000,1248.0000,0,301,0.1934730,0.6472619,0.0001512,35.1083627,-0.3434647,106.9858230
SPX 102FWY2SPYEJY|SPX 31,SPX   291221C05400000,1467.9000,1467.9000,1467.9000,1467.9000,0,9,0.2046702,0.6460372,0.0001254,36.9157598,-0.2993105,122.2236355
SPX YK9CDJQAQJJI|SPX 31,SPX   240719C05405000,95.4500,95.4500,95.4500,95.4500,1,311,0.1006795,0.6960459,0.0026897,4.4991247,-1.4284818,2.0701880
SPX YL0WW5Z0VO1A|SPX 31,SPX   240816C05405000,161.4000,161.4000,161.4000,161.4000,0,380,0.1088739,0.6472976,0.0017128,7.3449930,-1.1139626,4.5112640
SPX YLZDJFRXK2NI|SPX 31,SPX   240920C05405000,213.7000,213.7000,211.0000,211.0000,0,33,0.1149306,0.6316343,0.0012532,9.7567496,-0.9462173,7.4872272
SPX YMQY220NP75A|SPX 31,SPX   241018C05405000,254.0000,303.3500,218.2500,238.0500,0,,0.1183992,0.6273390,0.0010556,11.2892617,-0.8673778,9.8420483
SPX YK9CCXDVSOI6|SPX 31,SPX   240719C05410000,143.5900,143.5900,119.7100,119.7100,11,355,0.0995106,0.6842402,0.0027673,4.5750811,-1.4291241,2.0364155
SPX YL0WVJMLXSZY|SPX 31,SPX   240816C05410000,151.2000,151.2000,151.2000,151.2000,0,68,0.1080883,0.6395066,0.0017388,7.4027436,-1.1113164,4.4598077
SPX YLZDITFIM7M6|SPX 31,SPX   240920C05410000,202.5000,202.5000,201.9800,201.9800,0,211,0.1142983,0.6258911,0.0012667,9.8073284,-0.9438102,7.4239078
SPX YMQY1FO8RC3Y|SPX 31,SPX   241018C05410000,256.4800,256.4800,255.9000,255.9000,0,91,0.1180060,0.6223570,0.0010637,11.3388534,-0.8661655,9.7694707
SPX YNIIK1WYWGLQ|SPX 31,SPX   241115C05410000,279.7500,279.7500,279.2300,279.2300,0,65,0.1268034,0.6170056,0.0008881,12.7072390,-0.8357895,11.9829003
SPX YK9CDJRY9W1A|SPX 31,SPX   240719C05415000,123.1800,123.1800,98.0300,98.0300,5,307,0.0985516,0.6716430,0.0028403,4.6505424,-1.4312099,2.0001484
SPX YL0WW60OF0J2|SPX 31,SPX   240816C05415000,146.6900,146.6900,146.6900,146.6900,3,901,0.1073207,0.6315307,0.0017645,7.4585091,-1.1084001,4.4069495
SPX YLZDJFTL3F5A|SPX 31,SPX   240920C05415000,194.1000,196.7000,194.1000,196.7000,0,63,0.1136398,0.6200837,0.0012804,9.8561442,-0.9410592,7.3597879
SPX YMQY222B8JN2|SPX 31,SPX   241018C05415000,246.5000,295.7500,210.7500,230.9500,0,,0.1172852,0.6175838,0.0010746,11.3844988,-0.8632046,9.7014393
SPX YK9CCXE1R0JY|SPX 31,SPX   240719C05420000,119.7500,119.7500,94.0000,94.0000,31,453,0.0973479,0.6589639,0.0029188,4.7207612,-1.4288180,1.9636645
SPX YL0WVJMRW51Q|SPX 31,SPX   240816C05420000,181.5800,181.5800,154.8300,154.8300,4,110,0.1065704,0.6233721,0.0017897,7.5120648,-1.1051922,4.3527055
";

        private BaseData _underlying;
        private List<OptionUniverse> _testOptionsData;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var config = new SubscriptionDataConfig(typeof(OptionUniverse),
                Symbols.SPX,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var date = new DateTime(2024, 06, 28);

            _testOptionsData = new List<OptionUniverse>();
            var factory = new OptionUniverse();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TestOptionUniverseFile));
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var data = (OptionUniverse)factory.Reader(config, reader, date, false);
                if (data != null)
                {
                    if (data.Symbol.HasUnderlying)
                    {
                        _testOptionsData.Add(data);
                    }
                    else
                    {
                        _underlying = data;
                    }
                }
            }
        }

        [Test]
        public void FiltersContractsByImpliedVolatility()
        {
            var minIV = 0.10m;
            var maxIV = 0.12m;
            var expectedContracts = 11;

            // Set up
            var universe = new OptionFilterUniverse(GetOption(), _testOptionsData, _underlying);
            universe.Refresh(_testOptionsData, _underlying, _underlying.EndTime);

            // Filter
            universe.ImpliedVolatility(minIV, maxIV);

            // Assert
            Assert.That(universe.AllSymbols.Count(), Is.EqualTo(expectedContracts));
            Assert.That(universe.AllSymbols, Has.All.Matches<Symbol>(contract =>
            {
                var data = GetContractData(contract);
                return data.ImpliedVolatility >= minIV && data.ImpliedVolatility <= maxIV;
            }));
        }

        [Test]
        public void FiltersContractsByOpenInterest()
        {
            var minOpenInterest = 500;
            var maxOpenInterest = 1000;
            var expectedContracts = 2;

            // Set up
            var universe = new OptionFilterUniverse(GetOption(), _testOptionsData, _underlying);
            universe.Refresh(_testOptionsData, _underlying, _underlying.EndTime);

            // Filter
            universe.OpenInterest(minOpenInterest, maxOpenInterest);

            // Assert
            Assert.That(universe.AllSymbols.Count(), Is.EqualTo(expectedContracts));
            Assert.That(universe.AllSymbols, Has.All.Matches<Symbol>(contract =>
            {
                var data = GetContractData(contract);
                return data.OpenInterest >= minOpenInterest && data.OpenInterest <= maxOpenInterest;
            }));
        }

        [TestCase("Delta", 0.63, 0.64, 4)]
        [TestCase("Gamma", 0.0008, 0.0011, 4)]
        [TestCase("Vega", 7.5, 11.3, 5)]
        [TestCase("Theta", -1.10, -0.50, 8)]
        [TestCase("Rho", 4, 10, 10)]
        public void FiltersContractsByIndividualGreek(string greekName, decimal greekMinValue, decimal greekMaxValue, int expectedContracts)
        {
            // Set up
            var universe = new OptionFilterUniverse(GetOption(), _testOptionsData, _underlying);
            universe.Refresh(_testOptionsData, _underlying, _underlying.EndTime);

            // Filter
            var greekFilterMethod = universe.GetType().GetMethod(greekName);
            greekFilterMethod.Invoke(universe, new object[] { greekMinValue, greekMaxValue });

            // Assert
            Assert.That(universe.AllSymbols.Count(), Is.EqualTo(expectedContracts));
            Assert.That(universe.AllSymbols, Has.All.Matches<Symbol>(contract =>
            {
                var greeks = GetGreeks(contract);
                var greek = (decimal)greeks.GetType().GetProperty(greekName).GetValue(greeks);
                return greek >= greekMinValue && greek <= greekMaxValue;
            }));
        }

        [Test]
        public void FiltersContractsByMultipleGreeks()
        {
            var deltaMin = 0.62m;
            var deltaMax = 0.68m;
            var gammaMin = 0.00024m;
            var gammaMax = 0.0028m;
            var thetaMin = -1.40m;
            var thetaMax = -0.40m;
            var expectedContracts = 11;

            // Set up
            var universe = new OptionFilterUniverse(GetOption(), _testOptionsData, _underlying);
            universe.Refresh(_testOptionsData, _underlying, _underlying.EndTime);

            // Filter
            universe.Delta(deltaMin, deltaMax).Gamma(gammaMin, gammaMax).Theta(thetaMin, thetaMax);

            // Assert
            Assert.That(universe.AllSymbols.Count(), Is.EqualTo(expectedContracts));
            Assert.That(universe.AllSymbols, Has.All.Matches<Symbol>(contract =>
            {
                var greeks = GetGreeks(contract);
                return greeks.Delta >= deltaMin && greeks.Delta <= deltaMax &&
                       greeks.Gamma >= gammaMin && greeks.Gamma <= gammaMax &&
                       greeks.Theta >= thetaMin && greeks.Theta <= thetaMax;
            }));
        }

        [Test]
        public void OptionUnivereDataFiltersAreNotSupportedForFutureOptions()
        {
            // Set up
            var symbol = Symbols.CreateFutureOptionSymbol(Symbols.ES_Future_Chain, OptionRight.Call,
                1000m, new DateTime(2024, 12, 27));
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                    TimeZones.NewYork, TimeZones.NewYork, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var universe = new OptionFilterUniverse(option, _testOptionsData, _underlying);

            // Filter and assert
            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidOperationException>(() => universe.ImpliedVolatility(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.IV(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.OpenInterest(0, 1));
                Assert.Throws<InvalidOperationException>(() => universe.OI(0, 1));
                Assert.Throws<InvalidOperationException>(() => universe.Delta(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.D(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.Gamma(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.G(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.Vega(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.V(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.Theta(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.T(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.Rho(0m, 1m));
                Assert.Throws<InvalidOperationException>(() => universe.R(0m, 1m));
            });
        }

        private static Option GetOption(Symbol symbol = null)
        {
            symbol ??= Symbols.SPY_C_192_Feb19_2016;
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                    TimeZones.NewYork, TimeZones.NewYork, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            return option;
        }

        private OptionUniverse GetContractData(Symbol contract)
        {
            return _testOptionsData.Single(x => x.Symbol == contract);
        }

        private Greeks GetGreeks(Symbol contract)
        {
            return GetContractData(contract).Greeks;
        }
    }
}
