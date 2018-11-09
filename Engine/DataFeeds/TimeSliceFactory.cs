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
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Instance base class that will provide methods for creating new <see cref="TimeSlice"/>
    /// </summary>
    public class TimeSliceFactory
    {
        /// <summary>
        /// The time zone required for computing algorithm and slice time
        /// </summary>
        public DateTimeZone TimeZone { get; set; }

        /// <summary>
        /// Creates a new <see cref="TimeSlice"/> for the specified time using the specified data
        /// </summary>
        /// <param name="utcDateTime">The UTC frontier date time</param>
        /// <param name="data">The data in this <see cref="TimeSlice"/></param>
        /// <param name="changes">The new changes that are seen in this time slice as a result of universe selection</param>
        /// <param name="universeData"></param>
        /// <returns>A new <see cref="TimeSlice"/> containing the specified data</returns>
        public TimeSlice Create(DateTime utcDateTime,
            List<DataFeedPacket> data,
            SecurityChanges changes,
            Dictionary<Universe, BaseDataCollection> universeData)
        {
            int count = 0;
            var security = new List<UpdateData<ISecurityPrice>>();
            var custom = new List<UpdateData<ISecurityPrice>>();
            var consolidator = new List<UpdateData<SubscriptionDataConfig>>();
            var allDataForAlgorithm = new List<BaseData>(data.Count);
            var optionUnderlyingUpdates = new Dictionary<Symbol, BaseData>();

            Split split;
            Dividend dividend;
            Delisting delisting;
            SymbolChangedEvent symbolChange;

            // we need to be able to reference the slice being created in order to define the
            // evaluation of option price models, so we define a 'future' that can be referenced
            // in the option price model evaluation delegates for each contract
            Slice slice = null;
            var sliceFuture = new Lazy<Slice>(() => slice);

            var algorithmTime = utcDateTime.ConvertFromUtc(TimeZone);
            var tradeBars = new TradeBars(algorithmTime);
            var quoteBars = new QuoteBars(algorithmTime);
            var ticks = new Ticks(algorithmTime);
            var splits = new Splits(algorithmTime);
            var dividends = new Dividends(algorithmTime);
            var delistings = new Delistings(algorithmTime);
            var optionChains = new OptionChains(algorithmTime);
            var futuresChains = new FuturesChains(algorithmTime);
            var symbolChanges = new SymbolChangedEvents(algorithmTime);

            if (universeData.Count > 0)
            {
                // count universe data
                foreach (var kvp in universeData)
                {
                    count += kvp.Value.Data.Count;
                }
            }

            // ensure we read equity data before option data, so we can set the current underlying price
            foreach (var packet in data)
            {
                // filter out packets for removed subscriptions
                if (packet.IsSubscriptionRemoved)
                {
                    continue;
                }

                var list = packet.Data;
                var symbol = packet.Configuration.Symbol;

                if (list.Count == 0) continue;

                // keep count of all data points
                if (list.Count == 1 && list[0] is BaseDataCollection)
                {
                    var baseDataCollectionCount = ((BaseDataCollection)list[0]).Data.Count;
                    if (baseDataCollectionCount == 0)
                    {
                        continue;
                    }
                    count += baseDataCollectionCount;
                }
                else
                {
                    count += list.Count;
                }

                if (!packet.Configuration.IsInternalFeed && packet.Configuration.IsCustomData)
                {
                    // This is all the custom data
                    custom.Add(new UpdateData<ISecurityPrice>(packet.Security, packet.Configuration.Type, list));
                }

                var securityUpdate = new List<BaseData>(list.Count);
                var consolidatorUpdate = new List<BaseData>(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var baseData = list[i];
                    if (!packet.Configuration.IsInternalFeed)
                    {
                        // this is all the data that goes into the algorithm
                        allDataForAlgorithm.Add(baseData);
                    }
                    // don't add internal feed data to ticks/bars objects
                    if (baseData.DataType != MarketDataType.Auxiliary)
                    {
                        if (!packet.Configuration.IsInternalFeed)
                        {
                            PopulateDataDictionaries(baseData, ticks, tradeBars, quoteBars, optionChains, futuresChains);

                            // special handling of options data to build the option chain
                            if (symbol.SecurityType == SecurityType.Option)
                            {
                                if (baseData.DataType == MarketDataType.OptionChain)
                                {
                                    optionChains[baseData.Symbol] = (OptionChain)baseData;
                                }
                                else if (!HandleOptionData(algorithmTime, baseData, optionChains, packet.Security, sliceFuture, optionUnderlyingUpdates))
                                {
                                    continue;
                                }
                            }

                            // special handling of futures data to build the futures chain
                            if (symbol.SecurityType == SecurityType.Future)
                            {
                                if (baseData.DataType == MarketDataType.FuturesChain)
                                {
                                    futuresChains[baseData.Symbol] = (FuturesChain)baseData;
                                }
                                else if (!HandleFuturesData(algorithmTime, baseData, futuresChains, packet.Security))
                                {
                                    continue;
                                }
                            }

                            // this is data used to update consolidators
                            consolidatorUpdate.Add(baseData);
                        }

                        // this is the data used set market prices
                        // do not add it if it is a Suspicious tick
                        var tick = baseData as Tick;
                        if (tick != null && tick.Suspicious) continue;

                        securityUpdate.Add(baseData);

                        // option underlying security update
                        if (symbol.SecurityType == SecurityType.Equity)
                        {
                            optionUnderlyingUpdates[symbol] = baseData;
                        }
                    }
                    // include checks for various aux types so we don't have to construct the dictionaries in Slice
                    else if ((delisting = baseData as Delisting) != null)
                    {
                        delistings[symbol] = delisting;
                    }
                    else if ((dividend = baseData as Dividend) != null)
                    {
                        dividends[symbol] = dividend;
                    }
                    else if ((split = baseData as Split) != null)
                    {
                        splits[symbol] = split;
                    }
                    else if ((symbolChange = baseData as SymbolChangedEvent) != null)
                    {
                        // symbol changes is keyed by the requested symbol
                        symbolChanges[packet.Configuration.Symbol] = symbolChange;
                    }
                }

                if (securityUpdate.Count > 0)
                {
                    security.Add(new UpdateData<ISecurityPrice>(packet.Security, packet.Configuration.Type, securityUpdate));
                }
                if (consolidatorUpdate.Count > 0)
                {
                    consolidator.Add(new UpdateData<SubscriptionDataConfig>(packet.Configuration, packet.Configuration.Type, consolidatorUpdate));
                }
            }

            slice = new Slice(algorithmTime, allDataForAlgorithm, tradeBars, quoteBars, ticks, optionChains, futuresChains, splits, dividends, delistings, symbolChanges, allDataForAlgorithm.Count > 0);

            return new TimeSlice(utcDateTime, count, slice, data, security, consolidator, custom, changes, universeData);
        }

        /// <summary>
        /// Adds the specified <see cref="BaseData"/> instance to the appropriate <see cref="DataDictionary{T}"/>
        /// </summary>
        private void PopulateDataDictionaries(BaseData baseData, Ticks ticks, TradeBars tradeBars, QuoteBars quoteBars, OptionChains optionChains, FuturesChains futuresChains)
        {
            var symbol = baseData.Symbol;

            // populate data dictionaries
            switch (baseData.DataType)
            {
                case MarketDataType.Tick:
                    ticks.Add(symbol, (Tick)baseData);
                    break;

                case MarketDataType.TradeBar:
                    tradeBars[symbol] = (TradeBar)baseData;
                    break;

                case MarketDataType.QuoteBar:
                    quoteBars[symbol] = (QuoteBar)baseData;
                    break;

                case MarketDataType.OptionChain:
                    optionChains[symbol] = (OptionChain)baseData;
                    break;

                case MarketDataType.FuturesChain:
                    futuresChains[symbol] = (FuturesChain)baseData;
                    break;
            }
        }

        private bool HandleOptionData(DateTime algorithmTime, BaseData baseData, OptionChains optionChains, ISecurityPrice security, Lazy<Slice> sliceFuture, IReadOnlyDictionary<Symbol, BaseData> optionUnderlyingUpdates)
        {
            var symbol = baseData.Symbol;

            OptionChain chain;
            var canonical = Symbol.CreateOption(symbol.Underlying, symbol.ID.Market, default(OptionStyle), default(OptionRight), 0, SecurityIdentifier.DefaultDate);
            if (!optionChains.TryGetValue(canonical, out chain))
            {
                chain = new OptionChain(canonical, algorithmTime);
                optionChains[canonical] = chain;
            }

            // set the underlying current data point in the option chain
            var option = security as IOptionPrice;
            if (option != null)
            {
                if (option.Underlying == null)
                {
                    Log.Error($"TimeSlice.HandleOptionData(): {algorithmTime}: Option underlying is null");
                    return false;
                }

                BaseData underlyingData;
                if (!optionUnderlyingUpdates.TryGetValue(option.Underlying.Symbol, out underlyingData))
                {
                    underlyingData = option.Underlying.GetLastData();
                }

                if (underlyingData == null)
                {
                    Log.Error($"TimeSlice.HandleOptionData(): {algorithmTime}: Option underlying GetLastData returned null");
                    return false;
                }
                chain.Underlying = underlyingData;
            }

            var universeData = baseData as OptionChainUniverseDataCollection;
            if (universeData != null)
            {
                if (universeData.Underlying != null)
                {
                    foreach (var addedContract in chain.Contracts)
                    {
                        addedContract.Value.UnderlyingLastPrice = chain.Underlying.Price;
                    }
                }
                foreach (var contractSymbol in universeData.FilteredContracts)
                {
                    chain.FilteredContracts.Add(contractSymbol);
                }
                return false;
            }

            OptionContract contract;
            if (!chain.Contracts.TryGetValue(baseData.Symbol, out contract))
            {
                var underlyingSymbol = baseData.Symbol.Underlying;
                contract = new OptionContract(baseData.Symbol, underlyingSymbol)
                {
                    Time = baseData.EndTime,
                    LastPrice = security.Close,
                    Volume = (long)security.Volume,
                    BidPrice = security.BidPrice,
                    BidSize = (long)security.BidSize,
                    AskPrice = security.AskPrice,
                    AskSize = (long)security.AskSize,
                    OpenInterest = security.OpenInterest,
                    UnderlyingLastPrice = chain.Underlying.Price
                };

                chain.Contracts[baseData.Symbol] = contract;

                if (option != null)
                {
                    contract.SetOptionPriceModel(() => option.EvaluatePriceModel(sliceFuture.Value, contract));
                }
            }

            // populate ticks and tradebars dictionaries with no aux data
            switch (baseData.DataType)
            {
                case MarketDataType.Tick:
                    var tick = (Tick)baseData;
                    chain.Ticks.Add(tick.Symbol, tick);
                    UpdateContract(contract, tick);
                    break;

                case MarketDataType.TradeBar:
                    var tradeBar = (TradeBar)baseData;
                    chain.TradeBars[symbol] = tradeBar;
                    UpdateContract(contract, tradeBar);
                    break;

                case MarketDataType.QuoteBar:
                    var quote = (QuoteBar)baseData;
                    chain.QuoteBars[symbol] = quote;
                    UpdateContract(contract, quote);
                    break;

                case MarketDataType.Base:
                    chain.AddAuxData(baseData);
                    break;
            }
            return true;
        }


        private bool HandleFuturesData(DateTime algorithmTime, BaseData baseData, FuturesChains futuresChains, ISecurityPrice security)
        {
            var symbol = baseData.Symbol;

            FuturesChain chain;
            var canonical = Symbol.Create(symbol.ID.Symbol, SecurityType.Future, symbol.ID.Market);
            if (!futuresChains.TryGetValue(canonical, out chain))
            {
                chain = new FuturesChain(canonical, algorithmTime);
                futuresChains[canonical] = chain;
            }

            var universeData = baseData as FuturesChainUniverseDataCollection;
            if (universeData != null)
            {
                foreach (var contractSymbol in universeData.FilteredContracts)
                {
                    chain.FilteredContracts.Add(contractSymbol);
                }
                return false;
            }

            FuturesContract contract;
            if (!chain.Contracts.TryGetValue(baseData.Symbol, out contract))
            {
                var underlyingSymbol = baseData.Symbol.Underlying;
                contract = new FuturesContract(baseData.Symbol, underlyingSymbol)
                {
                    Time = baseData.EndTime,
                    LastPrice = security.Close,
                    Volume = (long)security.Volume,
                    BidPrice = security.BidPrice,
                    BidSize = (long)security.BidSize,
                    AskPrice = security.AskPrice,
                    AskSize = (long)security.AskSize,
                    OpenInterest = security.OpenInterest
                };
                chain.Contracts[baseData.Symbol] = contract;
            }

            // populate ticks and tradebars dictionaries with no aux data
            switch (baseData.DataType)
            {
                case MarketDataType.Tick:
                    var tick = (Tick)baseData;
                    chain.Ticks.Add(tick.Symbol, tick);
                    UpdateContract(contract, tick);
                    break;

                case MarketDataType.TradeBar:
                    var tradeBar = (TradeBar)baseData;
                    chain.TradeBars[symbol] = tradeBar;
                    UpdateContract(contract, tradeBar);
                    break;

                case MarketDataType.QuoteBar:
                    var quote = (QuoteBar)baseData;
                    chain.QuoteBars[symbol] = quote;
                    UpdateContract(contract, quote);
                    break;

                case MarketDataType.Base:
                    chain.AddAuxData(baseData);
                    break;
            }
            return true;
        }

        private static void UpdateContract(OptionContract contract, QuoteBar quote)
        {
            if (quote.Ask != null && quote.Ask.Close != 0m)
            {
                contract.AskPrice = quote.Ask.Close;
                contract.AskSize = (long)quote.LastAskSize;
            }
            if (quote.Bid != null && quote.Bid.Close != 0m)
            {
                contract.BidPrice = quote.Bid.Close;
                contract.BidSize = (long)quote.LastBidSize;
            }
        }

        private static void UpdateContract(OptionContract contract, Tick tick)
        {
            if (tick.TickType == TickType.Trade)
            {
                contract.LastPrice = tick.Price;
            }
            else if (tick.TickType == TickType.Quote)
            {
                if (tick.AskPrice != 0m)
                {
                    contract.AskPrice = tick.AskPrice;
                    contract.AskSize = (long)tick.AskSize;
                }
                if (tick.BidPrice != 0m)
                {
                    contract.BidPrice = tick.BidPrice;
                    contract.BidSize = (long)tick.BidSize;
                }
            }
            else if (tick.TickType == TickType.OpenInterest)
            {
                if (tick.Value != 0m)
                {
                    contract.OpenInterest = tick.Value;
                }
            }
        }

        private static void UpdateContract(OptionContract contract, TradeBar tradeBar)
        {
            if (tradeBar.Close == 0m) return;
            contract.LastPrice = tradeBar.Close;
            contract.Volume = (long)tradeBar.Volume;
        }

        private static void UpdateContract(FuturesContract contract, QuoteBar quote)
        {
            if (quote.Ask != null && quote.Ask.Close != 0m)
            {
                contract.AskPrice = quote.Ask.Close;
                contract.AskSize = (long)quote.LastAskSize;
            }
            if (quote.Bid != null && quote.Bid.Close != 0m)
            {
                contract.BidPrice = quote.Bid.Close;
                contract.BidSize = (long)quote.LastBidSize;
            }
        }

        private static void UpdateContract(FuturesContract contract, Tick tick)
        {
            if (tick.TickType == TickType.Trade)
            {
                contract.LastPrice = tick.Price;
            }
            else if (tick.TickType == TickType.Quote)
            {
                if (tick.AskPrice != 0m)
                {
                    contract.AskPrice = tick.AskPrice;
                    contract.AskSize = (long)tick.AskSize;
                }
                if (tick.BidPrice != 0m)
                {
                    contract.BidPrice = tick.BidPrice;
                    contract.BidSize = (long)tick.BidSize;
                }
            }
            else if (tick.TickType == TickType.OpenInterest)
            {
                if (tick.Value != 0m)
                {
                    contract.OpenInterest = tick.Value;
                }
            }
        }

        private static void UpdateContract(FuturesContract contract, TradeBar tradeBar)
        {
            if (tradeBar.Close == 0m) return;
            contract.LastPrice = tradeBar.Close;
            contract.Volume = (long)tradeBar.Volume;
        }
    }
}
