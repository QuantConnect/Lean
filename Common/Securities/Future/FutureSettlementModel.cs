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
                var quantityClosed = Math.Sign(-fill.FillQuantity) * absoluteQuantityClosed;

                var absoluteQuantityClosedSettled = Math.Min(absoluteQuantityClosed, Math.Abs(_settledFutureQuantity));
                var quantityClosedSettled = Math.Sign(-fill.FillQuantity) * absoluteQuantityClosedSettled;

                // here we use the last settlement price we've used to calculate the trade unsettled funds (daily P&L we should apply)
                var settledContractsTodaysProfit = futureHolding.TotalCloseProfit(includeFees: false, exitPrice: fill.FillPrice, entryPrice: _settlementPrice, quantity: quantityClosedSettled);
                var unsettledContractsTodaysProfit = 0m;
                if (quantityClosedSettled != quantityClosed)
                {
                    // if we fall into any of these cases, it means the position closed was increased today before closing which means the
                    // profit of the increased quantity is not related to the settlement price because it happens after the last settlement
                    unsettledContractsTodaysProfit = applyFundsParameters.CashAmount.Amount - futureHolding.SettledProfit - settledContractsTodaysProfit;
                }

                applyFundsParameters.CashAmount = new CashAmount(settledContractsTodaysProfit + unsettledContractsTodaysProfit, applyFundsParameters.CashAmount.Currency);

                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"FutureSettlementModel.ApplyFunds({security.Symbol}): {security.LocalTime} QuantityClosed: {quantityClosed} Settled: {_settledFutureQuantity} Applying: {applyFundsParameters.CashAmount.Amount}");
                }

                // reduce our settled future quantity proportionally too
                var factor = quantityClosedSettled / _settledFutureQuantity;
                _settledFutureQuantity -= quantityClosedSettled;

                futureHolding.SettledProfit -= factor * futureHolding.SettledProfit;
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
