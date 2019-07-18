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
using java.util;
using NodaTime;
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

            var securityType = _symbolMapper.GetBrokerageSecurityType(fxcmOrder.getInstrument().getSymbol());
            order.Symbol = _symbolMapper.GetLeanSymbol(fxcmOrder.getInstrument().getSymbol(), securityType, Market.FXCM);
            order.Quantity = Convert.ToInt32(fxcmOrder.getOrderQty() * (fxcmOrder.getSide() == SideFactory.BUY ? +1 : -1));
            order.Status = ConvertOrderStatus(fxcmOrder.getFXCMOrdStatus());
            order.BrokerId.Add(fxcmOrder.getOrderID());
            order.Properties.TimeInForce = ConvertTimeInForce(fxcmOrder.getTimeInForce());
            order.Time = FromJavaDate(fxcmOrder.getTransactTime().toDate());

            return order;
        }

        /// <summary>
        /// Converts an FXCM order time in force to QuantConnect order time in force
        /// </summary>
        private static TimeInForce ConvertTimeInForce(ITimeInForce timeInForce)
        {
            if (timeInForce == TimeInForceFactory.GOOD_TILL_CANCEL)
                return TimeInForce.GoodTilCanceled;

            if (timeInForce == TimeInForceFactory.DAY)
                return TimeInForce.Day;

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
        /// Converts a Java Date value to a UTC DateTime value
        /// </summary>
        /// <param name="javaDate">The Java date</param>
        /// <returns></returns>
        private static DateTime FromJavaDate(Date javaDate)
        {
            // Convert javaDate to UTC Instant (Epoch)
            var instant = Instant.FromMillisecondsSinceUnixEpoch(javaDate.getTime());

            // Convert to .Net UTC DateTime
            return instant.ToDateTimeUtc();
        }

        /// <summary>
        /// Converts a LEAN Resolution to an IFXCMTimingInterval
        /// </summary>
        /// <param name="resolution">The resolution to convert</param>
        /// <returns></returns>
        public static IFXCMTimingInterval ToFxcmInterval(Resolution resolution)
        {
            IFXCMTimingInterval interval = null;

            switch (resolution)
            {
                case Resolution.Tick:
                    interval = FXCMTimingIntervalFactory.TICK;

                    break;
                case Resolution.Second:
                    interval = FXCMTimingIntervalFactory.SEC10;

                    break;
                case Resolution.Minute:
                    interval = FXCMTimingIntervalFactory.MIN1;

                    break;
                case Resolution.Hour:
                    interval = FXCMTimingIntervalFactory.HOUR1;

                    break;
                case Resolution.Daily:
                    interval = FXCMTimingIntervalFactory.DAY1;

                    break;
            }

            return interval;
        }

        /// <summary>
        /// Converts a Java Date value to a UTC DateTime value
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value</param>
        /// <returns>A UTC Java Date value</returns>
        public static Date ToJavaDateUtc(DateTime utcDateTime)
        {
            var cal = Calendar.getInstance();
            cal.setTimeZone(java.util.TimeZone.getTimeZone("UTC"));

            cal.set(Calendar.YEAR, utcDateTime.Year);
            cal.set(Calendar.MONTH, utcDateTime.Month - 1);
            cal.set(Calendar.DAY_OF_MONTH, utcDateTime.Day);
            cal.set(Calendar.HOUR_OF_DAY, utcDateTime.Hour);
            cal.set(Calendar.MINUTE, utcDateTime.Minute);
            cal.set(Calendar.SECOND, utcDateTime.Second);
            cal.set(Calendar.MILLISECOND, utcDateTime.Millisecond);

            return cal.getTime();
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
