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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Orders.OptionExercise
{
    /// <summary>
    /// Represents the default option exercise model (physical, cash settlement)
    /// </summary>
    public class DefaultExerciseModel : IOptionExerciseModel
    {
        /// <summary>
        /// Default option exercise model for the basic equity/index option security class. 
        /// </summary>
        /// <param name="option">Option we're trading this order</param>
        /// <param name="order">Order to update</param>
        public OrderEvent OptionExercise(Option option, OptionExerciseOrder order)
        {
            //Default order event to return
            var utcTime = option.LocalTime.ConvertToUtc(option.Exchange.TimeZone);
            var deliveryEvent = new OrderEvent(order, utcTime, 0);

            // make sure the exchange is open before filling
            if (!IsExchangeOpen(option)) return deliveryEvent;

            //Order price for a option exercise order model is the strike price
            deliveryEvent.FillPrice = order.Price;
            deliveryEvent.FillQuantity = option.GetExerciseQuantity(order.Quantity);
            deliveryEvent.OrderFee = option.FeeModel.GetOrderFee(option, order);
            deliveryEvent.Status = OrderStatus.Filled;

            return deliveryEvent;
        }
        /// <summary>
        /// Determines if the exchange is open using the current time of the asset
        /// </summary>
        private static bool IsExchangeOpen(Security asset)
        {
            if (!asset.Exchange.DateTimeIsOpen(asset.LocalTime))
            {
                // if we're not open at the current time exactly, check the bar size, this handle large sized bars (hours/days)
                var currentBar = asset.GetLastData();
                if (asset.LocalTime.Date != currentBar.EndTime.Date || !asset.Exchange.IsOpenDuringBar(currentBar.Time, currentBar.EndTime, false))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
