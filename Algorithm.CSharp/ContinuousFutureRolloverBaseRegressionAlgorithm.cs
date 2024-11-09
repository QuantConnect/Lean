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

using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using System;
using QuantConnect.Util;
using System.Linq;
using NodaTime;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base class for regression algorithms testing that when a continuous future rollover happens,
    /// the continuous contract is updated correctly with the new contract data, regardless of the
    /// offset between the exchange time zone and the data time zone.
    /// </summary>
    public abstract class ContinuousFutureRolloverBaseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        const string Ticker = Futures.Indices.SP500EMini;

        private Future _continuousContract;

        private DateTime _rolloverTime;

        private MarketHoursDatabase.Entry _originalMhdbEntry;

        protected abstract Resolution Resolution { get; }

        protected abstract Offset ExchangeToDataTimeZoneOffset { get; }

        private DateTimeZone DataTimeZone => TimeZones.Utc;

        private DateTimeZone ExchangeTimeZone => DateTimeZone.ForOffset(ExchangeToDataTimeZoneOffset);

        private bool RolloverHappened => _rolloverTime != DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 12, 20);

            _originalMhdbEntry = MarketHoursDatabase.GetEntry(Market.CME, Ticker, SecurityType.Future);
            var exchangeHours = new SecurityExchangeHours(ExchangeTimeZone,
                _originalMhdbEntry.ExchangeHours.Holidays,
                _originalMhdbEntry.ExchangeHours.MarketHours.ToDictionary(),
                _originalMhdbEntry.ExchangeHours.EarlyCloses,
                _originalMhdbEntry.ExchangeHours.LateOpens);
            MarketHoursDatabase.SetEntry(Market.CME, Ticker, SecurityType.Future, exchangeHours, DataTimeZone);

            SetTimeZone(ExchangeTimeZone);

            _continuousContract = AddFuture(Ticker,
                Resolution,
                extendedMarketHours: true,
                dataNormalizationMode: DataNormalizationMode.Raw,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 0
            );

            SetBenchmark(x => 0);
        }

        public override void OnData(Slice slice)
        {
            try
            {
                var receivedRollover = false;
                foreach (var (symbol, symbolChangedEvent) in slice.SymbolChangedEvents)
                {
                    if (RolloverHappened)
                    {
                        throw new RegressionTestException($"[{Time}] -- Unexpected symbol changed event for {symbol}. Expected only one mapping.");
                    }

                    receivedRollover = true;
                    _rolloverTime = symbolChangedEvent.EndTime;

                    var oldSymbol = symbolChangedEvent.OldSymbol;
                    var newSymbol = symbolChangedEvent.NewSymbol;
                    Debug($"[{Time}] -- Rollover: {oldSymbol} -> {newSymbol}");

                    if (symbol != _continuousContract.Symbol)
                    {
                        throw new RegressionTestException($"[{Time}] -- Unexpected symbol changed event for {symbol}");
                    }

                    var expectedMappingDate = new DateTime(2013, 12, 18);
                    if (_rolloverTime != expectedMappingDate)
                    {
                        throw new RegressionTestException($"[{Time}] -- Unexpected date {_rolloverTime}. Expected {expectedMappingDate}");
                    }

                    var expectedMappingOldSymbol = "ES VMKLFZIH2MTD";
                    var expectedMappingNewSymbol = "ES VP274HSU1AF5";
                    if (symbolChangedEvent.OldSymbol != expectedMappingOldSymbol || symbolChangedEvent.NewSymbol != expectedMappingNewSymbol)
                    {
                        throw new RegressionTestException($"[{Time}] -- Unexpected mapping. " +
                            $"Expected {expectedMappingOldSymbol} -> {expectedMappingNewSymbol} " +
                            $"but was {symbolChangedEvent.OldSymbol} -> {symbolChangedEvent.NewSymbol}");
                    }
                }

                var mappedFuture = Securities[_continuousContract.Mapped];
                var mappedFuturePrice = mappedFuture.Price;

                var otherFuture = Securities.Values.SingleOrDefault(x => !x.Symbol.IsCanonical() && x.Symbol != _continuousContract.Mapped);
                var otherFuturePrice = otherFuture?.Price;

                var continuousContractPrice = _continuousContract.Price;

                Debug($"[{Time}] Contracts prices:\n" +
                    $"  -- Mapped future: {mappedFuture.Symbol} :: {mappedFuture.Price} :: {mappedFuture.GetLastData()}\n" +
                    $"  -- Other future: {otherFuture?.Symbol} :: {otherFuture?.Price} :: {otherFuture?.GetLastData()}\n" +
                    $"  -- Mapped future from continuous contract: {_continuousContract.Symbol} :: {_continuousContract.Mapped} :: " +
                    $"{_continuousContract.Price} :: {_continuousContract.GetLastData()}\n");

                if (receivedRollover)
                {
                    if (continuousContractPrice != otherFuturePrice)
                    {
                        var continuousContractLastData = _continuousContract.GetLastData();
                        throw new RegressionTestException($"[{Time}] -- Prices do not match. " +
                            $"At the time of the rollover, expected continuous future price to be the same as " +
                            $"the previously mapped contract since no data for the new mapped contract has been received:\n" +
                            $"   Continuous contract ({_continuousContract.Symbol}) price: " +
                            $"{continuousContractPrice} :: {continuousContractLastData.Symbol.Underlying} :: " +
                            $"{continuousContractLastData.Time} - {continuousContractLastData.EndTime} :: {continuousContractLastData}. \n" +
                            $"   Mapped contract ({mappedFuture.Symbol}) price: {mappedFuturePrice} :: {mappedFuture.GetLastData()}. \n" +
                            $"   Other contract ({otherFuture?.Symbol}) price: {otherFuturePrice} :: {otherFuture?.GetLastData()}\n");
                    }
                }
                else if (mappedFuturePrice != 0 || !RolloverHappened)
                {
                    if (continuousContractPrice != mappedFuturePrice)
                    {
                        var continuousContractLastData = _continuousContract.GetLastData();
                        throw new RegressionTestException($"[{Time}] -- Prices do not match. " +
                            $"Expected continuous future price to be the same as the mapped contract:\n" +
                            $"   Continuous contract ({_continuousContract.Symbol}) price: {continuousContractPrice} :: " +
                            $"{continuousContractLastData.Symbol.Underlying} :: {continuousContractLastData}. \n" +
                            $"   Mapped contract ({mappedFuture.Symbol}) price: {mappedFuturePrice} :: {mappedFuture.GetLastData()}. \n" +
                            $"   Other contract ({otherFuture?.Symbol}) price: {otherFuturePrice} :: {otherFuture?.GetLastData()}\n");
                    }
                }
                // No data for the mapped future yet after rollover
                else
                {
                    if (otherFuture == null)
                    {
                        throw new RegressionTestException($"[{Time}] --" +
                            $" Mapped future price is 0 (no data has arrived) so the previous mapped contract is expected to be there");
                    }

                    var continuousContractLastData = _continuousContract.GetLastData();

                    if (continuousContractLastData.EndTime > _rolloverTime)
                    {
                        throw new RegressionTestException($"[{Time}] -- Expected continuous future contract last data to be from the previously " +
                            $"mapped contract until the new mapped contract gets data:\n" +
                            $"   Rollover time: {_rolloverTime}\n" +
                            $"   Continuous contract ({_continuousContract.Symbol}) last data: " +
                            $"{continuousContractLastData.Symbol.Underlying} :: " +
                            $"{continuousContractLastData.Time} - {continuousContractLastData.EndTime} :: {continuousContractLastData}.");
                    }
                }
            }
            catch (Exception ex)
            {
                ResetMarketHoursDatabase();
                throw;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            ResetMarketHoursDatabase();

            if (!RolloverHappened)
            {
                throw new RegressionTestException($"[{Time}] -- Rollover did not happen.");
            }
        }

        private void ResetMarketHoursDatabase()
        {
            MarketHoursDatabase.SetEntry(Market.CME, Ticker, SecurityType.Future, _originalMhdbEntry.ExchangeHours, _originalMhdbEntry.DataTimeZone);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 0;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };

        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;
    }
}
