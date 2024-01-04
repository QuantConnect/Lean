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
using System.Linq;
using System.Collections.Generic;

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that combo orders are filled correctly and at the same time
    /// </summary>
    public abstract class ComboOrderAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        private List<OrderTicket> Tickets { get; set; }
        private bool _updated;

        protected List<OrderEvent> FillOrderEvents { get; private set; } = new();

        protected List<Leg> OrderLegs { get; private set; }

        protected int ComboOrderQuantity { get; } = 10;

        protected virtual int ExpectedFillCount
        {
            get
            {
                return OrderLegs.Count;
            }
        }

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            var equity = AddEquity("GOOG", leverage: 4, fillForward: true);
            var option = AddOption(equity.Symbol, fillForward: true);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                  .Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (OrderLegs == null)
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

                    OrderLegs = new List<Leg>()
                    {
                        Leg.Create(callContracts[0].Symbol, 1, 16.7m),
                        Leg.Create(callContracts[1].Symbol, -2, 14.6m),
                        Leg.Create(callContracts[2].Symbol, 1, 14.0m)
                    };
                    Tickets = PlaceComboOrder(OrderLegs, ComboOrderQuantity, 1.9m).ToList();
                }
            }
            // Let's test order updates
            else if (Tickets.All(ticket => ticket.OrderType != OrderType.ComboMarket) && FillOrderEvents.Count == 0 && !_updated)
            {
                UpdateComboOrder(Tickets);
                _updated = true;
            }
        }

        protected virtual void UpdateComboOrder(List<OrderTicket> tickets)
        {
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($" Order Event: {orderEvent}");

            if (orderEvent.Status == OrderStatus.Filled)
            {
                FillOrderEvents.Add(orderEvent);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (OrderLegs == null)
            {
                throw new Exception("Combo order legs were not initialized");
            }

            if (Tickets.All(ticket => ticket.OrderType != OrderType.ComboMarket) && !_updated)
            {
                throw new Exception("Combo order was not updated");
            }

            if (FillOrderEvents.Count != ExpectedFillCount)
            {
                throw new Exception($"Expected {ExpectedFillCount} fill order events, found {FillOrderEvents.Count}");
            }

            var fillTimes = FillOrderEvents.Select(x => x.UtcTime).ToHashSet();
            if (fillTimes.Count != 1)
            {
                throw new Exception($"Expected all fill order events to have the same time, found {string.Join(", ", fillTimes)}");
            }

            if (FillOrderEvents.Zip(OrderLegs).Any(x => x.First.FillQuantity != x.Second.Quantity * ComboOrderQuantity))
            {
                throw new Exception("Fill quantity does not match expected quantity for at least one order leg." +
                    $"Expected: {string.Join(", ", OrderLegs.Select(x => x.Quantity * ComboOrderQuantity))}. " +
                    $"Actual: {string.Join(", ", FillOrderEvents.Select(x => x.FillQuantity))}");
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
