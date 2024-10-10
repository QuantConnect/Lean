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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base class for regression algorithms testing that when a continuous future rollover happens,
    /// the continuous contract is updated correctly with the new contract data, regardless of the
    /// offset between the exchange time zone and the data time zone.
    /// </summary>
    public abstract class ContinuousFutureRolloverBaseRegressionAlgorithm : QCAlgorithm
    {
        private Future _continuousContract;

        private DateTime _rolloverTime;

        protected abstract Resolution Resolution { get; }

        protected abstract Offset ExchangeToDataTimeZoneOffset { get; }

        private DateTimeZone DataTimeZone => TimeZones.Utc;

        private DateTimeZone ExchangeTimeZone => DateTimeZone.ForOffset(ExchangeToDataTimeZoneOffset);

        private bool RolloverHappened => _rolloverTime != DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 12, 20);

            var ticker = Futures.Indices.SP500EMini;

            var marketHours = MarketHoursDatabase.GetEntry(Market.CME, ticker, SecurityType.Future);
            var exchangeHours = new SecurityExchangeHours(ExchangeTimeZone,
                marketHours.ExchangeHours.Holidays,
                marketHours.ExchangeHours.MarketHours.ToDictionary(),
                marketHours.ExchangeHours.EarlyCloses,
                marketHours.ExchangeHours.LateOpens);
            MarketHoursDatabase.SetEntry(Market.CME, ticker, SecurityType.Future, exchangeHours, DataTimeZone);

            SetTimeZone(ExchangeTimeZone);

            _continuousContract = AddFuture(ticker,
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
            foreach (var (symbol, symbolChangedEvent) in slice.SymbolChangedEvents)
            {
                if (RolloverHappened)
                {
                    throw new RegressionTestException($"[{Time}] -- Unexpected symbol changed event for {symbol}. Expected only one mapping.");
                }

                _rolloverTime = symbolChangedEvent.EndTime;

                var oldSymbol = symbolChangedEvent.OldSymbol;
                var newSymbol = symbolChangedEvent.NewSymbol;
                Debug($"[{Time}] -- Rollover: {oldSymbol} -> {newSymbol}");

                if (symbol != _continuousContract.Symbol)
                {
                    throw new RegressionTestException($"[{Time}] -- Unexpected symbol changed event for {symbol}");
                }

                var expectedMappingDate = new DateTime(2013, 12, 18);
                if (_rolloverTime  != expectedMappingDate)
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

            if (mappedFuturePrice != 0 || !RolloverHappened)
            {
                if (continuousContractPrice != mappedFuturePrice)
                {
                    throw new RegressionTestException($"[{Time}] -- Prices do not match. " +
                        $"Expected continuous future price to be the same as the mapped contract:\n" +
                        $"   Continuous contract ({_continuousContract.Symbol}) price: {continuousContractPrice} :: {_continuousContract.GetLastData()}. \n" +
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

                if (continuousContractLastData.EndTime >= _rolloverTime)
                {
                    throw new RegressionTestException($"[{Time}] -- Expected continuous future contract last data to be from the previously " +
                        $"mapped contract until the new mapped contract gets data:\n" +
                        $"   Continuous contract ({_continuousContract.Symbol}) last data: " +
                        $"{continuousContractLastData.Time} - {continuousContractLastData.EndTime} :: {continuousContractLastData}.");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!RolloverHappened)
            {
                throw new RegressionTestException($"[{Time}] -- Rollover did not happen.");
            }
        }
    }
}
