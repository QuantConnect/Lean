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


    public class CustomChartingGBPJPYAlgorithm : QCAlgorithm
    {
        private const string _symbol = "GBPJPY";
        decimal riskpercent = 1;
        int recentBars = 10;
        private decimal recentBuffer = 0.1m;
        int veryRecentBars = 3;
        private decimal veryRecentBuffer = 0.05m; //yen

        private string _symbolCounterCurrency = _symbol.ToString().Substring(3);
        

        DateTime startDate = new DateTime(2015, 1, 1);
        DateTime endDate = new DateTime(2016, 1, 1);

        private const Resolution resolution = Resolution.Hour;
        
        private DateTime previous;
        private ExponentialMovingAverage fastema;
        private ExponentialMovingAverage slowema;
        private SimpleMovingAverage[] ribbon;
        private OrderTicket orderticket;
        private OrderTicket _stopLossTicket;
        private OrderTicket _stopProfitTicket;
        public decimal CurrentStopLoss;
        public Dictionary<int, decimal> stoplosses = new Dictionary<int, decimal>();
        public decimal CurrentStopProfit;
        public Dictionary<int, decimal> stopProfits = new Dictionary<int, decimal>();

        /// Called at the start of your algorithm to setup your requirements:
        /// </summary>
        public override void Initialize()
        {
            //Set the date range you want to run your algorithm:
            SetStartDate(startDate);
            SetEndDate(endDate);
            //Set the starting cash for your strategy:
            SetCash(100000);

            //Add any stocks you'd like to analyse, and set the resolution:
            // Find more symbols here: http://quantconnect.com/data
            AddForex(_symbol, resolution);
            AddForex("usd" + _symbolCounterCurrency, resolution);
            
            History(recentBars, resolution);
            SetWarmUp(30);
            //Initialize indicators 
            // create a 15 day exponential moving average
            fastema = EMA(_symbol, 15, resolution);

            // create a 30 day exponential moving average
            slowema = EMA(_symbol, 30, resolution);

            int ribbonCount = 8;
            int ribbonInterval = 15;
            ribbon = Enumerable.Range(0, ribbonCount).Select(x => SMA(_symbol, (x + 1) * ribbonInterval, resolution)).ToArray();

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



            //if flat then trade

            if (Math.Abs(tradingDirection) == 1)
            {
                if (tradingDirection == 1) //want to buy
                {
                    if (holdings == 0)  //no problem just buy
                    {
                        //BUY
                        Log("BUY  >> " + Securities[_symbol].Price);
                        decimal sl = GetLossAmount(OrderDirection.Buy);
                        CurrentStopLoss = sl;
                        decimal sp = GetProfitAmount(OrderDirection.Buy);
                        CurrentStopProfit = sp;
                        orderticket = StopMarketOrder(_symbol, +GetRiskNormalizedOrderSize(sl), GetStopPrice(OrderDirection.Buy) );
                        stoplosses[orderticket.OrderId] = GetRecentLow();
                    }
                    else if (holdings > 0) //already in a buy ...
                    {

                    }
                    else if (holdings < 0) //in a short, liquidate!
                    {
                        Log("in a short, want to long, liquidate!");
                        Liquidate(_symbol);
                    }
                }
                else if (tradingDirection == -1)
                //want to sell
                {
                    if (holdings == 0)  //no problem just sell
                    {
                        //SELL
                        Log("SELL  >> " + Securities[_symbol].Price);
                        decimal sl = GetLossAmount(OrderDirection.Sell);
                        CurrentStopLoss = sl;
                        decimal sp = GetProfitAmount(OrderDirection.Buy);
                        CurrentStopProfit = sp;
                        orderticket = StopMarketOrder(_symbol, -GetRiskNormalizedOrderSize(sl), GetStopPrice(OrderDirection.Sell));
                        stoplosses[orderticket.OrderId] = GetRecentHigh();
                    }
                    else if (holdings < 0) //already in a short ...
                    {

                    }
                    else if (holdings > 0) //in a Long, liquidate!
                    {
                        Log("in a long, want to short,  liquidate!");
                        Liquidate(_symbol);
                    }

                }
            }



            Plot(_symbol, "Price", data[_symbol].Price);

            // easily plot indicators, the series name will be the name of the indicator
            Plot(_symbol, fastema, slowema);
            Plot("Ribbon", ribbon);

            previous = Time;

        }

        public decimal GetStopPrice(OrderDirection dir)
        {
            
            if (dir == OrderDirection.Buy)
            {
                return Math.Round(GetVeryRecentHigh(), 3);
            } else {
                return Math.Round(GetVeryRecentLow(), 3);
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
                usdcounter = Securities["usd" + _symbolCounterCurrency.ToLower()].Price;
            }

            var lossinusd = lossAmountInCounterCurrency/ usdcounter;

            //risk is lossinusd per unit we decide to buy.
            //total loss allowed is
            // x percent of equity
            // USD amount to risk
            var xpercentOfEquity = Portfolio.Cash * riskpercent * 0.01m;

            //which allows us to buy a total of
            var maxUnitsToGet = xpercentOfEquity / lossinusd;

            return (int)maxUnitsToGet;
        }


        public decimal GetRecentHigh()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, recentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Price  + recentBuffer;
            return bars.Select(a => a.Close).Max() + recentBuffer;
        }

        public decimal GetRecentLow()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, recentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Price - recentBuffer;
            return bars.Select(a => a.Close).Min() - recentBuffer;
        }

        public decimal GetVeryRecentHigh()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, veryRecentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Price + veryRecentBuffer;
            return bars.Select(a => a.Close).Max() + recentBuffer;
        }

        public decimal GetVeryRecentLow()
        {
            IEnumerable<QuoteBar> bars = History<QuoteBar>(_symbol, veryRecentBars, resolution);
            if (bars.Count() == 0) return Securities[_symbol].Price - veryRecentBuffer;
            return bars.Select(a => a.Close).Min() - recentBuffer;
        }


        public decimal GetLossAmount(OrderDirection direction)
        {
            decimal ret;
            if (direction == OrderDirection.Buy)
            {
                ret = Math.Abs(Securities[_symbol].Price - GetRecentLow());
            }
            else if (direction == OrderDirection.Sell)

            {
                ret = Math.Abs(Securities[_symbol].Price - GetRecentHigh());
            } else
            {
                throw new Exception("OrderDirection not specified");
            }
                return Math.Round(ret,3);
        }

        public decimal GetProfitAmount(OrderDirection direction)
        {
            decimal ret;
            if (direction == OrderDirection.Buy)
            {
                ret = Math.Abs(Securities[_symbol].Price + GetRecentHigh());
            }
            else if (direction == OrderDirection.Sell)

            {
                ret = Math.Abs(Securities[_symbol].Price - GetRecentLow());
            }
            else
            {
                throw new Exception("OrderDirection not specified");
            }
            return Math.Round(ret, 3);
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            if (fill.Status != OrderStatus.Filled)
            {
                return;
            }
            Log("OnOrderEvent  >> Stoploss " + fill.OrderId + " : " + " ^ ^ " + stoplosses);

            // if we just finished entering, place a stop loss as well
            if (Securities[_symbol].Invested)
            {
                decimal stop = CurrentStopLoss;
                if (stoplosses.ContainsKey(fill.OrderId))
                {
                    stop = stoplosses[fill.OrderId];
                }
                _stopLossTicket = StopMarketOrder(_symbol, -Securities[_symbol].Holdings.Quantity, Math.Round(stop, 3), "StopLoss at: " + stop);
            }
            // check for an exit, cancel the stop loss
            else
            {
                if (_stopLossTicket != null && _stopLossTicket.Status.IsOpen())
                {
                    // cancel our current stop loss
                    _stopLossTicket.Cancel("Exited position");
                    _stopLossTicket = null;
                }
            }





            // if we just finished entering, place a stop loss as well
            if (Securities[_symbol].Invested)
            {
                decimal profit = CurrentStopProfit;
                if (stopProfits.ContainsKey(fill.OrderId))
                {
                    profit = stopProfits[fill.OrderId];
                }
                _stopProfitTicket = StopMarketOrder(_symbol, -Securities[_symbol].Holdings.Quantity, Math.Round(profit, 3), "StopProfit at: " + profit);
            }
            // check for an exit, cancel the stop loss
            else
            {
                if (_stopProfitTicket != null && _stopProfitTicket.Status.IsOpen())
                {
                    // cancel our current stop loss
                    _stopProfitTicket.Cancel("Exited position");
                    _stopProfitTicket = null;
                }
            }



        }



    }

    

}