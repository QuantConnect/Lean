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
using QuantConnect.Logging;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Settlement model which can handle daily profit and loss settlement
    /// </summary>
    public class FutureSettlementModel : ImmediateSettlementModel
    {
        private DateTime _lastSettlementDate;
        private decimal _settledFutureQuantity;
        private decimal _settlementPrice;

        /// <summary>
        /// Applies unsettledContractsTodaysProfit settlement rules
        /// </summary>
        /// <param name="applyFundsParameters">The funds application parameters</param>
        public override void ApplyFunds(ApplyFundsSettlementModelParameters applyFundsParameters)
        {
            if(_settledFutureQuantity != 0)
            {
                var fill = applyFundsParameters.Fill;
                var security = applyFundsParameters.Security;
                var futureHolding = (FutureHolding)security.Holdings;

                var absoluteQuantityClosed = Math.Min(fill.AbsoluteFillQuantity, security.Holdings.AbsoluteQuantity);

                var absoluteQuantityClosedSettled = Math.Min(absoluteQuantityClosed, Math.Abs(_settledFutureQuantity));
                var quantityClosedSettled = Math.Sign(-fill.FillQuantity) * absoluteQuantityClosedSettled;

                // reduce our settled future quantity proportionally too
                var factor = quantityClosedSettled / _settledFutureQuantity;
                _settledFutureQuantity -= quantityClosedSettled;

                // the passed in cash amount will hold the complete profit/loss of the trade, so we need to substract the settled profit we were given or taken from
                var removedSettledProfit = factor * futureHolding.SettledProfit;
                futureHolding.SettledProfit -= removedSettledProfit;

                applyFundsParameters.CashAmount = new CashAmount(applyFundsParameters.CashAmount.Amount - removedSettledProfit, applyFundsParameters.CashAmount.Currency);
            }

            base.ApplyFunds(applyFundsParameters);
        }

        /// <summary>
        /// Scan for pending settlements
        /// </summary>
        /// <param name="settlementParameters">The settlement parameters</param>
        public override void Scan(ScanSettlementModelParameters settlementParameters)
        {
            var security = settlementParameters.Security;

            // In the futures markets, losers pay winners every day. So once a day after the settlement time has passed we will update the cash book to reflect this
            if (_lastSettlementDate.Date < security.LocalTime.Date)
            {
                if ((_lastSettlementDate != default) && security.Invested)
                {
                    var futureHolding = (FutureHolding)security.Holdings;
                    var futureCache = (FutureCache)security.Cache;
                    _settlementPrice = futureCache.SettlementPrice;
                    _settledFutureQuantity = security.Holdings.Quantity;

                    // We settled the daily P&L, losers pay winners
                    var dailyProfitLoss = futureHolding.TotalCloseProfit(includeFees: false, exitPrice: _settlementPrice) - futureHolding.SettledProfit;
                    if (dailyProfitLoss != 0)
                    {
                        futureHolding.SettledProfit += dailyProfitLoss;

                        settlementParameters.Portfolio.CashBook[security.QuoteCurrency.Symbol].AddAmount(dailyProfitLoss);
                        Log.Trace($"FutureSettlementModel.Scan({security.Symbol}): {security.LocalTime} Daily P&L: {dailyProfitLoss} " +
                            $"Quantity: {_settledFutureQuantity} Settlement: {_settlementPrice} UnrealizedProfit: {futureHolding.UnrealizedProfit}");
                    }
                }
                _lastSettlementDate = security.LocalTime.Date;
            }
        }

        /// <summary>
        /// Set the current datetime in terms of the exchange's local time zone
        /// </summary>
        /// <param name="newLocalTime">Current local time</param>
        public void SetLocalDateTimeFrontier(DateTime newLocalTime)
        {
            _lastSettlementDate = newLocalTime.Date;
        }
    }
}
