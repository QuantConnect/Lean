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
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// The demonstration algorithm shows some of the most common order methods when working with Crypto assets.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BasicTemplateCryptoAlgorithm : QCAlgorithm
    {
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4); // Set Start Date
            SetEndDate(2018, 4, 4); // Set End Date

            // Although typically real brokerages as GDAX only support a single account currency,
            // here we add both USD and EUR to demonstrate how to handle non-USD account currencies.
            // Set Strategy Cash (USD)
            SetCash(10000);

            // Set Strategy Cash (EUR)
            // EUR/USD conversion rate will be updated dynamically
            SetCash("EUR", 10000, 1.23m);

            // Add some coins as initial holdings
            // When connected to a real brokerage, the amount specified in SetCash
            // will be replaced with the amount in your actual account.
            SetCash("BTC", 1m, 7300m);
            SetCash("ETH", 5m, 400m);

            // Note: the conversion rates above are required in backtesting (for now) because of this issue:
            // https://github.com/QuantConnect/Lean/issues/1859

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            // You can uncomment the following line when live trading with GDAX,
            // to ensure limit orders will only be posted to the order book and never executed as a taker (incurring fees).
            // Please note this statement has no effect in backtesting or paper trading.
            // DefaultOrderProperties = new GDAXOrderProperties { PostOnly = true };

            // Find more symbols here: http://quantconnect.com/data
            AddCrypto("BTCUSD");
            AddCrypto("ETHUSD");
            AddCrypto("BTCEUR");
            var symbol = AddCrypto("LTCUSD").Symbol;

            // create two moving averages
            _fast = EMA(symbol, 30, Resolution.Minute);
            _slow = EMA(symbol, 60, Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // Note: all limit orders in this algorithm will be paying taker fees,
            // they shouldn't, but they do (for now) because of this issue:
            // https://github.com/QuantConnect/Lean/issues/1852

            if (Time.Hour == 1 && Time.Minute == 0)
            {
                // Sell all ETH holdings with a limit order at 1% above the current price
                var limitPrice = Math.Round(Securities["ETHUSD"].Price * 1.01m, 2);
                var quantity = Portfolio.CashBook["ETH"].Amount;
                LimitOrder("ETHUSD", -quantity, limitPrice);
            }
            else if (Time.Hour == 2 && Time.Minute == 0)
            {
                // Submit a buy limit order for BTC at 5% below the current price
                var usdTotal = Portfolio.CashBook["USD"].Amount;
                var limitPrice = Math.Round(Securities["BTCUSD"].Price * 0.95m, 2);
                // use only half of our total USD
                var quantity = usdTotal * 0.5m / limitPrice;
                LimitOrder("BTCUSD", quantity, limitPrice);
            }
            else if (Time.Hour == 2 && Time.Minute == 1)
            {
                // Get current USD available, subtracting amount reserved for buy open orders
                var usdTotal = Portfolio.CashBook["USD"].Amount;
                var usdReserved = Transactions.GetOpenOrders(x => x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit)
                    .Where(x => x.Symbol == "BTCUSD" || x.Symbol == "ETHUSD")
                    .Sum(x => x.Quantity * ((LimitOrder) x).LimitPrice);
                var usdAvailable = usdTotal - usdReserved;

                // Submit a marketable buy limit order for ETH at 1% above the current price
                var limitPrice = Math.Round(Securities["ETHUSD"].Price * 1.01m, 2);

                // use all of our available USD
                var quantity = usdAvailable / limitPrice;

                // this order will be rejected (for now) because of this issue:
                // https://github.com/QuantConnect/Lean/issues/1852
                LimitOrder("ETHUSD", quantity, limitPrice);

                // use only half of our available USD
                quantity = usdAvailable * 0.5m / limitPrice;
                LimitOrder("ETHUSD", quantity, limitPrice);
            }
            else if (Time.Hour == 11 && Time.Minute == 0)
            {
                // Liquidate our BTC holdings (including the initial holding)
                SetHoldings("BTCUSD", 0m);
            }
            else if (Time.Hour == 12 && Time.Minute == 0)
            {
                // Submit a market buy order for 1 BTC using EUR
                Buy("BTCEUR", 1m);

                // Submit a sell limit order at 10% above market price
                var limitPrice = Math.Round(Securities["BTCEUR"].Price * 1.1m, 2);
                LimitOrder("BTCEUR", -1, limitPrice);
            }
            else if (Time.Hour == 13 && Time.Minute == 0)
            {
                // Cancel the limit order if not filled
                Transactions.CancelOpenOrders("BTCEUR");
            }
            else if (Time.Hour > 13)
            {
                // To include any initial holdings, we read the LTC amount from the cashbook
                // instead of using Portfolio["LTCUSD"].Quantity

                if (_fast > _slow)
                {
                    if (Portfolio.CashBook["LTC"].Amount == 0)
                    {
                        Buy("LTCUSD", 10);
                    }
                }
                else
                {
                    if (Portfolio.CashBook["LTC"].Amount > 0)
                    {
                        // The following two statements currently behave differently if we have initial holdings:
                        // https://github.com/QuantConnect/Lean/issues/1860

                        Liquidate("LTCUSD");
                        // SetHoldings("LTCUSD", 0);
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");
        }
    }
}