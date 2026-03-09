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

using Python.Runtime;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Python
{
    /// <summary>
    /// Wraps a <see cref="PyObject"/> object that represents a model that simulates order fill events
    /// </summary>
    public class FillModelPythonWrapper : FillModel
    {
        private readonly BasePythonWrapper<FillModel> _model;

        /// <summary>
        /// Constructor for initialising the <see cref="FillModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Represents a model that simulates order fill events</param>
        public FillModelPythonWrapper(PyObject model)
        {
            _model = new BasePythonWrapper<FillModel>(model, false);
            using (Py.GIL())
            {
                (model as dynamic).SetPythonWrapper(this);
            }
        }

        /// <summary>
        /// Return an order event with the fill details
        /// </summary>
        /// <param name="parameters">A parameters object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override Fill Fill(FillModelParameters parameters)
        {
            Parameters = parameters;
            return _model.InvokeMethod<Fill>(nameof(Fill), parameters);
        }

        /// <summary>
        /// Limit Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Stock Object to use to help model limit fill</param>
        /// <param name="order">Order to fill. Alter the values directly if filled.</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(LimitFill), asset, order);
        }

        /// <summary>
        /// Limit if Touched Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order"><see cref="LimitIfTouchedOrder"/> Order to Check, return filled if true</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent LimitIfTouchedFill(Security asset, LimitIfTouchedOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(LimitIfTouchedFill), asset, order);
        }

        /// <summary>
        /// Model the slippage on a market order: fixed percentage of order price
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Order to update</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(MarketFill), asset, order);
        }

        /// <summary>
        /// Market on Close Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(MarketOnCloseFill), asset, order);
        }

        /// <summary>
        /// Market on Open Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(MarketOnOpenFill), asset, order);
        }

        /// <summary>
        /// Stop Limit Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Stop Limit Order to Check, return filled if true</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent StopLimitFill(Security asset, StopLimitOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(StopLimitFill), asset, order);
        }

        /// <summary>
        /// Stop Market Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Trailing Stop Order to check, return filled if true</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(StopMarketFill), asset, order);
        }

        /// <summary>
        /// Trailing Stop Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Stop Order to Check, return filled if true</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent TrailingStopFill(Security asset, TrailingStopOrder order)
        {
            return _model.InvokeMethod<OrderEvent>(nameof(TrailingStopFill), asset, order);
        }

        /// <summary>
        /// Default combo market fill model for the base security class. Fills at the last traded price for each leg.
        /// </summary>
        /// <param name="order">Order to fill</param>
        /// <param name="parameters">Fill parameters for the order</param>
        /// <returns>Order fill information detailing the average price and quantity filled for each leg. If any of the fills fails, none of the orders will be filled and the returned list will be empty</returns>
        public override List<OrderEvent> ComboMarketFill(Order order, FillModelParameters parameters)
        {
            return _model.InvokeMethod<List<OrderEvent>>(nameof(ComboMarketFill), order, parameters);
        }

        /// <summary>
        /// Default combo limit fill model for the base security class. Fills at the sum of prices for the assets of every leg.
        /// </summary>
        /// <param name="order">Order to fill</param>
        /// <param name="parameters">Fill parameters for the order</param>
        /// <returns>Order fill information detailing the average price and quantity filled for each leg. If any of the fills fails, none of the orders will be filled and the returned list will be empty</returns>
        public override List<OrderEvent> ComboLimitFill(Order order, FillModelParameters parameters)
        {
            return _model.InvokeMethod<List<OrderEvent>>(nameof(ComboLimitFill), order, parameters);
        }

        /// <summary>
        /// Default combo limit fill model for the base security class. Fills at the limit price for each leg
        /// </summary>
        /// <param name="order">Order to fill</param>
        /// <param name="parameters">Fill parameters for the order</param>
        /// <returns>Order fill information detailing the average price and quantity filled for each leg. If any of the fills fails, none of the orders will be filled and the returned list will be empty</returns>
        public override List<OrderEvent> ComboLegLimitFill(Order order, FillModelParameters parameters)
        {
            return _model.InvokeMethod<List<OrderEvent>>(nameof(ComboLegLimitFill), order, parameters);
        }

        /// <summary>
        /// Get the minimum and maximum price for this security in the last bar:
        /// </summary>
        /// <param name="asset">Security asset we're checking</param>
        /// <param name="direction">The order direction, decides whether to pick bid or ask</param>
        protected override Prices GetPrices(Security asset, OrderDirection direction)
        {
            return _model.InvokeMethod<Prices>(nameof(GetPrices), asset, direction);
        }

        /// <summary>
        /// Get the minimum and maximum price for this security in the last bar:
        /// </summary>
        /// <param name="asset">Security asset we're checking</param>
        /// <param name="direction">The order direction, decides whether to pick bid or ask</param>
        /// <remarks>This method was implemented temporarily to help the refactoring of fill models (GH #4567)</remarks>
        internal Prices GetPricesInternal(Security asset, OrderDirection direction)
        {
            return GetPrices(asset, direction);
        }
    }
}
