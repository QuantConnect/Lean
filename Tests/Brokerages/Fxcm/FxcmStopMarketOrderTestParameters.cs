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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages.Fxcm
{
    public class FxcmStopMarketOrderTestParameters : StopMarketOrderTestParameters
    {
        public FxcmStopMarketOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit)
            : base(symbol, highLimit, lowLimit)
        {
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            // FXCM Buy StopMarket orders will be rejected if the stop price is below the market price
            // FXCM Sell StopMarket orders will be rejected if the stop price is above the market price

            var stop = (StopMarketOrder)order;
            var previousStop = stop.StopPrice;

            var fxcmBrokerage = (FxcmBrokerage)brokerage;
            var quotes = fxcmBrokerage.GetBidAndAsk(new List<string> { new FxcmSymbolMapper().GetBrokerageSymbol(order.Symbol) });
            
            if (order.Quantity > 0)
            {
                // for stop buys we need to decrease the stop price
                // buy stop price must be strictly above ask price
                var askPrice = Convert.ToDecimal(quotes.Single().AskPrice);
                Log.Trace("FxcmStopMarketOrderTestParameters.ModifyOrderToFill(): Ask: " + askPrice);
                stop.StopPrice = Math.Min(previousStop, Math.Max(askPrice, stop.StopPrice / 2) + 0.00001m);
            }
            else
            {
                // for stop sells we need to increase the stop price
                // sell stop price must be strictly below bid price
                var bidPrice = Convert.ToDecimal(quotes.Single().BidPrice);
                Log.Trace("FxcmStopMarketOrderTestParameters.ModifyOrderToFill(): Bid: " + bidPrice);
                stop.StopPrice = Math.Max(previousStop, Math.Min(bidPrice, stop.StopPrice * 2) - 0.00001m);
            }

            return stop.StopPrice != previousStop;
        }
    }
}
