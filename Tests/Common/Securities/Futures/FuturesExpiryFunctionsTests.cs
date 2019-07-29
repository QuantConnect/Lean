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

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture]
    public class FuturesExpiryFunctionsTests
    {
        private IDictionary<String, List<Dates>> _data = new Dictionary<String, List<Dates>>();
        private const string Zero = "00:00:00";
        private const string ElevenAmHongKongTime = "03:00:00";
        private const string ElevenOclockMoscowTime = "08:00:00";
        private const string NineFifteenCentralTime = "14:15:00";
        private const string NineSixteenCentralTime = "14:16:00";
        private const string TwelvePMCentralTime = "17:00:00";
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
        private const string FourPmLondonTime = "15:00:00";
        [TestFixtureSetUp]
        public void Init()
        {
            var path = Path.Combine("TestData", "FuturesExpiryFunctionsTestData.xml");
            using (var reader = XmlReader.Create(path))
            {
                var serializer = new XmlSerializer(typeof(Item[]));
                _data = ((Item[])serializer.Deserialize(reader)).ToDictionary(i=>i.Symbol,i=>i.SymbolDates);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Grains.BlackSeaCornFinanciallySettledPlatts)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Wheat)]
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
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade;

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

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
        [TestCase(QuantConnect.Securities.Futures.Currencies.BTC, FourPmLondonTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CADJPY, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.StandardSizeUSDOffshoreRMBCNH, ElevenAmHongKongTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EuroFXEmini, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EURAUD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EURCAD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EURSEK, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.JapaneseYenEmini, NineSixteenCentralTime)]
        public void CurrenciesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Energies.PropaneNonLDHMontBelvieu, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ArgusPropaneFarEastIndexBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MiniEuropeanThreePointPercentFiveFuelOilBargesPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MiniSingaporeFuelOil180CstPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GulfCoastULSDPlattsUpDownBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GulfCoastJetPlattsUpDownBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.PropaneNonLDHMontBelvieuOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EuropeanPropaneCIFARAArgusBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.PremiumUnleadedGasoline10ppmFOBMEDPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ArgusPropaneFarEastIndex, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GasolineEurobobOxyNWEBargesArgusCrackSpreadBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuNaturalGasolineOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuNormalButaneOPISBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ConwayPropaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuLDHPropaneOPISBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ArgusPropaneFarEastIndexVsEuropeanPropaneCIFARAArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ArgusPropaneSaudiAramco, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GroupThreeULSDPlattsVsNYHarborULSD, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GroupThreeSuboctaneGasolinePlattsVsRBOB, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.SingaporeFuelOil180cstPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.SingaporeFuelOil380cstPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuEthaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuNormalButaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.BrentCrudeOilVsDubaiCrudeOilPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ArgusLLSvsWTIArgusTradeMonth, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.SingaporeGasoilPlattsVsLowSulphurGasoilFutures, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.LosAngelesCARBOBGasolineOPISvsRBOBGasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.LosAngelesJetOPISvsNYHarborULSD, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.LosAngelesCARBDieselOPISvsNYHarborULSD, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EuropeanNaphthaPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EuropeanPropaneCIFARAArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuNaturalGasolineOPISBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.RBOBGasolineCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GulfCoastHSFOPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MarsArgusVsWTITradeMonth, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MarsArgusVsWTIFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EthanolT2FOBRdamIncludingDutyPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.MontBelvieuLDHPropaneOPIS, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GasolineEurobobOxyNWEBargesArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.WTIBrentFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread1000mt, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GasolineEurobobOxyNWEBargesArgusBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.BrentLastDayFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.CrudeOilWTI, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GulfCoastCBOBGasolineA2PlattsVsRBOBGasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ClearbrookBakkenSweetCrudeOilMonthlyIndexNetEnergy, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.WTIFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ChicagoEthanolPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.SingaporeMogas92UnleadedPlattsBrentCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.DubaiCrudeOilPlattsFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.JapanCnFNaphthaPlattsBALMO, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.Ethanol, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EuropeanNaphthaPlattsCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EuropeanPropaneCIFARAArgusVsNaphthaCargoesCIFNWEPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.SingaporeFuelOil380cstPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EastWestGasolineSpreadPlattsArgus, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.EastWestNaphthaJapanCFvsCargoesCIFNWESpreadPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.RBOBGasolineVsEurobobOxyNWEBargesArgusThreeHundredFiftyThousandGallons, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.FreightRouteTC14Baltic, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.OnePercentFuelOilCargoesFOBNWEPlattsVsThreePointFivePercentFuelOilBargesFOBRdamPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.GulfCoastHSFOPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.WTIHoustonCrudeOil, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.NaturalGasHenryHubLastDayFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.HeatingOil, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.NaturalGasHenryHubPenultimateFinancial, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.WTIHoustonArgusVsWTITradeMonth, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.Gasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.NaturalGas, Zero)]
        public void EnergiesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Financials.Y30TreasuryBond, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y10TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y5TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y2TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.FiveYearUSDMACSwap, TwoPMCentralTime)]
        public void FinancialsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
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
        public void IndicesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
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
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
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
        public void MetalsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + Parse.TimeSpan(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Softs.Sugar11CME)]
        public void SoftsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
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
                var security = Symbol.CreateFuture(symbol, "usa", date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                // Act
                var actual = func(security.ID.Date);
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
    }
}
