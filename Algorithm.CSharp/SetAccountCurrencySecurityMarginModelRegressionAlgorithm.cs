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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test used for testing setting an account currency different than USD
    /// and trading a Security in quote currency different than account currency.
    /// Uses SecurityMarginModel as BuyingPowerModel.
    /// </summary>
    public class SetAccountCurrencySecurityMarginModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _spy;
        private int _step;
        private decimal _expectedOrderQuantity;
        private decimal _previousHoldingsFees;
        private int _previousClosedTradesCount;
        private decimal _initialCapital;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 15);    //Set End Date
            SetAccountCurrency("EUR");   // Change account currency
            _initialCapital = Portfolio.CashBook["EUR"].Amount;

            _spy = AddEquity("SPY", Resolution.Daily);
            if (!(_spy.BuyingPowerModel is SecurityMarginModel))
            {
                throw new Exception("This regression algorithm is expected to test the SecurityMarginModel");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            Log($"OnData(): Current execution step: {_step}");
            switch (_step)
            {
                case 0:
                    _step++;
                    UpdateExpectedOrderQuantity(0.5m);
                    SetHoldings(_spy.Symbol, 0.5);
                    break;
                case 1:
                    _step++;
                    UpdateExpectedOrderQuantity(1);
                    SetHoldings(_spy.Symbol, 1);
                    break;
                case 2:
                    _step++;
                    UpdateExpectedOrderQuantity(0);
                    SetHoldings(_spy.Symbol, 0);
                    break;
                case 3:
                    _step++;
                    UpdateExpectedOrderQuantity(-0.5m);
                    SetHoldings(_spy.Symbol, -0.5);
                    break;
                case 4:
                    _step++;
                    UpdateExpectedOrderQuantity(-1);
                    SetHoldings(_spy.Symbol, -1);
                    break;
                case 5:
                    _step++;
                    UpdateExpectedOrderQuantity(0);
                    SetHoldings(_spy.Symbol, 0);
                    break;
            }
        }

        private void UpdateExpectedOrderQuantity(decimal target)
        {
            _expectedOrderQuantity = (Portfolio.TotalPortfolioValueLessFreeBuffer * target - _spy.Holdings.HoldingsValue)
                / (_spy.Price * _spy.QuoteCurrency.ConversionRate);
            _expectedOrderQuantity--; // minus 1 per fees
            _expectedOrderQuantity -= _expectedOrderQuantity % _spy.SymbolProperties.LotSize;
            _expectedOrderQuantity = _expectedOrderQuantity.Normalize();
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.CashBook["EUR"].Amount != _initialCapital)
            {
                throw new Exception($"Unexpected EUR ending cash amount: {Portfolio.CashBook["EUR"].Amount}.");
            }
            var expectedAmount = Portfolio.CashBook.Convert(Portfolio.TotalProfit, "EUR", "USD")
                - Portfolio.CashBook.Convert(Portfolio.TotalFees, "EUR", "USD");
            var amount = Portfolio.CashBook["USD"].Amount;
            // there could be a small difference due to conversion rates
            // leave 1% for error
            if (Math.Abs(expectedAmount - amount) > Math.Abs(expectedAmount) * 0.01m)
            {
                throw new Exception($"Unexpected USD ending cash amount: {amount}. Expected {expectedAmount}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"OnOrderEvent(): New filled order event: {orderEvent}");
                // leave 1 unit as error in expected value
                if (Math.Abs(orderEvent.FillQuantity - _expectedOrderQuantity) > 2)
                {
                    throw new Exception($"Unexpected order event fill quantity: {orderEvent.FillQuantity}. " +
                        $"Expected {_expectedOrderQuantity}");
                }

                var orderFeeInAccountCurrency = Portfolio.CashBook.ConvertToAccountCurrency(orderEvent.OrderFee.Value).Amount;
                var expectedOrderFee = _spy.Holdings.TotalFees - _previousHoldingsFees;
                if (orderEvent.OrderFee.Value.Currency == AccountCurrency
                    // leave 0.00001m as error in expected fee value
                    || Math.Abs(expectedOrderFee - orderFeeInAccountCurrency) > 0.00001m)
                {
                    throw new Exception($"Unexpected order fee: {orderFeeInAccountCurrency}. " +
                        $"Expected {expectedOrderFee}");
                }

                if (!TradeBuilder.HasOpenPosition(_spy.Symbol))
                {
                    var lastTrade = TradeBuilder.ClosedTrades.Last();

                    var expectedProfitLoss = (lastTrade.ExitPrice - lastTrade.EntryPrice)
                        * lastTrade.Quantity
                        * _spy.QuoteCurrency.ConversionRate
                        * (lastTrade.Direction == TradeDirection.Long ? 1 : -1);

                    if (Math.Abs(expectedProfitLoss - lastTrade.ProfitLoss) > 1)
                    {
                        throw new Exception($"Unexpected last trade ProfitLoss: {lastTrade.ProfitLoss}. " +
                            $"Expected {expectedProfitLoss}");
                    }

                    // There is a difference in what does Holdings and TradeBuilder consider LastTrade
                    if (TradeBuilder.ClosedTrades.Count - _previousClosedTradesCount > 1)
                    {
                        var trade = TradeBuilder.ClosedTrades[_previousClosedTradesCount];
                        expectedProfitLoss += trade.ProfitLoss;
                    }

                    if (Math.Abs(_spy.Holdings.LastTradeProfit - expectedProfitLoss) > 1)
                    {
                        throw new Exception($"Unexpected Holdings.NetProfit: {_spy.Holdings.LastTradeProfit}. " +
                            $"Expected {expectedProfitLoss}");
                    }
                }

                _previousHoldingsFees = _spy.Holdings.TotalFees;
                _previousClosedTradesCount = TradeBuilder.ClosedTrades.Count;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 73;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0.41%"},
            {"Average Loss", "-0.85%"},
            {"Compounding Annual Return", "-15.350%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "-0.260"},
            {"Start Equity", "100000"},
            {"End Equity", "99552.05"},
            {"Net Profit", "-0.448%"},
            {"Sharpe Ratio", "-1.457"},
            {"Sortino Ratio", "-2.62"},
            {"Probabilistic Sharpe Ratio", "33.743%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.48"},
            {"Alpha", "-0.349"},
            {"Beta", "0.34"},
            {"Annual Standard Deviation", "0.084"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "-6.6"},
            {"Tracking Error", "0.119"},
            {"Treynor Ratio", "-0.361"},
            {"Total Fees", "€13.73"},
            {"Estimated Strategy Capacity", "€310000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "40.08%"},
            {"OrderListHash", "3928dff130532ed7e110a40d447c71ad"}
        };
    }
}
