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

using System.Collections.Generic;
using Python.Runtime;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Wraps a <see cref="PyObject"/> object that models brokerage transactions, fees, and order
    /// </summary>
    public class BrokerageModelPythonWrapper : IBrokerageModel
    {
        private readonly dynamic _model;

        /// <summary>
        /// Constructor for initialising the <see cref="BrokerageModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Models brokerage transactions, fees, and order</param>
        public BrokerageModelPythonWrapper(PyObject model)
        {
            _model = model;
        }

        /// <summary>
        /// Gets or sets the account type used by this model
        /// </summary>
        public AccountType AccountType
        {
            get
            {
                using (Py.GIL())
                {
                    return _model.AccountType;
                }
            }
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get
            {
                using (Py.GIL())
                {
                    return _model.DefaultMarkets;
                }
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.ApplySplit(List{OrderTicket}, Split)" /> in Python
        /// </summary>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        public void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            using (Py.GIL())
            {
                _model.ApplySplit(tickets, split);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.CanExecuteOrder(Security, Order)" /> in Python
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public bool CanExecuteOrder(Security security, Order order)
        {
            using (Py.GIL())
            {
                return _model.CanExecuteOrder(security, order);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.CanSubmitOrder(Security, Order, out BrokerageMessageEvent)" /> in Python
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                return _model.CanSubmitOrder(security, order, out message);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.CanUpdateOrder(Security, Order, UpdateOrderRequest, out BrokerageMessageEvent)" /> in Python
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested updated to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                return _model.CanUpdateOrder(security, order, out message);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.GetFeeModel(Security)" /> in Python
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public IFeeModel GetFeeModel(Security security)
        {
            using (Py.GIL())
            {
                return _model.GetFeeModel(security);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.GetFillModel(Security)" /> in Python
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        public IFillModel GetFillModel(Security security)
        {
            using (Py.GIL())
            {
                return _model.GetFillModel(security);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.GetLeverage(Security)" /> in Python
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public decimal GetLeverage(Security security)
        {
            using (Py.GIL())
            {
                return _model.GetLeverage(security);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.GetSettlementModel(Security, AccountType)" /> in Python
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The settlement model for this brokerage</returns>
        public ISettlementModel GetSettlementModel(Security security, AccountType accountType)
        {
            using (Py.GIL())
            {
                return _model.GetSettlementModel(security, accountType);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IBrokerageModel.GetSlippageModel(Security)" /> in Python
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        public ISlippageModel GetSlippageModel(Security security)
        {
            using (Py.GIL())
            {
                return _model.GetSlippageModel(security);
            }
        }
    }
}