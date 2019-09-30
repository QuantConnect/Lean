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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Brokerages
{
    public class AlphaStreamsBrokerageModel : DefaultBrokerageModel
    {

        public AlphaStreamsBrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new ArgumentException("The Alpha Streams brokerage does not currently support Margin trading.", nameof(accountType));
            }
        }

        private readonly Type[] _supportedTimeInForces =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce),
            typeof(GoodTilDateTimeInForce)
        };

        public override IFeeModel GetFeeModel(Security security)
        {
            return new AlphaStreamsFeeModel();
        }

        public override ISlippageModel GetSlippageModel(Security security)
        {
            return new AlphaStreamsSlippageModel();
        }

        /// <summary>
        /// Force equities to be restricted to 1x leverage
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security) => 1m;

        /// <summary>
        /// Force delayed settlement of equities until end of competition (Jan 2020)
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override ISettlementModel GetSettlementModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Equity:
                    return new DelayedSettlementModel(Equity.DefaultSettlementDays, Equity.DefaultSettlementTime);

                default:
                    return new ImmediateSettlementModel();
            }
        }

        /// <summary>
        /// Force the buying power for equities to be cash -- this will be modified after the compeition (Jan 2020) when a full model is built
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security) => new CashBuyingPowerModel();

        /// <summary>
        /// Inherit from Interactive Brokers but with changes to reflect Alpha Stream compatibility (no Futures or Options)
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            // validate security type
            if (security.Type != SecurityType.Equity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(AlphaStreamsBrokerageModel)} does not support {security.Type} security type.")
                );

                return false;
            }

            // validate order type
            if (!security.Invested && order.Direction == OrderDirection.Sell)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(AlphaStreamsBrokerageModel)} does not support short positions.")
                );

                return false;
            }

            return true;
        }
    }
}
