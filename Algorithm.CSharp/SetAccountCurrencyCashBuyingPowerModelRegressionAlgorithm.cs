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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test used for testing setting an account currency different than USD
    /// and trading a Security in quote currency different than account currency.
    /// Uses CashBuyingPowerModel as BuyingPowerModel.
    /// </summary>
    public class SetAccountCurrencyCashBuyingPowerModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _btcUsd;
        private Security _btcEur;
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
            SetStartDate(2018, 04, 04);  //Set Start Date
            SetEndDate(2018, 04, 04);    //Set End Date
            SetAccountCurrency("EUR");   // Change account currency
            // We have no account currency, this is useful so using SetHoldings()
            // target quantity is a reachable value since it uses Portfolio.TotalPortfolioValue
            SetCash(0);
            _initialCapital = 10000;
            SetCash("USD", _initialCapital);

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            _btcUsd = AddCrypto("BTCUSD");
            _btcEur = AddCrypto("BTCEUR");
            if (!(_btcUsd.BuyingPowerModel is CashBuyingPowerModel)
                || !(_btcEur.BuyingPowerModel is CashBuyingPowerModel))
            {
                throw new Exception("This regression algorithm is expected to test the CashBuyingPowerModel");
            }

            try
            {
                // Change account currency after adding a security will throw
                SetAccountCurrency("ARG");
                throw new Exception("Calling SetAccountCurrency() after adding a Security should throw");
            }
            catch (InvalidOperationException)
            {
                // expected
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

                    var res = Buy(_btcEur.Symbol, 1);
                    if (res.Status != OrderStatus.Invalid
                        && res.OrderEvents.First().Message.Contains("Reason: Your portfolio holds 0 EUR"))
                    {
                        throw new Exception($"We shouldn't be able to buy {_btcEur.Symbol}" +
                            " because we don't own any EUR");
                    }

                    UpdateExpectedOrderQuantity(0.5m);
                    SetHoldings(_btcUsd.Symbol, 0.5);
                    break;
                case 1:
                    _step++;
                    UpdateExpectedOrderQuantity(1);
                    SetHoldings(_btcUsd.Symbol, 1);
                    break;
                case 2:
                    _step++;
                    UpdateExpectedOrderQuantity(0);
                    SetHoldings(_btcUsd.Symbol, 0);
                    break;
                case 3:
                    // buying power model does not allow shorting, this will not work
                    _step++;
                    UpdateExpectedOrderQuantity(-0.5m);
                    SetHoldings(_btcUsd.Symbol, -0.5);
                    break;
                case 4:
                    // buying power model does not allow shorting, this will not work
                    _step++;
                    UpdateExpectedOrderQuantity(-1);
                    SetHoldings(_btcUsd.Symbol, -1);
                    break;
                case 5:
                    _step++;
                    UpdateExpectedOrderQuantity(0);
                    SetHoldings(_btcUsd.Symbol, 0);
                    break;
            }
        }

        private void UpdateExpectedOrderQuantity(decimal target)
        {
            _expectedOrderQuantity = ((Portfolio.TotalPortfolioValue - Settings.FreePortfolioValue) * target - _btcUsd.Holdings.HoldingsValue)
                / (_btcUsd.Price * _btcUsd.QuoteCurrency.ConversionRate);
            _expectedOrderQuantity--; // minus 1 per fees
            _expectedOrderQuantity -= _expectedOrderQuantity % _btcUsd.SymbolProperties.LotSize;
            _expectedOrderQuantity = _expectedOrderQuantity.Normalize();
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.CashBook["BTC"].Amount != 0)
            {
                throw new Exception($"Unexpected BTC ending cash amount: {Portfolio.CashBook["BTC"].Amount}.");
            }
            if (Portfolio.CashBook["EUR"].Amount != 0)
            {
                throw new Exception($"Unexpected EUR ending cash amount: {Portfolio.CashBook["EUR"].Amount}.");
            }

            var expectedAmount = _initialCapital
                + Portfolio.CashBook.Convert(Portfolio.TotalProfit, "EUR", "USD")
                - Portfolio.CashBook.Convert(Portfolio.TotalFees, "EUR", "USD");
            var amount = Portfolio.CashBook["USD"].Amount;
            // there could be a small difference due to conversion rates
            // leave 0.5% for error
            if (Math.Abs(expectedAmount - amount) > Math.Abs(expectedAmount) * 0.005m)
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
                if (Math.Abs(orderEvent.FillQuantity - _expectedOrderQuantity) > 1)
                {
                    throw new Exception($"Unexpected order event fill quantity: {orderEvent.FillQuantity}. " +
                        $"Expected {_expectedOrderQuantity}");
                }

                var orderFeeInAccountCurrency = Portfolio.CashBook.ConvertToAccountCurrency(orderEvent.OrderFee.Value).Amount;
                var expectedOrderFee = _btcUsd.Holdings.TotalFees - _previousHoldingsFees;

                // just to verify let calculate the order fee using taker fee
                var calculatedOrderFee = Portfolio.CashBook.ConvertToAccountCurrency(
                    orderEvent.AbsoluteFillQuantity * 0.003m * orderEvent.FillPrice,
                    orderEvent.OrderFee.Value.Currency);

                if (orderEvent.OrderFee.Value.Currency == AccountCurrency
                    // leave 0.00001m as error in expected fee value
                    || Math.Abs(expectedOrderFee - orderFeeInAccountCurrency) > 0.00001m
                    || Math.Abs(expectedOrderFee - calculatedOrderFee) > 0.00001m)
                {
                    throw new Exception($"Unexpected order fee: {orderFeeInAccountCurrency}. " +
                        $"Expected {expectedOrderFee}. Calculated Order Fee {calculatedOrderFee}");
                }

                if (!TradeBuilder.HasOpenPosition(_btcUsd.Symbol))
                {
                    var lastTrade = TradeBuilder.ClosedTrades.Last();

                    var expectedProfitLoss = (lastTrade.ExitPrice - lastTrade.EntryPrice)
                        * lastTrade.Quantity
                        * _btcUsd.QuoteCurrency.ConversionRate
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

                    if (Math.Abs(_btcUsd.Holdings.LastTradeProfit - expectedProfitLoss) > 1)
                    {
                        throw new Exception($"Unexpected Holdings.NetProfit: {_btcUsd.Holdings.LastTradeProfit}. " +
                            $"Expected {expectedProfitLoss}");
                    }
                }

                _previousHoldingsFees = _btcUsd.Holdings.TotalFees;
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.62%"},
            {"Compounding Annual Return", "-90.499%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.643%"},
            {"Sharpe Ratio", "-10.671"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-1.662"},
            {"Beta", "0.568"},
            {"Annual Standard Deviation", "0.076"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-40.011"},
            {"Tracking Error", "0.058"},
            {"Treynor Ratio", "-1.426"},
            {"Total Fees", "$48.56"}
        };
    }
}
