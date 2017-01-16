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
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.PythonWrappers
{
    /// <summary>
    /// Wrapper for an IBrokerageModel instance created in Python.
    /// All calls to python should be inside a "using (Py.GIL()) {/* Your code here */}" block.
    /// </summary>
    class BrokerageModelPythonWrapper : IBrokerageModel
    {
        private IBrokerageModel _brokerageModel;

        public BrokerageModelPythonWrapper(IBrokerageModel brokegeModel)
        {
            _brokerageModel = brokegeModel;
        } 

        public AccountType AccountType
        {
            get
            {
                using (Py.GIL())
                {
                    return _brokerageModel.AccountType;
                }
            }
        }

        public IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get
            {
                using (Py.GIL())
                {
                    return _brokerageModel.DefaultMarkets;
                }
            }
        }

        public void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            using (Py.GIL())
            {
                _brokerageModel.ApplySplit(tickets, split);
            }
        }

        public bool CanExecuteOrder(Security security, Order order)
        {
            using (Py.GIL())
            {
                return _brokerageModel.CanExecuteOrder(security, order);
            }
        }

        public bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                return _brokerageModel.CanSubmitOrder(security, order, out message);
            }
        }

        public bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                return _brokerageModel.CanUpdateOrder(security, order, request, out message);
            }
        }

        public IFeeModel GetFeeModel(Security security)
        {
            using (Py.GIL())
            {
                return _brokerageModel.GetFeeModel(security);
            }
        }

        public IFillModel GetFillModel(Security security)
        {
            using (Py.GIL())
            {
                return _brokerageModel.GetFillModel(security);
            }
        }

        public decimal GetLeverage(Security security)
        {
            using (Py.GIL())
            {
                return _brokerageModel.GetLeverage(security);
            }
        }

        public ISettlementModel GetSettlementModel(Security security, AccountType accountType)
        {
            using (Py.GIL())
            {
                return _brokerageModel.GetSettlementModel(security, AccountType);
            }
        }

        public ISlippageModel GetSlippageModel(Security security)
        {
            using (Py.GIL())
            {
                return _brokerageModel.GetSlippageModel(security);
            }
        }
    }
}
