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
using com.fxcm.entity;
using com.fxcm.fix;
using com.fxcm.fix.posttrade;
using com.fxcm.fix.trade;
using com.sun.rowset;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// FXCM brokerage - private helper functions
    /// </summary>
    public partial class FxcmBrokerage
    {
        /// <summary>
        /// Converts an FXCM order to a QuantConnect order.
        /// </summary>
        /// <param name="fxcmOrder">The FXCM order</param>
        private Order ConvertOrder(ExecutionReport fxcmOrder)
        {
            Order order;

            if (fxcmOrder.getOrdType() == OrdTypeFactory.LIMIT)
            {
                order = new LimitOrder
                {
                    LimitPrice = Convert.ToDecimal(fxcmOrder.getPrice())
                };
            }

            else if (fxcmOrder.getOrdType() == OrdTypeFactory.MARKET)
            {
                order = new MarketOrder();
            }

            else if (fxcmOrder.getOrdType() == OrdTypeFactory.STOP)
            {
                order = new StopMarketOrder
                {
                    StopPrice = Convert.ToDecimal(fxcmOrder.getPrice())
                };
            }

            else
            {
                throw new NotSupportedException("FxcmBrokerage.ConvertOrder(): The FXCM order type " + fxcmOrder.getOrdType() + " is not supported.");
            }

            order.SecurityType = _symbolMapper.GetBrokerageSecurityType(fxcmOrder.getInstrument().getSymbol());
            order.Symbol = _symbolMapper.GetLeanSymbol(fxcmOrder.getInstrument().getSymbol(), order.SecurityType, Market.FXCM);
            order.Quantity = Convert.ToInt32(fxcmOrder.getOrderQty() * (fxcmOrder.getSide() == SideFactory.BUY ? +1 : -1));
            order.Status = ConvertOrderStatus(fxcmOrder.getFXCMOrdStatus());
            order.BrokerId.Add(fxcmOrder.getOrderID());
            order.Duration = ConvertDuration(fxcmOrder.getTimeInForce());
            order.Time = FromJavaDate(fxcmOrder.getTransactTime().toDate());

            return order;
        }

        /// <summary>
        /// Converts an FXCM order duration to QuantConnect order duration
        /// </summary>
        private static OrderDuration ConvertDuration(ITimeInForce timeInForce)
        {
            if (timeInForce == TimeInForceFactory.GOOD_TILL_CANCEL)
                return OrderDuration.GTC;
            
            if (timeInForce == TimeInForceFactory.DAY)
                return (OrderDuration)1; //.Day;

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Converts an FXCM position to a QuantConnect holding.
        /// </summary>
        /// <param name="fxcmPosition">The FXCM position</param>
        private Holding ConvertHolding(PositionReport fxcmPosition)
        {
            var securityType = _symbolMapper.GetBrokerageSecurityType(fxcmPosition.getInstrument().getSymbol());

            return new Holding
            {
                Symbol = _symbolMapper.GetLeanSymbol(fxcmPosition.getInstrument().getSymbol(), securityType, Market.FXCM),
                Type = securityType,
                AveragePrice = Convert.ToDecimal(fxcmPosition.getSettlPrice()),
                ConversionRate = 1.0m,
                CurrencySymbol = "$",
                Quantity = Convert.ToDecimal(fxcmPosition.getPositionQty().getLongQty() > 0 
                    ? fxcmPosition.getPositionQty().getLongQty() 
                    : -fxcmPosition.getPositionQty().getShortQty())
            };        
        }

        /// <summary>
        /// Converts an FXCM OrderStatus to a QuantConnect <see cref="OrderStatus"/>
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private static OrderStatus ConvertOrderStatus(ICode status)
        {
            var result = OrderStatus.None;

            switch (status.getCode())
            {
                case IFixValueDefs.__Fields.FXCMORDSTATUS_INPROCESS:
                case IFixValueDefs.__Fields.FXCMORDSTATUS_WAITING:
                case IFixValueDefs.__Fields.FXCMORDSTATUS_EXECUTING:
                    result = OrderStatus.Submitted;
                    break;

                case IFixValueDefs.__Fields.FXCMORDSTATUS_EXECUTED:
                    result = OrderStatus.Filled;
                    break;

                case IFixValueDefs.__Fields.FXCMORDSTATUS_CANCELLED:
                case IFixValueDefs.__Fields.FXCMORDSTATUS_EXPIRED:
                    result = OrderStatus.Canceled;
                    break;

                case IFixValueDefs.__Fields.FXCMORDSTATUS_REJECTED:
                    result = OrderStatus.Invalid;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the specified order is considered open, otherwise false
        /// </summary>
        private static bool OrderIsOpen(string orderStatus)
        {
            return orderStatus == IFixValueDefs.__Fields.FXCMORDSTATUS_WAITING;
        }

        /// <summary>
        /// Returns true if the specified order is considered close, otherwise false
        /// </summary>
        protected static bool OrderIsClosed(string orderStatus)
        {
            return orderStatus == IFixValueDefs.__Fields.FXCMORDSTATUS_EXECUTED
                || orderStatus == IFixValueDefs.__Fields.FXCMORDSTATUS_CANCELLED
                || orderStatus == IFixValueDefs.__Fields.FXCMORDSTATUS_EXPIRED
                || orderStatus == IFixValueDefs.__Fields.FXCMORDSTATUS_REJECTED;
        }

        /// <summary>
        /// Returns true if the specified order is being processed, otherwise false
        /// </summary>
        private static bool OrderIsBeingProcessed(string orderStatus)
        {
            return !OrderIsOpen(orderStatus) && !OrderIsClosed(orderStatus);
        }

        /// <summary>
        /// Converts a Java Date value to a DateTime value
        /// </summary>
        /// <param name="javaDate">The Java date</param>
        /// <returns></returns>
        private static DateTime FromJavaDate(java.util.Date javaDate)
        {
            var cal = java.util.Calendar.getInstance();
            cal.setTime(javaDate);

            // note that the Month component of java.util.Date  
            // from 0-11 (i.e. Jan == 0)
            return new DateTime(cal.get(java.util.Calendar.YEAR),
                                cal.get(java.util.Calendar.MONTH) + 1,
                                cal.get(java.util.Calendar.DAY_OF_MONTH),
                                cal.get(java.util.Calendar.HOUR_OF_DAY),
                                cal.get(java.util.Calendar.MINUTE),
                                cal.get(java.util.Calendar.SECOND),
                                cal.get(java.util.Calendar.MILLISECOND));
        }


        //
        // So it turns out that in order to properly load the QuantConnect.Brokerages
        // dll we need the IKVM.OpenJdk.Jdbc referenced in other projects that use 
        // this. By placing a hard reference to an IKVM.OpenJdk.Jdbc type, the compiler
        // will properly copy the required dlls into other project bin directories. 
        // Without this, consuming projects would need to hard refernce the IKVM dlls,
        // which is less than perfect. This seems to be the better of two evils
        //
        private static void ManageIKVMDependency()
        {
            var rowset = new CachedRowSetImpl();
        }
    }
}
