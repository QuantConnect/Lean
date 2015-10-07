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
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages.Fxcm
{
    public class FxcmLimitOrderTestParameters : LimitOrderTestParameters
    {
        private readonly FxcmBrokerageTests _tests;

        public FxcmLimitOrderTestParameters(FxcmBrokerageTests tests, string symbol, SecurityType securityType, decimal highLimit, decimal lowLimit)
            : base(symbol, securityType, highLimit, lowLimit)
        {
            _tests = tests;
        }

        public override bool ModifyOrderToFill(Order order, decimal lastMarketPrice)
        {
            // FXCM Buy Limit orders will be rejected if the limit price is above the market price
            // FXCM Sell Limit orders will be rejected if the limit price is below the market price

            var limit = (LimitOrder)order;
            var previousLimit = limit.LimitPrice;

            var brokerage = (FxcmBrokerage)_tests.Brokerage;
            var quotes = brokerage.GetQuotes(new List<string> { brokerage.ConvertSymbolToFxcmSymbol(order.Symbol) });

            if (order.Quantity > 0)
            {
                // for limit buys we need to increase the limit price
                // buy limit price must be at bid price or below
                var bidPrice = Convert.ToDecimal(quotes.Single().getBidClose());
                Log.Trace("FxcmLimitOrderTestParameters.ModifyOrderToFill(): Bid: " + bidPrice);
                limit.LimitPrice = Math.Min(bidPrice, limit.LimitPrice * 2);
            }
            else
            {
                // for limit sells we need to decrease the limit price
                // sell limit price must be at ask price or above
                var askPrice = Convert.ToDecimal(quotes.Single().getAskClose());
                Log.Trace("FxcmLimitOrderTestParameters.ModifyOrderToFill(): Ask: " + askPrice);
                limit.LimitPrice = Math.Max(askPrice, limit.LimitPrice / 2);
            }

            return limit.LimitPrice != previousLimit;
        }
    }
}
