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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents the base Brokerage implementation. This provides logging on brokerage events.
    /// </summary>
    public abstract class Brokerage : IBrokerage
    {
        // 7:45 AM (New York time zone)
        private static readonly TimeSpan LiveBrokerageCashSyncTime = new TimeSpan(7, 45, 0);

        private readonly object _performCashSyncReentranceGuard = new object();
        private bool _syncedLiveBrokerageCashToday = true;
        private long _lastSyncTimeTicks = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        public event EventHandler<OrderEvent> OrderStatusChanged;

        /// <summary>
        /// Event that fires each time a short option position is assigned
        /// </summary>
        public event EventHandler<OrderEvent> OptionPositionAssigned;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        public event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Event that fires when an error is encountered in the brokerage
        /// </summary>
        public event EventHandler<BrokerageMessageEvent> Message;

        /// <summary>
        /// Gets the name of the brokerage
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Creates a new Brokerage instance with the specified name
        /// </summary>
        /// <param name="name">The name of the brokerage</param>
        protected Brokerage(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public abstract bool PlaceOrder(Order order);

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public abstract bool UpdateOrder(Order order);

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public abstract bool CancelOrder(Order order);

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Dispose of the brokerage instance
        /// </summary>
        public virtual void Dispose()
        {
            // NOP
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        protected virtual void OnOrderEvent(OrderEvent e)
        {
            try
            {
                OrderStatusChanged?.Invoke(this, e);

                if (Log.DebuggingEnabled)
                {
                    // log after calling the OrderStatusChanged event, the BrokerageTransactionHandler will set the order quantity
                    Log.Debug("Brokerage.OnOrderEvent(): " + e);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the OptionPositionAssigned event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        protected virtual void OnOptionPositionAssigned(OrderEvent e)
        {
            try
            {
                Log.Debug("Brokerage.OptionPositionAssigned(): " + e);

                OptionPositionAssigned?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the AccountChanged event
        /// </summary>
        /// <param name="e">The AccountEvent</param>
        protected virtual void OnAccountChanged(AccountEvent e)
        {
            try
            {
                Log.Trace("Brokerage.OnAccountChanged(): " + e);

                AccountChanged?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        protected virtual void OnMessage(BrokerageMessageEvent e)
        {
            try
            {
                if (e.Type == BrokerageMessageType.Error)
                {
                    Log.Error("Brokerage.OnMessage(): " + e);
                }
                else
                {
                    Log.Trace("Brokerage.OnMessage(): " + e);
                }

                Message?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public abstract List<Order> GetOpenOrders();

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public abstract List<Holding> GetAccountHoldings();

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public abstract List<CashAmount> GetCashBalance();

        /// <summary>
        /// Specifies whether the brokerage will instantly update account balances
        /// </summary>
        public virtual bool AccountInstantlyUpdated => false;

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public virtual IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            return Enumerable.Empty<BaseData>();
        }

        #region IBrokerageCashSynchronizer implementation

        /// <summary>
        /// Gets the date of the last sync (New York time zone)
        /// </summary>
        protected DateTime LastSyncDate => LastSyncDateTimeUtc.ConvertFromUtc(TimeZones.NewYork).Date;

        /// <summary>
        /// Gets the datetime of the last sync (UTC)
        /// </summary>
        public DateTime LastSyncDateTimeUtc => new DateTime(Interlocked.Read(ref _lastSyncTimeTicks));

        /// <summary>
        /// Returns whether the brokerage should perform the cash synchronization
        /// </summary>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <returns>True if the cash sync should be performed</returns>
        public virtual bool ShouldPerformCashSync(DateTime currentTimeUtc)
        {
            // every morning flip this switch back
            var currentTimeNewYork = currentTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            if (_syncedLiveBrokerageCashToday && currentTimeNewYork.Date != LastSyncDate)
            {
                _syncedLiveBrokerageCashToday = false;
            }

            return !_syncedLiveBrokerageCashToday && currentTimeNewYork.TimeOfDay >= LiveBrokerageCashSyncTime;
        }

        /// <summary>
        /// Synchronizes the cashbook with the brokerage account
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <param name="getTimeSinceLastFill">A function which returns the time elapsed since the last fill</param>
        /// <returns>True if the cash sync was performed successfully</returns>
        public virtual bool PerformCashSync(IAlgorithm algorithm, DateTime currentTimeUtc, Func<TimeSpan> getTimeSinceLastFill)
        {
            try
            {
                // prevent reentrance in this method
                if (!Monitor.TryEnter(_performCashSyncReentranceGuard))
                {
                    Log.Trace("Brokerage.PerformCashSync(): Reentrant call, cash sync not performed");
                    return false;
                }

                Log.Trace("Brokerage.PerformCashSync(): Sync cash balance");

                var balances = new List<CashAmount>();
                try
                {
                    balances = GetCashBalance();
                }
                catch (Exception err)
                {
                    Log.Error(err, "Error in GetCashBalance:");
                }

                if (balances.Count == 0)
                {
                    Log.Trace("Brokerage.PerformCashSync(): No cash balances available, cash sync not performed");
                    return false;
                }

                // Adds currency to the cashbook that the user might have deposited
                foreach (var balance in balances)
                {
                    if (!algorithm.Portfolio.CashBook.ContainsKey(balance.Currency))
                    {
                        Log.Trace($"Brokerage.PerformCashSync(): Unexpected cash found {balance.Currency} {balance.Amount}", true);
                        algorithm.Portfolio.SetCash(balance.Currency, balance.Amount, 0);
                    }
                }

                // if we were returned our balances, update everything and flip our flag as having performed sync today
                foreach (var kvp in algorithm.Portfolio.CashBook)
                {
                    var cash = kvp.Value;

                    //update the cash if the entry if found in the balances
                    var balanceCash = balances.Find(balance => balance.Currency == cash.Symbol);
                    if (balanceCash != default(CashAmount))
                    {
                        // compare in account currency
                        var delta = cash.Amount - balanceCash.Amount;
                        if (Math.Abs(algorithm.Portfolio.CashBook.ConvertToAccountCurrency(delta, cash.Symbol)) > 5)
                        {
                            // log the delta between
                            Log.Trace($"Brokerage.PerformCashSync(): {balanceCash.Currency} Delta: {delta:0.00}", true);
                        }
                        algorithm.Portfolio.CashBook[cash.Symbol].SetAmount(balanceCash.Amount);
                    }
                    else
                    {
                        //Set the cash amount to zero if cash entry not found in the balances
                        Log.Trace($"Brokerage.PerformCashSync(): {cash.Symbol} was not found in brokerage cash balance, setting the amount to 0", true);
                        algorithm.Portfolio.CashBook[cash.Symbol].SetAmount(0);
                    }
                }
                _syncedLiveBrokerageCashToday = true;
                _lastSyncTimeTicks = currentTimeUtc.Ticks;
            }
            finally
            {
                Monitor.Exit(_performCashSyncReentranceGuard);
            }

            // fire off this task to check if we've had recent fills, if we have then we'll invalidate the cash sync
            // and do it again until we're confident in it
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
                {
                    // we want to make sure this is a good value, so check for any recent fills
                    if (getTimeSinceLastFill() <= TimeSpan.FromSeconds(20))
                    {
                        // this will cause us to come back in and reset cash again until we
                        // haven't processed a fill for +- 10 seconds of the set cash time
                        _syncedLiveBrokerageCashToday = false;
                        //_failedCashSyncAttempts = 0;
                        Log.Trace("Brokerage.PerformCashSync(): Unverified cash sync - resync required.");
                    }
                    else
                    {
                        Log.Trace("Brokerage.PerformCashSync(): Verified cash sync.");

                        algorithm.Portfolio.LogMarginInformation();
                    }
                });

            return true;
        }

        #endregion

    }
}
