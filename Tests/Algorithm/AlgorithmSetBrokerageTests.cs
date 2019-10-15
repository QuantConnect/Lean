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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Brokerages;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    /// <summary>
    /// Test class for
    ///  - SetBrokerageModel() in QCAlgorithm
    ///  - Default market for new securities
    /// </summary>
    [TestFixture]
    public class AlgorithmSetBrokerageTests
    {
        private QCAlgorithm _algo;
        private const string ForexSym = "EURUSD";
        private const string Sym = "SPY";

        /// <summary>
        /// Instatiate a new algorithm before each test.
        /// Clear the <see cref="SymbolCache"/> so that no symbols and associated brokerage models are cached between test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _algo = new QCAlgorithm();
            _algo.SubscriptionManager.SetDataManager(new DataManagerStub(_algo));
            SymbolCache.TryRemove(ForexSym);
            SymbolCache.TryRemove(Sym);
        }

        /// <summary>
        /// The default market for FOREX should be FXCM
        /// </summary>
        [Test]
        public void DefaultBrokerageModel_IsFXCM_ForForex()
        {
            var forex = _algo.AddForex(ForexSym);


            Assert.IsTrue(forex.Symbol.ID.Market == Market.FXCM);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(DefaultBrokerageModel));
        }

        [Test]
        public void PythonCallPureCSharpSetBrokerageModel()
        {
            using (Py.GIL())
            {
                var model = new AlphaStreamsBrokerageModel().ToPython();
                _algo.SetBrokerageModel(model);
                Assert.DoesNotThrow(() => _algo.BrokerageModel.ApplySplit(new List<OrderTicket>(), new Split()));
            }
        }

        [Test]
        public void PythonCallSetBrokerageModel()
        {
            using (Py.GIL())
            {
                var model = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""QuantConnect.Common"")
from QuantConnect.Brokerages import *

class Test(AlphaStreamsBrokerageModel):
    def GetLeverage(self, security):
        return 12").GetAttr("Test");
                _algo.SetBrokerageModel(model.Invoke());

                var equity = _algo.AddEquity(Sym);
                Assert.DoesNotThrow(() => _algo.BrokerageModel.ApplySplit(new List<OrderTicket>(), new Split()));
                Assert.AreEqual(12m, _algo.BrokerageModel.GetLeverage(equity));
            }
        }

        /// <summary>
        /// The default market for equities should be USA
        /// </summary>
        [Test]
        public void DefaultBrokerageModel_IsUSA_ForEquity()
        {
            var equity = _algo.AddEquity(Sym);


            Assert.IsTrue(equity.Symbol.ID.Market == Market.USA);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(DefaultBrokerageModel));
        }

        /// <summary>
        /// The default market for options should be USA
        /// </summary>
        [Test]
        public void DefaultBrokerageModel_IsUSA_ForOption()
        {
            var option = _algo.AddOption(Sym);


            Assert.IsTrue(option.Symbol.ID.Market == Market.USA);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(DefaultBrokerageModel));
        }

        /// <summary>
        /// Brokerage model for an algorithm can be changed using <see cref="QCAlgorithm.SetBrokerageModel(IBrokerageModel)"/>
        /// This changes the brokerage models used when forex currency pairs are added via AddForex and no brokerage is specified.
        /// </summary>
        [Test]
        public void BrokerageModel_CanBeSpecifiedWith_SetBrokerageModel()
        {
            _algo.SetBrokerageModel(BrokerageName.OandaBrokerage);
            var forex = _algo.AddForex(ForexSym);

            string brokerage = GetDefaultBrokerageForSecurityType(SecurityType.Forex);


            Assert.IsTrue(forex.Symbol.ID.Market == Market.Oanda);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(OandaBrokerageModel));
            Assert.IsTrue(brokerage == Market.Oanda);
        }

        /// <summary>
        /// Specifying the market in <see cref="QCAlgorithm.AddForex"/> will change the market of the security created.
        /// </summary>
        [Test]
        public void BrokerageModel_CanBeSpecifiedWith_AddForex()
        {
            var forex = _algo.AddForex(ForexSym, Resolution.Minute, Market.Oanda);

            string brokerage = GetDefaultBrokerageForSecurityType(SecurityType.Forex);


            Assert.IsTrue(forex.Symbol.ID.Market == Market.Oanda);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(DefaultBrokerageModel));
            Assert.IsTrue(brokerage == Market.FXCM);  // Doesn't change brokerage defined in BrokerageModel.DefaultMarkets
        }

        /// <summary>
        /// The method <see cref="QCAlgorithm.AddSecurity(SecurityType, string, Resolution, bool, bool)"/> should use the default brokerage for the sepcific security.
        /// Setting the brokerage with <see cref="QCAlgorithm.SetBrokerageModel(IBrokerageModel)"/> will affect the market of securities added with  <see cref="QCAlgorithm.AddSecurity(SecurityType, string, Resolution, bool, bool)"/>
        /// </summary>
        [Test]
        public void AddSecurity_Follows_SetBrokerageModel()
        {
            // No brokerage set
            var equity = _algo.AddSecurity(SecurityType.Equity, Sym);

            string equityBrokerage = GetDefaultBrokerageForSecurityType(SecurityType.Equity);


            Assert.IsTrue(equity.Symbol.ID.Market == Market.USA);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(DefaultBrokerageModel));
            Assert.IsTrue(equityBrokerage == Market.USA);

            // Set Brokerage
            _algo.SetBrokerageModel(BrokerageName.OandaBrokerage);

            var sec = _algo.AddSecurity(SecurityType.Forex, ForexSym, Resolution.Daily, false, 1, false);

            string forexBrokerage = GetDefaultBrokerageForSecurityType(SecurityType.Forex);


            Assert.IsTrue(sec.Symbol.ID.Market == Market.Oanda);
            Assert.IsTrue(_algo.BrokerageModel.GetType() == typeof(OandaBrokerageModel));
            Assert.IsTrue(forexBrokerage ==  Market.Oanda);
        }

        [Test]
        public void AddSecurityCanAddWithSameTickerAndDifferentMarket()
        {
            var fxcmSecurity = _algo.AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute, Market.FXCM, true, 1m, true);
            var oandaSecurity = _algo.AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute, Market.Oanda, true, 1m, true);

            Assert.AreEqual(2, _algo.Securities.Count);
            Assert.AreEqual(Market.FXCM, _algo.Securities.First().Key.ID.Market);
            Assert.AreEqual(Market.Oanda, _algo.Securities.Last().Key.ID.Market);
            Assert.AreEqual(Market.FXCM, fxcmSecurity.Symbol.ID.Market);
            Assert.AreEqual(Market.Oanda, oandaSecurity.Symbol.ID.Market);
        }

        [Test]
        public void AddForexCanAddWithSameTickerAndDifferentMarket()
        {
            var fxcmSecurity = _algo.AddForex("EURUSD", Resolution.Minute, Market.FXCM);
            var oandaSecurity = _algo.AddForex("EURUSD", Resolution.Minute, Market.Oanda);

            Assert.AreEqual(2, _algo.Securities.Count);
            Assert.AreEqual(Market.FXCM, _algo.Securities.First().Key.ID.Market);
            Assert.AreEqual(Market.Oanda, _algo.Securities.Last().Key.ID.Market);
            Assert.AreEqual(Market.FXCM, fxcmSecurity.Symbol.ID.Market);
            Assert.AreEqual(Market.Oanda, oandaSecurity.Symbol.ID.Market);
        }

        /// <summary>
        /// Returns the default market for a security type
        /// </summary>
        /// <param name="secType">The type of security</param>
        /// <returns>A string representing the default market of a security</returns>
        private string GetDefaultBrokerageForSecurityType(SecurityType secType)
        {
            string brokerage;
            _algo.BrokerageModel.DefaultMarkets.TryGetValue(secType, out brokerage);
            return brokerage;
        }
    }
}
