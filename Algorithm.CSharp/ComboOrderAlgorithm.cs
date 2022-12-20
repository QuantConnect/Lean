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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// TODO:
    /// </summary>
    public abstract class ComboOrderAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected Symbol _optionSymbol;

        private List<OrderEvent> _fillOrderEvents = new();

        private bool _orderPlaced;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(10000);

            var equity = AddEquity("GOOG", leverage: 4, fillDataForward: true);
            var option = AddOption(equity.Symbol, fillDataForward: true);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                  .Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (!_orderPlaced)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                        .GroupBy(x => x.Expiry)
                        .OrderBy(grouping => grouping.Key)
                        .First()
                        .OrderBy(x => x.Strike)
                        .ToList();

                    // Let's wait until we have at least three contracts
                    if (callContracts.Count < 3)
                    {
                        return;
                    }

                    var legs = new List<Leg>()
                    {
                        new Leg() { Symbol = callContracts[0].Symbol, Quantity = 1, OrderPrice = 16.7m },
                        new Leg() { Symbol = callContracts[1].Symbol, Quantity = -2, OrderPrice  = 14.6m },
                        new Leg() { Symbol = callContracts[2].Symbol, Quantity = 1, OrderPrice = 14.0m},
                    };
                    PlaceComboOrder(legs, 10, 45m);

                    _orderPlaced = true;
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($" Order Event: {orderEvent}");

            if (orderEvent.Status == OrderStatus.Filled)
            {
                _fillOrderEvents.Add(orderEvent);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_fillOrderEvents.Count != 3)
            {
                throw new Exception($"Expected 3 fill order events, found {_fillOrderEvents.Count}");
            }

            var fillTimes = _fillOrderEvents.Select(x => x.UtcTime).ToHashSet();
            if (fillTimes.Count != 1)
            {
                throw new Exception($"Expected all fill order events to have the same time, found {string.Join(", ", fillTimes)}");
            }
        }

        protected abstract IEnumerable<OrderTicket> PlaceComboOrder(List<Leg> legs, int quantity, decimal? limitPrice = null);

        public abstract bool CanRunLocally { get; }

        public abstract Language[] Languages { get; }

        public abstract long DataPoints { get; }

        public abstract int AlgorithmHistoryDataPoints { get; }

        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}
