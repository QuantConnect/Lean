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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates how to submit orders to a Financial Advisor account group, allocation profile or a single managed account.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="financial advisor" />
    public class FinancialAdvisorDemoAlgorithm : QCAlgorithm
    {
        private const string GroupName = "TestGroupEQ";
        private static readonly TimeSpan ReconcileRetryInterval =
            TimeSpan.FromSeconds(5);

        private Symbol _symbol;
        private BrokerageAccountSnapshot _preOrderSnapshot;
        private bool _initialSnapshotRefreshAccepted;
        private bool _groupOrderSubmitted;
        private int _groupOrderId;
        private long _pendingReconcileGeneration = -1;
        private DateTime _pendingReconcileTerminalUtc;
        private DateTime _nextReconcileRefreshUtc;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            _symbol = AddEquity("SPY").Symbol;

            // The default order properties can be set here to choose the FA settings
            // to be automatically used in any order submission method (such as SetHoldings, Buy, Sell and Order)

            // Use a default FA Account Group with an Allocation Method
            DefaultOrderProperties = new InteractiveBrokersOrderProperties
            {
                // account group created manually in IB/TWS
                FaGroup = GroupName,
                // supported allocation methods are: EqualQuantity, NetLiq, AvailableEquity, PctChange
                FaMethod = "EqualQuantity"
            };

            // set a default FA Allocation Profile
            //DefaultOrderProperties = new InteractiveBrokersOrderProperties
            //{
            //    // allocation profile created manually in IB/TWS
            //    FaProfile = "TestProfileP"
            //};

            // send all orders to a single managed account
            //DefaultOrderProperties = new InteractiveBrokersOrderProperties
            //{
            //    // a sub-account linked to the Financial Advisor master account
            //    Account = "DU123456"
            //};

            if (LiveMode)
            {
                _initialSnapshotRefreshAccepted =
                    RequestBrokerageAccountSnapshotRefresh(new[] { GroupName });
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!LiveMode)
            {
                if (!Portfolio.Invested)
                {
                    // when logged into IB as a Financial Advisor, this call will use order properties
                    // set in the DefaultOrderProperties property of QCAlgorithm
                    SetHoldings(_symbol, 1);
                }
                return;
            }

            var snapshot = BrokerageAccountSnapshot;
            if (_pendingReconcileGeneration >= 0)
            {
                if (snapshot.IsReady &&
                    snapshot.Generation > _pendingReconcileGeneration &&
                    snapshot.CollectionStartedUtc >= _pendingReconcileTerminalUtc)
                {
                    ReconcileAccountPositions(snapshot);
                    _preOrderSnapshot = null;
                    _pendingReconcileGeneration = -1;
                }
                else
                {
                    TryRequestReconcileRefresh(snapshot);
                }
                return;
            }

            if (!_initialSnapshotRefreshAccepted)
            {
                _initialSnapshotRefreshAccepted =
                    RequestBrokerageAccountSnapshotRefresh(new[] { GroupName });
                return;
            }

            if (_groupOrderSubmitted ||
                !snapshot.IsReady ||
                !snapshot.Groups.ContainsKey(GroupName))
            {
                return;
            }

            _preOrderSnapshot = snapshot;
            var ticket = SetHoldings(
                _symbol,
                1,
                asynchronous: true).FirstOrDefault();
            if (ticket == null)
            {
                _preOrderSnapshot = null;
                return;
            }

            _groupOrderId = ticket.OrderId;
            _groupOrderSubmitted = true;
        }

        /// <summary>
        /// Requests authoritative account reconciliation after the parent group order becomes terminal.
        /// </summary>
        /// <param name="orderEvent">The aggregate parent order event</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!LiveMode ||
                orderEvent.OrderId != _groupOrderId ||
                !orderEvent.Status.IsClosed() ||
                _pendingReconcileGeneration >= 0)
            {
                return;
            }

            _groupOrderId = 0;
            _pendingReconcileGeneration = BrokerageAccountSnapshot.Generation;
            _pendingReconcileTerminalUtc = orderEvent.UtcTime;
            _nextReconcileRefreshUtc = UtcTime;
            TryRequestReconcileRefresh(BrokerageAccountSnapshot);
        }

        private void TryRequestReconcileRefresh(BrokerageAccountSnapshot snapshot)
        {
            if (snapshot.Status == BrokerageAccountSnapshotStatus.Refreshing ||
                UtcTime < _nextReconcileRefreshUtc)
            {
                return;
            }

            _nextReconcileRefreshUtc = UtcTime + ReconcileRetryInterval;
            if (!RequestBrokerageAccountSnapshotRefresh(new[] { GroupName }))
            {
                Error(
                    $"The post-order snapshot refresh for Financial Advisor group " +
                    $"'{GroupName}' was not accepted.");
            }
        }

        private void ReconcileAccountPositions(
            BrokerageAccountSnapshot currentSnapshot)
        {
            if (_preOrderSnapshot == null ||
                !_preOrderSnapshot.Groups.TryGetValue(GroupName, out var group))
            {
                Error(
                    $"The pre-order snapshot for Financial Advisor group " +
                    $"'{GroupName}' is unavailable.");
                return;
            }

            foreach (var accountId in group.AccountIds)
            {
                if (!_preOrderSnapshot.Accounts.TryGetValue(
                        accountId,
                        out var previousAccount) ||
                    !currentSnapshot.Accounts.TryGetValue(
                        accountId,
                        out var currentAccount))
                {
                    Error(
                        $"Account state for '{accountId}' is unavailable during " +
                        $"Financial Advisor group reconciliation.");
                    continue;
                }

                var previousQuantity = GetPositionQuantity(previousAccount);
                var currentQuantity = GetPositionQuantity(currentAccount);
                Log(
                    $"FA reconciliation: account={accountId}, symbol={_symbol.Value}, " +
                    $"before={previousQuantity}, after={currentQuantity}, " +
                    $"change={currentQuantity - previousQuantity}");
            }
        }

        private decimal GetPositionQuantity(BrokerageAccountState account)
        {
            return account.Positions
                .Where(position => position.Symbol == _symbol)
                .Sum(position => position.Quantity);
        }
    }
}
