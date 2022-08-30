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

using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Python;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class BrokerageModelTests
    {
        [TestCaseSource(nameof(GetBrokerageNameTestCases))]
        public void GetsCorrectBrokerageNameFromBrokerageInstance(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            Assert.AreEqual(brokerageName, BrokerageModel.GetBrokerageName(brokerage));
        }

        [TestCaseSource(nameof(GetCustomBrokerageNameTestCases))]
        public void GetsCorrectCustomBrokerageNameFromBrokerageInstance_CSharp(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            Assert.AreEqual(brokerageName, BrokerageModel.GetBrokerageName(brokerage));
        }

        [TestCaseSource(nameof(GetBrokerageNameTestCases))]
        public void GetsCorrectCustomBrokerageNameFromBrokerageInstance_Python(IBrokerageModel brokerage, BrokerageName brokerageName)
        {
            using (Py.GIL())
            {
                dynamic PyCustomBrokerageModel = PyModule.FromString("testModule",
                    @$"
from AlgorithmImports import *

class CustomBrokerageModel({brokerage.GetType().Name}):
    pass
                ").GetAttr("CustomBrokerageModel");

                Assert.AreEqual(brokerageName, BrokerageModel.GetBrokerageName(new BrokerageModelPythonWrapper(PyCustomBrokerageModel())));
            }
        }

        private static TestCaseData[] GetBrokerageNameTestCases()
        {
            return new[]
            {
                new TestCaseData(new InteractiveBrokersBrokerageModel(), BrokerageName.InteractiveBrokersBrokerage),
                new TestCaseData(new TradierBrokerageModel(), BrokerageName.TradierBrokerage),
                new TestCaseData(new OandaBrokerageModel(), BrokerageName.OandaBrokerage),
                new TestCaseData(new FxcmBrokerageModel(), BrokerageName.FxcmBrokerage),
                new TestCaseData(new BitfinexBrokerageModel(), BrokerageName.Bitfinex),
                new TestCaseData(new BinanceUSBrokerageModel(), BrokerageName.BinanceUS),
                new TestCaseData(new BinanceBrokerageModel(), BrokerageName.Binance),
                new TestCaseData(new GDAXBrokerageModel(), BrokerageName.GDAX),
                new TestCaseData(new AlphaStreamsBrokerageModel(), BrokerageName.AlphaStreams),
                new TestCaseData(new ZerodhaBrokerageModel(), BrokerageName.Zerodha),
                new TestCaseData(new AtreyuBrokerageModel(), BrokerageName.Atreyu),
                new TestCaseData(new TradingTechnologiesBrokerageModel(), BrokerageName.TradingTechnologies),
                new TestCaseData(new SamcoBrokerageModel(), BrokerageName.Samco),
                new TestCaseData(new KrakenBrokerageModel(), BrokerageName.Kraken),
                new TestCaseData(new ExanteBrokerageModel(), BrokerageName.Exante),
                new TestCaseData(new FTXUSBrokerageModel(), BrokerageName.FTXUS),
                new TestCaseData(new FTXBrokerageModel(), BrokerageName.FTX),
                new TestCaseData(new DefaultBrokerageModel(), BrokerageName.Default)
            };
        }

        private class CustomInteractiveBrokersBrokerageModel : InteractiveBrokersBrokerageModel {}
        private class CustomTradierBrokerageModel : TradierBrokerageModel {}
        private class CustomOandaBrokerageModel : OandaBrokerageModel {}
        private class CustomFxcmBrokerageModel : FxcmBrokerageModel {}
        private class CustomBitfinexBrokerageModel : BitfinexBrokerageModel {}
        private class CustomBinanceUSBrokerageModel : BinanceUSBrokerageModel {}
        private class CustomBinanceBrokerageModel : BinanceBrokerageModel {}
        private class CustomGDAXBrokerageModel : GDAXBrokerageModel {}
        private class CustomAlphaStreamsBrokerageModel : AlphaStreamsBrokerageModel {}
        private class CustomZerodhaBrokerageModel : ZerodhaBrokerageModel {}
        private class CustomAtreyuBrokerageModel : AtreyuBrokerageModel {}
        private class CustomTradingTechnologiesBrokerageModel : TradingTechnologiesBrokerageModel {}
        private class CustomSamcoBrokerageModel : SamcoBrokerageModel {}
        private class CustomKrakenBrokerageModel : KrakenBrokerageModel {}
        private class CustomExanteBrokerageModel : ExanteBrokerageModel {}
        private class CustomFTXUSBrokerageModel : FTXUSBrokerageModel {}
        private class CustomFTXBrokerageModel : FTXBrokerageModel {}
        private class CustomDefaultBrokerageModel : DefaultBrokerageModel {}

        private static TestCaseData[] GetCustomBrokerageNameTestCases()
        {
            return new[]
            {
                new TestCaseData(new CustomInteractiveBrokersBrokerageModel(), BrokerageName.InteractiveBrokersBrokerage),
                new TestCaseData(new CustomTradierBrokerageModel(), BrokerageName.TradierBrokerage),
                new TestCaseData(new CustomOandaBrokerageModel(), BrokerageName.OandaBrokerage),
                new TestCaseData(new CustomFxcmBrokerageModel(), BrokerageName.FxcmBrokerage),
                new TestCaseData(new CustomBitfinexBrokerageModel(), BrokerageName.Bitfinex),
                new TestCaseData(new CustomBinanceUSBrokerageModel(), BrokerageName.BinanceUS),
                new TestCaseData(new CustomBinanceBrokerageModel(), BrokerageName.Binance),
                new TestCaseData(new CustomGDAXBrokerageModel(), BrokerageName.GDAX),
                new TestCaseData(new CustomAlphaStreamsBrokerageModel(), BrokerageName.AlphaStreams),
                new TestCaseData(new CustomZerodhaBrokerageModel(), BrokerageName.Zerodha),
                new TestCaseData(new CustomAtreyuBrokerageModel(), BrokerageName.Atreyu),
                new TestCaseData(new CustomTradingTechnologiesBrokerageModel(), BrokerageName.TradingTechnologies),
                new TestCaseData(new CustomSamcoBrokerageModel(), BrokerageName.Samco),
                new TestCaseData(new CustomKrakenBrokerageModel(), BrokerageName.Kraken),
                new TestCaseData(new CustomExanteBrokerageModel(), BrokerageName.Exante),
                new TestCaseData(new CustomFTXUSBrokerageModel(), BrokerageName.FTXUS),
                new TestCaseData(new CustomFTXBrokerageModel(), BrokerageName.FTX),
                new TestCaseData(new CustomDefaultBrokerageModel(), BrokerageName.Default)
            };
        }
    }
}
