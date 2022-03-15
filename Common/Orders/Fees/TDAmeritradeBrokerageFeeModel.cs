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
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the TD Ameritrade implementation of <see cref="IFeeModel"/>
    /// </summary>
    public class TDAmeritradeBrokerageFeeModel : FeeModel
    {
        private readonly decimal _optionFeeRate = 0.65m;
        private readonly decimal _futuresFeeRate = 2.25m + 1 + 0.02m; //(plus exchange & regulatory fees)

        /// <summary>
        /// Initializes a new instance of the <see cref="TDAmeritradeBrokerageFeeModel"/>
        /// </summary>
        public TDAmeritradeBrokerageFeeModel()
        {
        }

        /// <summary>
        /// Gets the order fee associated with the specified order. This returns the cost
        /// of the transaction in the account currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var order = parameters.Order;
            var security = parameters.Security;

            // Option exercise for equity options is free of charge
            if (order.Type == OrderType.OptionExercise)
            {
                var optionOrder = (OptionExerciseOrder)order;

                if (optionOrder.Symbol.ID.SecurityType == SecurityType.Option)
                {
                    return OrderFee.Zero;
                }
            }

            decimal feeResult = 0m;
            string feeCurrency = Currencies.USD;
            var market = security.Symbol.ID.Market;
            switch (security.Type)
            {
                case SecurityType.Option:
                case SecurityType.IndexOption:

                    // applying commission to the order
                    feeResult = order.AbsoluteQuantity * _optionFeeRate;
                    feeCurrency = Currencies.USD;
                    break;

                case SecurityType.Future:
                case SecurityType.FutureOption:
                    // applying commission to the order
                    feeResult = order.AbsoluteQuantity * _futuresFeeRate;
                    feeCurrency = Currencies.USD;
                    break;

                default:
                    // 0 commission
                    break;
            }

            return new OrderFee(new CashAmount(
                feeResult,
                feeCurrency));
        }
    }
}
