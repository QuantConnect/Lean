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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System.Linq;
using QuantConnect.Orders;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Securities;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// 4.0 DEMONSTRATION OF CUSTOM CHARTING FLEXIBILITY:
    /// 
    /// The entire charting system of quantconnect is adaptable. You can adjust it to draw whatever you'd like.
    /// 
    /// Charts can be stacked, or overlayed on each other.
    /// Series can be candles, lines or scatter plots.
    /// 
    /// Even the default behaviours of QuantConnect can be overridden
    /// 
    /// </summary>
    /// 


    public class StopType
    {
        public const string stop = "stop";
        public const string loss = "loss";
        public const string profit = "profit";

    }
    public class stoplossorder
    {
        public OrderTicket orderTicket { get; set; }
        public string type { get; set; }
        public int masterOrderId { get; set; }
        public int stopLossOrderId { get; set; }
        public int takeProfitOrderId { get; set; }
        public decimal stopLossPrice { get; set; }
        public decimal takeProfitPrice { get; set; }
    }

    public class CustomChartingGBPJPYAlgorithm : QCAlgorithm
    {
        private const string _symbol = "GBPJPY";
        decimal riskpercent = 1;
        int recentBars = 10;
        private decimal recentBuffer = 1.0m;
        int veryRecentBars = 3;
        private decimal veryRecentBuffer = 0.05m; //yen

        private string _symbolCounterCurrency = _symbol.ToString().Substring(3);


        DateTime startDate = new DateTime(2015, 1, 1);
        DateTime endDate = new DateTime(2015, 4, 1);

        private const Resolution resolution = Resolution.Minute;

        private DateTime previous;
        private ExponentialMovingAverage fastema;
        private ExponentialMovingAverage slowema;

        public Dictionary<int, stoplossorder> orders = new Dictionary<int, stoplossorder>();


        /// Called at the start of your algorithm to setup your requirements:
        /// </summary>
        public override void Initialize()
        {
            //Set the date range you want to run your algorithm:
            SetStartDate(startDate);
            SetEndDate(endDate);
            //Set the starting cash for your strategy:
            SetCash(30000);

            //Add any stocks you'd like to analyse, and set the resolution:
            // Find more symbols here: http://quantconnect.com/data
            AddForex(_symbol, resolution);
            AddForex("usd" + _symbolCounterCurrency, resolution);

            History(recentBars, resolution);
            SetWarmUp(90);
            //Initialize indicators 
            // create a 15 day exponential moving average
            fastema = EMA(_symbol, 30, resolution);

            // create a 30 day exponential moving average
            slowema = EMA(_symbol, 90, resolution);


        }

        /// <summary>
        /// On receiving new tradebar data it will be passed into this function. The general pattern is:
        /// "public void OnData( CustomType name ) {...s"
        /// </summary>
        /// <param name="data">TradeBars data type synchronized and pushed into this function. The tradebars are grouped in a dictionary.</param>
        public void OnData(Slice slicedata)
        {
            
            if (IsWarmingUp) return;
            QuoteBars data = slicedata.QuoteBars;
            // a couple things to notice in this method:
            //  1. We never need to 'update' our indicators with the data, the engine takes care of this for us
            //  2. We can use indicators directly in math expressions
            //  3. We can easily plot many indicators at the same time

            // wait for our slow ema to fully initialize
            if (!slowema.IsReady) return;

            // only once per day
            if (previous.Date == Time.Date) return;

            // define a small tolerance on our checks to avoid bouncing
            const decimal tolerance = 0.00015m;
            var holdings = Portfolio[_symbol].Quantity;


            //up or down?
            int tradingDirection = 0;
            if ((fastema > slowema * (1 + tolerance)))
                tradingDirection = 1;
            else if ((fastema < slowema * (1 + tolerance)))
                tradingDirection = -1;


            //check stop loss

            //if flat then trade
            Log("data:" + data[_symbol].Close);
            if (Math.Abs(tradingDirection) == 1)
            {
                Log("Want to trade at :" + data[_symbol].Close);

                if (tradingDirection == 1) //want to buy
                {
                    Log("  Want to buy...");
                    if (holdings == 0)  //no problem just buy
                    {
                        //BUY
                        CancelAll();
                        StopStopLossProfitOrder(
                            units: +GetRiskNormalizedOrderSize(GetStopPrice(OrderDirection.Buy) - GetStopLossPrice(OrderDirection.Buy)),
                            loss: GetStopLossPrice(OrderDirection.Buy),
                            profit: GetTakeProfitPrice(OrderDirection.Buy),
                            stop: GetStopPrice(OrderDirection.Buy)
                            );


                        Log("    Setting BUY order, Price Now:" + data[_symbol].Close + "");

                    }
                    else if (holdings > 0) //already in a buy ...
                    {
                        Log("    but in a buy... so ignoring");
                    }
                    else if (holdings < 0) //in a short, liquidate!
                    {
                        Log("    but in a short, liquidate!");
                        CancelAll();
                    }
                }
                else if (tradingDirection == -1)
                //want to sell
                {
                    Log("  Want to sell...");
                    if (holdings == 0)  //no problem just sell
                    {
                        //SELL                        
                        CancelAll();
                        StopStopLossProfitOrder(
                            units: -GetRiskNormalizedOrderSize(GetStopLossPrice(OrderDirection.Sell) - GetStopPrice(OrderDirection.Sell)),
                            loss: GetStopLossPrice(OrderDirection.Sell),
                            profit: GetTakeProfitPrice(OrderDirection.Sell),
                            stop: GetStopPrice(OrderDirection.Sell)
                            );

                        Log("    Setting SELL order, Price Now:" + data[_symbol].Close + "");

                    }
                    else if (holdings < 0) //already in a short ...
                    {
                        Log("    but in a sell... so ignoring");


                    }
                    else if (holdings > 0) //in a Long, liquidate!
                    {
                        Log("    in a long, want to short,  liquidate!");
                        CancelAll();
                    }

                }
            }


            Plot(_symbol, "Price", data[_symbol].Close);

            // easily plot indicators, the series name will be the name of the indicator
            Plot(_symbol, fastema, slowema);
            Plot(_symbol, "VeryRecentHigh", GetVeryRecentHigh());
            Plot(_symbol, "RecentHigh", GetRecentHigh());
            Plot(_symbol, "RecentLow", GetRecentLow());
            Plot(_symbol, "VeryRecentLow", GetVeryRecentLow());


            previous = Time;

        }

        public int StopStopLossProfitOrder(int units, decimal stop, decimal loss, decimal profit)
        {
            var sm = StopMarketOrder(_symbol, units, stop, "MainEntry");

            orders[sm.OrderId] = new stoplossorder()
            {
                orderTicket = sm,
                type = StopType.stop,
                stopLossPrice = loss,
                takeProfitPrice = profit
            };


            Log("Created potential order: " + sm.OrderId + ", Q:" + units+",SLP:" + stop + "/" + loss + "/" + profit);
            return sm.OrderId;

        }

        public void CancelAll()
        {
            Log("Liquidating all");
            Liquidate();
            //foreach (var order in orders.ToList()){
            //    if (order.Value.orderTicket.CancelRequest == null)
            //    {
            //        order.Value.orderTicket.Cancel();
            //        Log("Cancelling order: " + order.Value.orderTicket.OrderId);
            //    }
            //}
        }

        public void logorder(int id)
        {
            var order = orders[id];
            Log("OrderId:" + order.orderTicket.OrderId + ",Type:" + order.type + ", Amount:" + order.orderTicket.Quantity);
        }




        public decimal GetStopPrice(OrderDirection dir)
        {

            if (dir == OrderDirection.Buy)
            {
                return Math.Round(GetVeryRecentHigh() + veryRecentBuffer, 3);
            }
            else
            {
                return Math.Round(GetVeryRecentLow() - veryRecentBuffer, 3);
            }
        }

        public decimal GetStopLossPrice(OrderDirection dir)
        {
            if (dir == OrderDirection.Buy)
            {
                return Math.Round(GetRecentLow() - recentBuffer, 3);

            }
            else
            {
                return Math.Round(GetRecentHigh() + recentBuffer, 3);
            }
        }


        public decimal GetTakeProfitPrice(OrderDirection dir)
        {
            if (dir == OrderDirection.Buy)
            {
                return Math.Round(GetRecentHigh() + recentBuffer, 3);
            }
            else
            {
                return Math.Round(GetRecentLow() - recentBuffer, 3);
            }

        }

        // not sure what unit lossAmount will be
        public int GetRiskNormalizedOrderSize(decimal lossAmountInCounterCurrency)
        {

            //we have lossAmount (in counter currency)
            //need to get this in usd
            decimal usdcounter;
            if (_symbolCounterCurrency.ToLower() == "usd")
            {
                usdcounter = 1;
            }
            else
            {
                usdcounter = Securities["usd" + _symbolCounterCurrency.ToLower()].Close;
            }

            var lossinusd = lossAmountInCounterCurrency / usdcounter;

            //risk is lossinusd per unit we decide to buy.
            //total loss allowed is
            // x percent of equity
            // USD amount to risk
            var xpercentOfEquity = Portfolio.Cash * riskpercent * 0.01m;

            //which allows us to buy a total of
            var maxUnitsToGet = xpercentOfEquity / lossinusd;

            return (int)maxUnitsToGet;
            //return (int)Math.Floor(maxUnitsToGet / 1000) * 1000;
        }


        public decimal GetRecentHigh()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, recentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Close + recentBuffer;
            return bars.Select(a => a.Close).Max() + recentBuffer;
        }

        public decimal GetRecentLow()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, recentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Close;
            return bars.Select(a => a.Close).Min() - recentBuffer;
        }

        public decimal GetVeryRecentHigh()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, veryRecentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Close + veryRecentBuffer;
            return bars.Select(a => a.Close).Max() + veryRecentBuffer;
        }

        public decimal GetVeryRecentLow()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, veryRecentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Close - veryRecentBuffer;
            return bars.Select(a => a.Close).Min() - veryRecentBuffer;
        }



        // If the StopLoss or ProfitTarget is filled, cancel the other
        // If you don't do this, then  the ProfitTarget or StopLoss order will remain outstanding
        // indefinitely, which will cause very bad behaviors in your algorithm
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // Ignore OrderEvents that are not closed

            Log("Order Event:" + orderEvent.Status + " for order " + orderEvent.OrderId);
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }


            var filledOrderId = orderEvent.OrderId;
            stoplossorder order = orders[orderEvent.OrderId];
            //if its the main order
            if (order.type == StopType.stop)
            {
                Log("  Filling Main order " + orderEvent.OrderId);
                //active the other orders:
                //if order quantity is positive we were buying
                Log("     Stop Loss @" + order.stopLossPrice + " , Q:" + -order.orderTicket.Quantity);
                Log("     Take profit @" + order.takeProfitPrice + " , Q:" + -order.orderTicket.Quantity);

                OrderTicket s;
                OrderTicket sp;
                
                s = StopMarketOrder(_symbol, -order.orderTicket.Quantity, order.stopLossPrice, "Stop Loss");
                sp = StopMarketOrder(_symbol, -order.orderTicket.Quantity, order.takeProfitPrice, "Take Profit");
                
                

                orders[s.OrderId] = new stoplossorder()
                {
                    orderTicket = s,
                    masterOrderId = orderEvent.OrderId,
                    type = StopType.loss,
                    takeProfitOrderId = sp.OrderId
                };

                orders[sp.OrderId] = new stoplossorder()
                {
                    orderTicket = sp,
                    masterOrderId = orderEvent.OrderId,
                    type = StopType.profit,
                    stopLossOrderId = s.OrderId
                };

                //update original
                orders[sp.OrderId].stopLossOrderId = s.OrderId;
                orders[sp.OrderId].takeProfitOrderId = sp.OrderId;

            }
            
            // If the ProfitTarget order was filled, close the StopLoss order
            if (order.type == StopType.profit)
            {
                Log("  Taking profit order " + orderEvent.OrderId + ", for original order " + order.masterOrderId);
                orders[order.stopLossOrderId].orderTicket.Cancel();
            }

            // If the StopLoss order was filled, close the ProfitTarget
            if (order.type == StopType.loss)
            {
                Log("  Taking loss order " + orderEvent.OrderId + ", for original order " + order.masterOrderId);
                orders[order.takeProfitOrderId].orderTicket.Cancel();
            }

        }


        //public void printOrders()
        //{
        //    foreach (var order in orders)
        //    {
        //        Log("Order:" + order.Value.masterOrderId, ", Status: " + order.Value.orderTicket.Status+ "\n\r StopLoss:" + order.Value.stopLossPrice + ",Status: "+ order.Value.stopLossOrderId)
        //    }
        //}


    }

}

