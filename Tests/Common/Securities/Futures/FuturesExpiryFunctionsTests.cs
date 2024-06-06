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
using NUnit.Framework;
using QuantConnect.Securities.Future;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using QuantConnect.Brokerages;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class FuturesExpiryFunctionsTests
    {
        private IDictionary<String, List<Dates>> _data = new Dictionary<String, List<Dates>>();
        private const string Zero = "00:00:00";
        private const string ElevenAmHongKongTime = "03:00:00";
        private const string ElevenOclockMoscowTime = "08:00:00";
        private const string TenSixteen = "10:16:00";
        private const string ElevenOclock = "11:00:00";
        private const string NineFifteenCentralTime = "14:15:00";
        private const string NineSixteenCentralTime = "14:16:00";
        private const string TwelvePMCentralTime = "17:00:00";
        private const string TwelveFivePMCentralTime = "17:05:00";
        private const string TwelveTenCentralTime = "17:10:00";
        private const string OneThirtyPMCentralTime = "18:30:00";
        private const string OneFortyPMCentralTime = "18:40:00";
        private const string TwoPMCentralTime = "19:00:00";
        private const string ThreePMCentralTime = "20:00:00";
        private const string NineThirtyEasternTime = "13:30:00";
        private const string FiveOClockPMEasternTime = "21:00:00";
        private const string EightOClockChicagoTime = "13:00:00";
        private const string TwelveOclock = "12:00:00";
        private const string TwelveOne = "12:01:00";
        private const string FourPMLondonTime = "15:00:00";
        private const string OneTwentyFivePM = "13:25:00";
        private const string OneThirtyPM = "13:30:00";
        private const string TwoThirtyPM = "14:30:00";
        private const string OneFortyFivePM = "13:45:00";
        private const string ThreeThirtyPM = "15:30:00";
        private const string FourPM = "16:00:00";
        private const string FourFifteenPM = "16:15:00";
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        [OneTimeSetUp]
        public void Init()
        {
            var path = Path.Combine("TestData", "FuturesExpiryFunctionsTestData.xml");
            using (var reader = XmlReader.Create(path))
            {
                var serializer = new XmlSerializer(typeof(Item[]));
                _data = ((Item[])serializer.Deserialize(reader)).ToDictionary(i=>i.Symbol,i=>i.SymbolDates);
            }
        }

        [Test]
        public void FuturesExpiryFunction_MissingSymbol_ShouldThrowArgumentException()
        {
            const string badSymbol = "AAAAA";
            Assert.Throws<ArgumentException>(() => { FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(badSymbol)); },
                                             $"Expiry function not implemented for {badSymbol} in FuturesExpiryFunctions.FuturesExpiryDictionary");
        }

        [Test]
        public void FuturesExpiryFunctions_AllFutures_ShouldHaveExpiryFunction()
        {
            var missingFutures = new List<string>();

            var futuresSymbols = typeof(QuantConnect.Securities.Futures).GetNestedTypes()
                                                                        .SelectMany(x => x.GetFields())
                                                                        .Select(x => x.GetValue(null)) // null for obj in GetValue indicates static field
                                                                        .Cast<string>();

            foreach (var futuresSymbol in futuresSymbols)
            {
                try
                {
                    FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(futuresSymbol));
                }
                catch (ArgumentException)
                {
                    missingFutures.Add(futuresSymbol);
                }
            }

            Assert.IsEmpty(missingFutures,
                           $"The following symbols do not have an expiry function defined in FuturesExpiryFunction.FuturesExpiryDictionary: {string.Join(", ", missingFutures)}");
        }

        [TestCase(QuantConnect.Securities.Futures.Grains.BlackSeaCornFinanciallySettledPlatts)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SRWWheat)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Wheat)]
        [TestCase(QuantConnect.Securities.Futures.Grains.HRWWheat)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Corn)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Soybeans)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanMeal)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanOil)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Oats)]
        [TestCase(QuantConnect.Securities.Futures.Grains.BlackSeaWheatFinanciallySettledPlatts)]
        public void GrainsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade;

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Currencies.USD, TenSixteen)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.GBP, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CAD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.JPY, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CHF, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EUR, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.NZD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.RUB, ElevenOclockMoscowTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.BRL, NineFifteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.MXN, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.ZAR, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUDCAD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUDJPY, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUDNZD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.BTC, FourPMLondonTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.ETH, FourPMLondonTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CADJPY, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.StandardSizeUSDOffshoreRMBCNH, ElevenAmHongKongTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EuroFXEmini, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EURAUD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EURCAD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EURSEK, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.JapaneseYenEmini, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.MicroEUR, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.MicroBTC, FourPMLondonTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.MicroEther, FourPMLondonTime)]
        public void CurrenciesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Energy.PropaneNonLDHMontBelvieu, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ArgusPropaneFarEastIndexBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MiniEuropeanThreePointPercentFiveFuelOilBargesPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MiniSingaporeFuelOil180CstPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GulfCoastULSDPlattsUpDownBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GulfCoastJetPlattsUpDownBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.PropaneNonLDHMontBelvieuOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EuropeanPropaneCIFARAArgusBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.PremiumUnleadedGasoline10ppmFOBMEDPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ArgusPropaneFarEastIndex, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GasolineEurobobOxyNWEBargesArgusCrackSpreadBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuNaturalGasolineOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuNormalButaneOPISBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ConwayPropaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuLDHPropaneOPISBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ArgusPropaneFarEastIndexVsEuropeanPropaneCIFARAArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ArgusPropaneSaudiAramco, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GroupThreeULSDPlattsVsNYHarborULSD, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GroupThreeSuboctaneGasolinePlattsVsRBOB, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.SingaporeFuelOil180cstPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.SingaporeFuelOil380cstPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuEthaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuNormalButaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.BrentCrudeOilVsDubaiCrudeOilPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ArgusLLSvsWTIArgusTradeMonth, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.SingaporeGasoilPlattsVsLowSulphurGasoilFutures, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.LosAngelesCARBOBGasolineOPISvsRBOBGasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.LosAngelesJetOPISvsNYHarborULSD, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.LosAngelesCARBDieselOPISvsNYHarborULSD, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EuropeanNaphthaPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EuropeanPropaneCIFARAArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuNaturalGasolineOPISBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.RBOBGasolineCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GulfCoastHSFOPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MarsArgusVsWTITradeMonth, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MarsArgusVsWTIFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EthanolT2FOBRdamIncludingDutyPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MontBelvieuLDHPropaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GasolineEurobobOxyNWEBargesArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.WTIBrentFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread1000mt, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GasolineEurobobOxyNWEBargesArgusBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.BrentLastDayFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.CrudeOilWTI, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GulfCoastCBOBGasolineA2PlattsVsRBOBGasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ClearbrookBakkenSweetCrudeOilMonthlyIndexNetEnergy, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.WTIFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ChicagoEthanolPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.SingaporeMogas92UnleadedPlattsBrentCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.DubaiCrudeOilPlattsFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.JapanCnFNaphthaPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.Ethanol, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EuropeanNaphthaPlattsCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EuropeanPropaneCIFARAArgusVsNaphthaCargoesCIFNWEPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.SingaporeFuelOil380cstPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EastWestGasolineSpreadPlattsArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.EastWestNaphthaJapanCFvsCargoesCIFNWESpreadPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.RBOBGasolineVsEurobobOxyNWEBargesArgusThreeHundredFiftyThousandGallons, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.FreightRouteTC14Baltic, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.OnePercentFuelOilCargoesFOBNWEPlattsVsThreePointFivePercentFuelOilBargesFOBRdamPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.GulfCoastHSFOPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.WTIHoustonCrudeOil, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.NaturalGasHenryHubLastDayFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.HeatingOil, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.NaturalGasHenryHubPenultimateFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.WTIHoustonArgusVsWTITradeMonth, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.Gasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.NaturalGas, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.BrentCrude, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energy.LowSulfurGasoil, TwelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Energy.MicroCrudeOilWTI, Zero)]
        public void EnergyExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        // 25th is a sunday
        [TestCase(QuantConnect.Securities.Futures.Energy.MicroCrudeOilWTI, "20221001", "20220919")]
        [TestCase(QuantConnect.Securities.Futures.Energy.CrudeOilWTI, "20221001", "20220920")]
        // 25th is a tuesday
        [TestCase(QuantConnect.Securities.Futures.Energy.MicroCrudeOilWTI, "20221101", "20221019")]
        [TestCase(QuantConnect.Securities.Futures.Energy.CrudeOilWTI, "20221101", "20221020")]
        // 25th is a friday but includes thanks giving
        [TestCase(QuantConnect.Securities.Futures.Energy.MicroCrudeOilWTI, "20221201", "20221118")]
        [TestCase(QuantConnect.Securities.Futures.Energy.CrudeOilWTI, "20221201", "20221121")]
        public void MicroCrudeOilExpiration(string symbol, string dateStr, string expectedDate)
        {
            var date = Time.ParseDate(dateStr);
            var expected = Time.ParseDate(expectedDate);

            var futureSymbol = GetFutureSymbol(symbol, date);
            var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

            var actual = func(futureSymbol.ID.Date);

            Assert.AreEqual(expected, actual, $"Failed for symbol: {symbol}. Date {dateStr}");
        }

        [TestCase(QuantConnect.Securities.Futures.Financials.EuroDollar, ElevenOclock)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y30TreasuryBond, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y10TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y5TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y2TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.FiveYearUSDMACSwap, TwoPMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Financials.UltraUSTreasuryBond, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.UltraTenYearUSTreasuryNote, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Financials.MicroY10TreasuryNote, Zero)]
        public void FinancialsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Indices.BloombergCommodityIndex, OneThirtyPMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.NASDAQ100BiotechnologyEMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.FTSEEmergingEmini, ThreePMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.SP400MidCapEmini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.SPGSCICommodity, OneFortyPMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.USDDenominatedIbovespa, ThreePMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.SP500EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.NASDAQ100EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Dow30EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Russell2000EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Nikkei225Dollar, FiveOClockPMEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.VIX, EightOClockChicagoTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Nikkei225Yen, TwoThirtyPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCITaiwanIndex, OneFortyFivePM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Nifty50, ThreeThirtyPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.BankNifty, ThreeThirtyPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.BseSensex, ThreeThirtyPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MicroSP500EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MicroDow30EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCIEuropeNTR, FourFifteenPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCIJapanNTR, FourFifteenPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCIUsaIndex, FourFifteenPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCIEmergingMarketsAsiaNTR, FourFifteenPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCIEmergingMarketsIndex, FourFifteenPM)]
        [TestCase(QuantConnect.Securities.Futures.Indices.MSCIEafeIndex, FourFifteenPM)]
        public void IndicesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Meats.LiveCattle, TwelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.LeanHogs, TwelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.FeederCattle, Zero)]
        public void MeatsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Forestry.Lumber, TwelveFivePMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Forestry.RandomLengthLumber, TwelveFivePMCentralTime)]
        public void LumberPulpExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Metals.Gold, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Silver, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Platinum, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Palladium, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.AluminumMWUSTransactionPremiumPlatts25MT, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.AluminiumEuropeanPremiumDutyPaidMetalBulletin, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Copper, TwelvePMCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Metals.USMidwestDomesticHotRolledCoilSteelCRUIndex, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.MicroGold, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.MiniNYGold, OneThirtyPM)]
        [TestCase(QuantConnect.Securities.Futures.Metals.MiniNYSilver, OneTwentyFivePM)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Gold100Oz, OneThirtyPM)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Silver5000Oz, OneTwentyFivePM)]
        public void MetalsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Softs.Cotton2)]
        [TestCase(QuantConnect.Securities.Futures.Softs.OrangeJuice)]
        [TestCase(QuantConnect.Securities.Futures.Softs.Coffee)]
        [TestCase(QuantConnect.Securities.Futures.Softs.Sugar11)]
        [TestCase(QuantConnect.Securities.Futures.Softs.Sugar11CME)]
        [TestCase(QuantConnect.Securities.Futures.Softs.Cocoa)]
        public void SoftsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                //Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade;

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Dairy.CashSettledButter, TwelveTenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Dairy.CashSettledCheese, TwelveTenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Dairy.ClassIIIMilk, TwelveTenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Dairy.DryWhey, TwelveTenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Dairy.ClassIVMilk, TwelveTenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Dairy.NonfatDryMilk, TwelveTenCentralTime)]
        public void DairyExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                // Arrange
                var futureSymbol = GetFutureSymbol(symbol, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(GetFutureSymbol(symbol));

                // Act
                var actual = func(futureSymbol.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                // Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        /// <summary>
        /// Dates for Termination Conditions of futures
        /// </summary>
        public class Dates
        {
            public DateTime ContractMonth;
            public DateTime LastTrade;
            public Dates() { }
            public Dates(DateTime c, DateTime l)
            {
                ContractMonth = c;
                LastTrade = l;
            }
        }

        /// <summary>
        /// Class to convert Array into Dictionary using XmlSerializer
        /// </summary>
        public class Item
        {
            [XmlAttribute]
            public String Symbol;
            public List<Dates> SymbolDates;
        }

        private Symbol GetFutureSymbol(string symbol, DateTime? date =null)
        {
            string market;
            if (!_symbolPropertiesDatabase.TryGetMarket(symbol, SecurityType.Future, out market))
            {
                market = DefaultBrokerageModel.DefaultMarketMap[SecurityType.Future];
            }

            if (date.HasValue)
            {
                return Symbol.CreateFuture(symbol, market, date.Value);
            }
            return Symbol.Create(symbol, SecurityType.Future, market);
        }
    }
}
