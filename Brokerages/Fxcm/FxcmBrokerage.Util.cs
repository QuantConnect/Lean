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
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Fxcm
{
    public partial class FxcmBrokerage
    {
        /// <summary>
        /// Converts an FXCM order into a QuantConnect order.
        /// </summary>
        /// <param name="fxcmOrder">The FXCM order</param>
        private static Order ConvertOrder(ExecutionReport fxcmOrder)
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
                    StopPrice = (decimal)fxcmOrder.getPrice()
                };
            }

            else if (fxcmOrder.getOrdType() == OrdTypeFactory.STOP_LIMIT)
            {
                order = new StopLimitOrder
                {
                    StopPrice = (decimal)fxcmOrder.getPrice(),
                    LimitPrice = (decimal)fxcmOrder.getStopPx()
                };
            }

            else
            {
                throw new NotSupportedException("The FXCM order type " + fxcmOrder.getOrdType() + " is not supported.");
            }

            order.Symbol = ConvertSymbol(fxcmOrder.getInstrument());
            order.Quantity = Convert.ToInt32(fxcmOrder.getOrderQty());
            order.SecurityType = GetSecurityType(fxcmOrder.getInstrument());
            order.Status = ConvertOrderStatus(fxcmOrder.getFXCMOrdStatus());
            order.BrokerId.Add(Convert.ToInt64(fxcmOrder.getOrderID()));
            order.Duration = ConvertDuration(fxcmOrder.getTimeInForce());
            order.Time = FromJavaDate(fxcmOrder.getTransactTime().toDate());

            return order;
        }

        /// <summary>
        /// Converts the tradier order duration into a qc order duration
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
        /// Converts an FXCM position into a QuantConnect holding.
        /// </summary>
        /// <param name="fxcmPosition">The FXCM position</param>
        private static Holding ConvertHolding(PositionReport fxcmPosition)
        {
            return new Holding
            {
                Symbol = ConvertSymbol(fxcmPosition.getInstrument()),
                Type = GetSecurityType(fxcmPosition.getInstrument()),
                AveragePrice = (decimal)fxcmPosition.getSettlPrice(),
                ConversionRate = 1.0m,
                CurrencySymbol = "$",
                Quantity = (decimal)(fxcmPosition.getPositionQty().getLongQty() > 0 
                    ? fxcmPosition.getPositionQty().getLongQty() 
                    : -fxcmPosition.getPositionQty().getShortQty())
            };        
        }

        /// <summary>
        /// Gets the <see cref="SecurityType"/> of an FXCM instrument
        /// </summary>
        /// <param name="instrument">The FXCM instrument</param>
        /// <returns>The security type of the instrument</returns>
        private static SecurityType GetSecurityType(Instrument instrument)
        {
            return instrument.getFXCMProductID() == IFixValueDefs.__Fields.FXCMPRODUCTID_FOREX
                ? SecurityType.Forex
                : SecurityType.Cfd;
        }

        private static string ConvertSymbol(Instrument instrument)
        {
            return ConvertFxcmSymbolToSymbol(instrument.getSymbol());
        }

        private static string ConvertFxcmSymbolToSymbol(string symbol)
        {
            return symbol.Replace("/", "").ToUpper();
        }

        /// <summary>
        /// Converts a QuantConnect symbol to an FXCM symbol
        /// </summary>
        public string ConvertSymbolToFxcmSymbol(string symbol)
        {
            return _mapInstrumentSymbols[symbol];
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
                                cal.get(java.util.Calendar.SECOND));
        }

    }
}
