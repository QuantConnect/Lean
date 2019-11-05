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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.PsychSignal;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Data.Custom.USTreasury;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;
using Bitcoin = QuantConnect.Algorithm.CSharp.LiveTradingFeaturesAlgorithm.Bitcoin;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmAddDataTests
    {
        [Test]
        public void DefaultDataFeeds_CanBeOverwritten_Successfully()
        {
            Config.Set("security-data-feeds", "{ Forex: [\"Trade\"] }");
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));

            // forex defult - should be tradebar
            var forexTrade = algo.AddForex("EURUSD");
            Assert.IsTrue(forexTrade.Subscriptions.Count() == 1);
            Assert.IsTrue(GetMatchingSubscription(forexTrade, typeof(QuoteBar)) != null);

            // Change
            var dataFeedsConfigString = Config.Get("security-data-feeds");
            Dictionary<SecurityType, List<TickType>> dataFeeds = new Dictionary<SecurityType, List<TickType>>();
            if (dataFeedsConfigString != string.Empty)
            {
                dataFeeds = JsonConvert.DeserializeObject<Dictionary<SecurityType, List<TickType>>>(dataFeedsConfigString);
            }

            algo.SetAvailableDataTypes(dataFeeds);

            // new forex - should be quotebar
            var forexQuote = algo.AddForex("EURUSD");
            Assert.IsTrue(forexQuote.Subscriptions.Count() == 1);
            Assert.IsTrue(GetMatchingSubscription(forexQuote, typeof(TradeBar)) != null);
        }

        [Test]
        public void DefaultDataFeeds_AreAdded_Successfully()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));

            // forex
            var forex = algo.AddSecurity(SecurityType.Forex, "eurusd");
            Assert.IsTrue(forex.Subscriptions.Count() == 1);
            Assert.IsTrue(GetMatchingSubscription(forex, typeof(QuoteBar)) != null);

            // equity
            var equity = algo.AddSecurity(SecurityType.Equity, "goog");
            Assert.IsTrue(equity.Subscriptions.Count() == 1);
            Assert.IsTrue(GetMatchingSubscription(equity, typeof(TradeBar)) != null);

            // option
            var option = algo.AddSecurity(SecurityType.Option, "goog");
            Assert.IsTrue(option.Subscriptions.Count() == 1);
            Assert.IsTrue(GetMatchingSubscription(option, typeof(ZipEntryName)) != null);

            // cfd
            var cfd = algo.AddSecurity(SecurityType.Cfd, "abc");
            Assert.IsTrue(cfd.Subscriptions.Count() == 1);
            Assert.IsTrue(GetMatchingSubscription(cfd, typeof(QuoteBar)) != null);

            // future
            var future = algo.AddSecurity(SecurityType.Future, "ES");
            Assert.IsTrue(future.Subscriptions.Count() == 1);
            Assert.IsTrue(future.Subscriptions.FirstOrDefault(x => typeof(ZipEntryName).IsAssignableFrom(x.Type)) != null);

            // Crypto
            var crypto = algo.AddSecurity(SecurityType.Crypto, "btcusd", Resolution.Daily);
            Assert.IsTrue(crypto.Subscriptions.Count() == 2);
            Assert.IsTrue(GetMatchingSubscription(crypto, typeof(QuoteBar)) != null);
            Assert.IsTrue(GetMatchingSubscription(crypto, typeof(TradeBar)) != null);
        }


        [Test]
        public void CustomDataTypes_AreAddedToSubscriptions_Successfully()
        {
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            // Add a bitcoin subscription
            qcAlgorithm.AddData<Bitcoin>("BTC");
            var bitcoinSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.FirstOrDefault(x => x.Type == typeof(Bitcoin));
            Assert.AreEqual(bitcoinSubscription.Type, typeof(Bitcoin));

            // Add a quandl subscription
            qcAlgorithm.AddData<Quandl>("EURCAD");
            var quandlSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.FirstOrDefault(x => x.Type == typeof(Quandl));
            Assert.AreEqual(quandlSubscription.Type, typeof(Quandl));
        }

        [Test]
        public void OnEndOfTimeStepSeedsUnderlyingSecuritiesThatHaveNoData()
        {
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm, new MockDataFeed()));
            qcAlgorithm.SetLiveMode(true);
            var testHistoryProvider = new TestHistoryProvider();
            qcAlgorithm.HistoryProvider = testHistoryProvider;

            var option = qcAlgorithm.AddSecurity(SecurityType.Option, testHistoryProvider.underlyingSymbol);
            var option2 = qcAlgorithm.AddSecurity(SecurityType.Option, testHistoryProvider.underlyingSymbol2);
            Assert.IsFalse(qcAlgorithm.Securities.ContainsKey(option.Symbol.Underlying));
            Assert.IsFalse(qcAlgorithm.Securities.ContainsKey(option2.Symbol.Underlying));
            qcAlgorithm.OnEndOfTimeStep();
            var data = qcAlgorithm.Securities[testHistoryProvider.underlyingSymbol].GetLastData();
            var data2 = qcAlgorithm.Securities[testHistoryProvider.underlyingSymbol2].GetLastData();
            Assert.IsNotNull(data);
            Assert.IsNotNull(data2);
            Assert.AreEqual(data.Price, 2);
            Assert.AreEqual(data2.Price, 3);
        }

        [Test]
        public void OnEndOfTimeStepDoesNotThrowWhenSeedsSameUnderlyingForTwoSecurities()
        {
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm, new MockDataFeed()));
            qcAlgorithm.SetLiveMode(true);
            var testHistoryProvider = new TestHistoryProvider();
            qcAlgorithm.HistoryProvider = testHistoryProvider;
            var option = qcAlgorithm.AddOption(testHistoryProvider.underlyingSymbol);

            var symbol = Symbol.CreateOption(testHistoryProvider.underlyingSymbol, Market.USA, OptionStyle.American,
                OptionRight.Call, 1, new DateTime(2015, 12, 24));
            var symbol2 = Symbol.CreateOption(testHistoryProvider.underlyingSymbol, Market.USA, OptionStyle.American,
                OptionRight.Put, 1, new DateTime(2015, 12, 24));

            var optionContract = qcAlgorithm.AddOptionContract(symbol);
            var optionContract2 = qcAlgorithm.AddOptionContract(symbol2);

            qcAlgorithm.OnEndOfTimeStep();
            var data = qcAlgorithm.Securities[testHistoryProvider.underlyingSymbol].GetLastData();
            Assert.AreEqual(testHistoryProvider.LastResolutionRequest, Resolution.Minute);
            Assert.IsNotNull(data);
            Assert.AreEqual(data.Price, 2);
        }

        [TestCase("EURUSD", typeof(PsychSignalSentiment), SecurityType.Cfd, false, true)]
        [TestCase("BTCUSD", typeof(PsychSignalSentiment), SecurityType.Crypto, false, true)]
        [TestCase("CL", typeof(PsychSignalSentiment), SecurityType.Future, false, true)]
        [TestCase("EURUSD", typeof(PsychSignalSentiment), SecurityType.Forex, false, true)]
        [TestCase("AAPL", typeof(PsychSignalSentiment), SecurityType.Equity, true, true)]
        [TestCase("EURUSD", typeof(TradingEconomicsCalendar), SecurityType.Cfd, false, false)]
        [TestCase("BTCUSD", typeof(TradingEconomicsCalendar), SecurityType.Crypto, false, false)]
        [TestCase("CL", typeof(TradingEconomicsCalendar), SecurityType.Future, false, false)]
        [TestCase("AAPL", typeof(TradingEconomicsCalendar), SecurityType.Equity, true, false)]
        [TestCase("EURUSD", typeof(TradingEconomicsCalendar), SecurityType.Forex, false, false)]
        public void AddDataSecuritySymbolWithUnderlying(string ticker, Type customDataType, SecurityType securityType, bool securityShouldBeMapped, bool customDataShouldBeMapped)
        {
            SymbolCache.Clear();
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            Security asset;

            switch (securityType)
            {
                case SecurityType.Cfd:
                    asset = qcAlgorithm.AddCfd(ticker, Resolution.Daily);
                    break;
                case SecurityType.Crypto:
                    asset = qcAlgorithm.AddCrypto(ticker, Resolution.Daily);
                    break;
                case SecurityType.Equity:
                    asset = qcAlgorithm.AddEquity(ticker, Resolution.Daily);
                    break;
                case SecurityType.Forex:
                    asset = qcAlgorithm.AddForex(ticker, Resolution.Daily);
                    break;
                case SecurityType.Future:
                    asset = qcAlgorithm.AddFuture(ticker, Resolution.Daily);
                    break;
                default:
                    throw new Exception($"SecurityType {securityType} is not valid for this test");
            }

            // Dummy here is meant to try to corrupt the SymbolCache. Ideally, SymbolCache should return non-custom data types with higher priority
            // in case we want to add two custom data types, but still have them associated with the equity from the cache if we're using it.
            // This covers the case where two idential data subscriptions are created.
            var dummy = qcAlgorithm.AddData(customDataType, asset.Symbol, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First().DataTimeZone);
            var customData = qcAlgorithm.AddData(customDataType, asset.Symbol, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First().DataTimeZone);

            Assert.IsTrue(customData.Symbol.HasUnderlying, $"{customDataType.Name} added as {ticker} Symbol with SecurityType {securityType} does not have underlying");
            Assert.AreEqual(customData.Symbol.Underlying, asset.Symbol, $"Custom data underlying does not match {securityType} Symbol for {ticker}");

            var assetSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First();
            var customDataSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Base).Single();

            var assetShouldBeMapped = assetSubscription.TickerShouldBeMapped();
            var customShouldBeMapped = customDataSubscription.TickerShouldBeMapped();

            Assert.AreEqual(securityShouldBeMapped, assetShouldBeMapped);
            Assert.AreEqual(customDataShouldBeMapped, customShouldBeMapped);

            Assert.AreNotEqual(assetSubscription, customDataSubscription);

            if (assetShouldBeMapped == customShouldBeMapped)
            {
                Assert.AreEqual(assetSubscription.MappedSymbol, customDataSubscription.MappedSymbol);
                Assert.AreEqual(asset.Symbol.Value, customData.Symbol.Value.Split('.').First());
            }
        }

        [TestCase("EURUSD", typeof(PsychSignalSentiment), SecurityType.Cfd, false, false)]
        [TestCase("BTCUSD", typeof(PsychSignalSentiment), SecurityType.Crypto, false, false)]
        [TestCase("CL", typeof(PsychSignalSentiment), SecurityType.Future, false, false)]
        [TestCase("EURUSD", typeof(PsychSignalSentiment), SecurityType.Forex, false, false)]
        [TestCase("AAPL", typeof(PsychSignalSentiment), SecurityType.Equity, true, true)]
        public void AddDataSecurityTickerWithUnderlying(string ticker, Type customDataType, SecurityType securityType, bool securityShouldBeMapped, bool customDataShouldBeMapped)
        {
            SymbolCache.Clear();
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            Security asset;

            switch (securityType)
            {
                case SecurityType.Cfd:
                    asset = qcAlgorithm.AddCfd(ticker, Resolution.Daily);
                    break;
                case SecurityType.Crypto:
                    asset = qcAlgorithm.AddCrypto(ticker, Resolution.Daily);
                    break;
                case SecurityType.Equity:
                    asset = qcAlgorithm.AddEquity(ticker, Resolution.Daily);
                    break;
                case SecurityType.Forex:
                    asset = qcAlgorithm.AddForex(ticker, Resolution.Daily);
                    break;
                case SecurityType.Future:
                    asset = qcAlgorithm.AddFuture(ticker, Resolution.Daily);
                    break;
                default:
                    throw new Exception($"SecurityType {securityType} is not valid for this test");
            }

            // Aliased value for Futures contains a forward-slash, which causes the
            // lookup in the SymbolCache to fail
            if (securityType == SecurityType.Future)
            {
                ticker = asset.Symbol.Value;
            }

            // Dummy here is meant to try to corrupt the SymbolCache. Ideally, SymbolCache should return non-custom data types with higher priority
            // in case we want to add two custom data types, but still have them associated with the equity from the cache if we're using it.
            // This covers the case where two idential data subscriptions are created.
            var dummy = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First().DataTimeZone);
            var customData = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First().DataTimeZone);

            Assert.IsTrue(customData.Symbol.HasUnderlying, $"Custom data added as {ticker} Symbol with SecurityType {securityType} does not have underlying");
            Assert.AreEqual(customData.Symbol.Underlying, asset.Symbol, $"Custom data underlying does not match {securityType} Symbol for {ticker}");

            var assetSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First();
            var customDataSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Base).Single();

            var assetShouldBeMapped = assetSubscription.TickerShouldBeMapped();
            var customShouldBeMapped = customDataSubscription.TickerShouldBeMapped();

            if (securityType == SecurityType.Equity)
            {
                Assert.AreEqual(securityShouldBeMapped, assetShouldBeMapped);
                Assert.AreEqual(customDataShouldBeMapped, customShouldBeMapped);

                Assert.AreNotEqual(assetSubscription, customDataSubscription);

                if (assetShouldBeMapped == customShouldBeMapped)
                {
                    Assert.AreEqual(assetSubscription.MappedSymbol, customDataSubscription.MappedSymbol);
                    Assert.AreEqual(asset.Symbol.Value, customData.Symbol.Value.Split('.').First());
                }
            }
        }

        [TestCase("EURUSD", typeof(TradingEconomicsCalendar), SecurityType.Cfd, false, false)]
        [TestCase("BTCUSD", typeof(TradingEconomicsCalendar), SecurityType.Crypto, false, false)]
        [TestCase("CL", typeof(TradingEconomicsCalendar), SecurityType.Future, false, false)]
        [TestCase("AAPL", typeof(TradingEconomicsCalendar), SecurityType.Equity, true, false)]
        [TestCase("EURUSD", typeof(TradingEconomicsCalendar), SecurityType.Forex, false, false)]
        public void AddDataSecurityTickerNoUnderlying(string ticker, Type customDataType, SecurityType securityType, bool securityShouldBeMapped, bool customDataShouldBeMapped)
        {
            SymbolCache.Clear();
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            Security asset;

            switch (securityType)
            {
                case SecurityType.Cfd:
                    asset = qcAlgorithm.AddCfd(ticker, Resolution.Daily);
                    break;
                case SecurityType.Crypto:
                    asset = qcAlgorithm.AddCrypto(ticker, Resolution.Daily);
                    break;
                case SecurityType.Equity:
                    asset = qcAlgorithm.AddEquity(ticker, Resolution.Daily);
                    break;
                case SecurityType.Forex:
                    asset = qcAlgorithm.AddForex(ticker, Resolution.Daily);
                    break;
                case SecurityType.Future:
                    asset = qcAlgorithm.AddFuture(ticker, Resolution.Daily);
                    break;
                default:
                    throw new Exception($"SecurityType {securityType} is not valid for this test");
            }

            // Dummy here is meant to try to corrupt the SymbolCache. Ideally, SymbolCache should return non-custom data types with higher priority
            // in case we want to add two custom data types, but still have them associated with the equity from the cache if we're using it.
            // This covers the case where two idential data subscriptions are created.
            var dummy = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First().DataTimeZone);
            var customData = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First().DataTimeZone);

            // Check to see if we have an underlying symbol when we shouldn't
            Assert.IsFalse(customData.Symbol.HasUnderlying, $"{customDataType.Name} has underlying symbol for SecurityType {securityType} with ticker {ticker}");
            Assert.AreEqual(customData.Symbol.Underlying, null, $"{customDataType.Name} - Custom data underlying Symbol for SecurityType {securityType} is not null");

            var assetSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == securityType).First();
            var customDataSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Base).Single();

            var assetShouldBeMapped = assetSubscription.TickerShouldBeMapped();
            var customShouldBeMapped = customDataSubscription.TickerShouldBeMapped();

            Assert.AreEqual(securityShouldBeMapped, assetShouldBeMapped);
            Assert.AreEqual(customDataShouldBeMapped, customShouldBeMapped);

            Assert.AreNotEqual(assetSubscription, customDataSubscription);

            if (assetShouldBeMapped == customShouldBeMapped)
            {
                // Would fail with CL future without this check because MappedSymbol returns "/CL" for the Future symbol
                if (assetSubscription.SecurityType == SecurityType.Future)
                {
                    Assert.AreNotEqual(assetSubscription.MappedSymbol, customDataSubscription.MappedSymbol);
                    Assert.AreNotEqual(asset.Symbol.Value, customData.Symbol.Value.Split('.').First());
                }
                else
                {
                    Assert.AreEqual(assetSubscription.MappedSymbol, customDataSubscription.MappedSymbol);
                    Assert.AreEqual(asset.Symbol.Value, customData.Symbol.Value.Split('.').First());
                }
            }
        }

        [TestCase("AAPL", typeof(PsychSignalSentiment), true)]
        [TestCase("TWX", typeof(PsychSignalSentiment), true)]
        [TestCase("FB", typeof(PsychSignalSentiment), true)]
        [TestCase("NFLX", typeof(PsychSignalSentiment), true)]
        [TestCase("TWX", typeof(TradingEconomicsCalendar), false)]
        [TestCase("AAPL", typeof(TradingEconomicsCalendar), false)]
        public void AddDataOptionsSymbolHasChainedUnderlyingSymbols(string ticker, Type customDataType, bool customDataShouldBeMapped)
        {
            SymbolCache.Clear();
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            var asset = qcAlgorithm.AddOption(ticker);

            // Dummy here is meant to try to corrupt the SymbolCache. Ideally, SymbolCache should return non-custom data types with higher priority
            // in case we want to add two custom data types, but still have them associated with the equity from the cache if we're using it.
            // This covers the case where two idential data subscriptions are created.
            var dummy = qcAlgorithm.AddData(customDataType, asset.Symbol, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single().DataTimeZone);
            var customData = qcAlgorithm.AddData(customDataType, asset.Symbol, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single().DataTimeZone);

            // Check to see if we have an underlying symbol when we shouldn't
            Assert.IsTrue(customData.Symbol.HasUnderlying, $"{customDataType.Name} - {ticker} has no underlying Symbol");
            Assert.AreEqual(customData.Symbol.Underlying, asset.Symbol);
            Assert.AreEqual(customData.Symbol.Underlying.Underlying, asset.Symbol.Underlying);
            Assert.AreEqual(customData.Symbol.Underlying.Underlying.Underlying, null);

            var assetSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single();
            var customDataSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Base).Single();

            Assert.IsTrue(assetSubscription.TickerShouldBeMapped());
            Assert.AreEqual(customDataShouldBeMapped, customDataSubscription.TickerShouldBeMapped());

            Assert.AreEqual($"?{assetSubscription.MappedSymbol}", customDataSubscription.MappedSymbol);
        }

        [TestCase("AAPL", typeof(PsychSignalSentiment))]
        [TestCase("TWX", typeof(PsychSignalSentiment))]
        [TestCase("FB", typeof(PsychSignalSentiment))]
        [TestCase("NFLX", typeof(PsychSignalSentiment))]
        public void AddDataOptionsTickerHasChainedUnderlyingSymbol(string ticker, Type customDataType)
        {
            SymbolCache.Clear();
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            var asset = qcAlgorithm.AddOption(ticker);

            // Dummy here is meant to try to corrupt the SymbolCache. Ideally, SymbolCache should return non-custom data types with higher priority
            // in case we want to add two custom data types, but still have them associated with the equity from the cache if we're using it.
            // This covers the case where two idential data subscriptions are created.
            var dummy = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single().DataTimeZone);
            var customData = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single().DataTimeZone);

            // Check to see if we have an underlying symbol when we shouldn't
            Assert.IsTrue(customData.Symbol.HasUnderlying, $"{customDataType.Name} - {ticker} has no underlying Symbol");
            Assert.AreNotEqual(customData.Symbol.Underlying, asset.Symbol);
            Assert.IsFalse(customData.Symbol.Underlying.HasUnderlying);
            Assert.AreEqual(customData.Symbol.Underlying, asset.Symbol.Underlying);

            var assetSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single();
            var customDataSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Base).Single();

            Assert.IsTrue(assetSubscription.TickerShouldBeMapped());
            Assert.IsTrue(customDataSubscription.TickerShouldBeMapped());

            Assert.AreEqual(assetSubscription.MappedSymbol, customDataSubscription.MappedSymbol);
        }

        [TestCase("AAPL", typeof(TradingEconomicsCalendar))]
        [TestCase("FDTR", typeof(TradingEconomicsCalendar))]
        public void AddDataOptionsTickerHasNoChainedUnderlyingSymbols(string ticker, Type customDataType)
        {
            SymbolCache.Clear();
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            var asset = qcAlgorithm.AddOption(ticker);

            // Dummy here is meant to try to corrupt the SymbolCache. Ideally, SymbolCache should return non-custom data types with higher priority
            // in case we want to add two custom data types, but still have them associated with the equity from the cache if we're using it.
            // This covers the case where two idential data subscriptions are created.
            var dummy = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single().DataTimeZone);
            var customData = qcAlgorithm.AddData(customDataType, ticker, Resolution.Daily, qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single().DataTimeZone);

            // Check to see if we have an underlying symbol when we shouldn't
            Assert.IsFalse(customData.Symbol.HasUnderlying, $"{customDataType.Name} has an underlying Symbol");

            var assetSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Option).Single();
            var customDataSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.Where(x => x.SecurityType == SecurityType.Base).Single();

            Assert.IsTrue(assetSubscription.TickerShouldBeMapped());
            Assert.IsFalse(customDataSubscription.TickerShouldBeMapped());

            //Assert.AreNotEqual(assetSubscription.MappedSymbol, customDataSubscription.MappedSymbol);
        }

        [Test]
        public void PythonCustomDataTypes_AreAddedToSubscriptions_Successfully()
        {
            var qcAlgorithm = new AlgorithmPythonWrapper("Test_CustomDataAlgorithm");
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            // Initialize contains the statements:
            // self.AddData(Nifty, "NIFTY")
            // self.AddData(QuandlFuture, "SCF/CME_CL1_ON", Resolution.Daily)
            qcAlgorithm.Initialize();

            var niftySubscription = qcAlgorithm.SubscriptionManager.Subscriptions.FirstOrDefault(x => x.Symbol.Value == "NIFTY");
            Assert.IsNotNull(niftySubscription);

            var niftyFactory = (BaseData)ObjectActivator.GetActivator(niftySubscription.Type).Invoke(new object[] { niftySubscription.Type });
            Assert.DoesNotThrow(() => niftyFactory.GetSource(niftySubscription, DateTime.UtcNow, false));

            var quandlSubscription = qcAlgorithm.SubscriptionManager.Subscriptions.FirstOrDefault(x => x.Symbol.Value == "SCF/CME_CL1_ON");
            Assert.IsNotNull(quandlSubscription);

            var quandlFactory = (BaseData)ObjectActivator.GetActivator(quandlSubscription.Type).Invoke(new object[] { quandlSubscription.Type });
            Assert.DoesNotThrow(() => quandlFactory.GetSource(quandlSubscription, DateTime.UtcNow, false));
        }

        [Test]
        public void PythonCustomDataTypes_AreAddedToConsolidator_Successfully()
        {
            var qcAlgorithm = new AlgorithmPythonWrapper("Test_CustomDataAlgorithm");
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));

            // Initialize contains the statements:
            // self.AddData(Nifty, "NIFTY")
            // self.AddData(QuandlFuture, "SCF/CME_CL1_ON", Resolution.Daily)
            qcAlgorithm.Initialize();

            var niftyConsolidator = new DynamicDataConsolidator(TimeSpan.FromDays(2));
            Assert.DoesNotThrow(() => qcAlgorithm.SubscriptionManager.AddConsolidator("NIFTY", niftyConsolidator));

            var quandlConsolidator = new DynamicDataConsolidator(TimeSpan.FromDays(2));
            Assert.DoesNotThrow(() => qcAlgorithm.SubscriptionManager.AddConsolidator("SCF/CME_CL1_ON", quandlConsolidator));
        }

        [Test]
        public void AddingInvalidDataTypeThrows()
        {
            var qcAlgorithm = new QCAlgorithm();
            qcAlgorithm.SubscriptionManager.SetDataManager(new DataManagerStub(qcAlgorithm));
            Assert.Throws<ArgumentException>(() => qcAlgorithm.AddData(typeof(double),
                "double",
                Resolution.Daily,
                DateTimeZone.Utc));
        }

        [Test]
        public void AppendsCustomDataTypeName_ToSecurityIdentifierSymbol()
        {
            const string ticker = "ticker";
            var algorithm = Algorithm();

            var security = algorithm.AddData<Quandl>(ticker);
            Assert.AreEqual(ticker.ToUpperInvariant(), security.Symbol.Value);
            Assert.AreEqual($"{ticker.ToUpperInvariant()}.{typeof(Quandl).Name}", security.Symbol.ID.Symbol);
            Assert.AreEqual(SecurityIdentifier.GenerateBaseSymbol(typeof(Quandl), ticker), security.Symbol.ID.Symbol);
        }

        [Test]
        public void RegistersSecurityIdentifierSymbol_AsTickerString_InSymbolCache()
        {
            var algorithm = Algorithm();

            Symbol cachedSymbol;
            var security = algorithm.AddData<Quandl>("ticker");
            var symbolCacheAlias = security.Symbol.ID.Symbol;

            Assert.IsTrue(SymbolCache.TryGetSymbol(symbolCacheAlias, out cachedSymbol));
            Assert.AreSame(security.Symbol, cachedSymbol);
        }

        [Test]
        public void DoesNotCauseCollision_WhenRegisteringMultipleDifferentCustomDataTypes_WithSameTicker()
        {
            const string ticker = "ticker";
            var algorithm = Algorithm();

            var security1 = algorithm.AddData<Quandl>(ticker);
            var security2 = algorithm.AddData<Bitcoin>(ticker);

            var quandl = algorithm.Securities[security1.Symbol];
            Assert.AreSame(security1, quandl);

            var bitcoin = algorithm.Securities[security2.Symbol];
            Assert.AreSame(security2, bitcoin);

            Assert.AreNotSame(quandl, bitcoin);
        }

        private static SubscriptionDataConfig GetMatchingSubscription(Security security, Type type)
        {
            // find a subscription matchin the requested type with a higher resolution than requested
            return (from sub in security.Subscriptions.OrderByDescending(s => s.Resolution)
                    where type.IsAssignableFrom(sub.Type)
                    select sub).FirstOrDefault();
        }

        private static QCAlgorithm Algorithm()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            return algorithm;
        }

        private class TestHistoryProvider : HistoryProviderBase
        {
            public string underlyingSymbol = "GOOG";
            public string underlyingSymbol2 = "AAPL";
            public override int DataPointCount { get; }
            public Resolution LastResolutionRequest;

            public override void Initialize(HistoryProviderInitializeParameters parameters)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                var now = DateTime.UtcNow;
                LastResolutionRequest = requests.First().Resolution;
                var tradeBar1 = new TradeBar(now, underlyingSymbol, 1, 1, 1, 1, 1, TimeSpan.FromDays(1));
                var tradeBar2 = new TradeBar(now, underlyingSymbol2, 3, 3, 3, 3, 3, TimeSpan.FromDays(1));
                var slice1 = new Slice(now, new List<BaseData> { tradeBar1, tradeBar2 },
                                    new TradeBars(now), new QuoteBars(),
                                    new Ticks(), new OptionChains(),
                                    new FuturesChains(), new Splits(),
                                    new Dividends(now), new Delistings(),
                                    new SymbolChangedEvents());
                var tradeBar1_2 = new TradeBar(now, underlyingSymbol, 2, 2, 2, 2, 2, TimeSpan.FromDays(1));
                var slice2 = new Slice(now, new List<BaseData> { tradeBar1_2 },
                    new TradeBars(now), new QuoteBars(),
                    new Ticks(), new OptionChains(),
                    new FuturesChains(), new Splits(),
                    new Dividends(now), new Delistings(),
                    new SymbolChangedEvents());
                return new[] { slice1, slice2 };
            }
        }
    }
}
