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
using QuantConnect.Benchmarks;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Python;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Models brokerage transactions, fees, and order
    /// </summary>
    public interface IBrokerageModel
    {
        /// <summary>
        /// Gets the account type used by this model
        /// </summary>
        AccountType AccountType
        {
            get;
        }

        /// <summary>
        /// Gets the brokerages model percentage factor used to determine the required unused buying power for the account.
        /// From 1 to 0. Example: 0 means no unused buying power is required. 0.5 means 50% of the buying power should be left unused.
        /// </summary>
        decimal RequiredFreeBuyingPowerPercent
        {
            get;
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message);

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested updated to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message);

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        bool CanExecuteOrder(Security security, Order order);

        /// <summary>
        /// Applies the split to the specified order ticket
        /// </summary>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        void ApplySplit(List<OrderTicket> tickets, Split split);

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        decimal GetLeverage(Security security);

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        IBenchmark GetBenchmark(SecurityManager securities);

        /// <summary>
        /// Gets a new fill model that represents this brokerage's fill behavior
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        IFillModel GetFillModel(Security security);

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        IFeeModel GetFeeModel(Security security);

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        ISlippageModel GetSlippageModel(Security security);

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <returns>The settlement model for this brokerage</returns>
        ISettlementModel GetSettlementModel(Security security);

        /// <summary>
        /// Gets a new margin interest rate model for the security
        /// </summary>
        /// <param name="security">The security to get a margin interest rate model for</param>
        /// <returns>The margin interest rate model for this brokerage</returns>
        IMarginInterestRateModel GetMarginInterestRateModel(Security security);

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The settlement model for this brokerage</returns>
        [Obsolete("Flagged deprecated and will remove December 1st 2018")]
        ISettlementModel GetSettlementModel(Security security, AccountType accountType);

        /// <summary>
        /// Gets a new buying power model for the security
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        IBuyingPowerModel GetBuyingPowerModel(Security security);

        /// <summary>
        /// Gets a new buying power model for the security
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        [Obsolete("Flagged deprecated and will remove December 1st 2018")]
        IBuyingPowerModel GetBuyingPowerModel(Security security, AccountType accountType);

        /// <summary>
        /// Gets the shortable provider
        /// </summary>
        /// <returns>Shortable provider</returns>
        IShortableProvider GetShortableProvider();
    }

    /// <summary>
    /// Provides factory method for creating an <see cref="IBrokerageModel"/> from the <see cref="BrokerageName"/> enum
    /// </summary>
    public static class BrokerageModel
    {
        /// <summary>
        /// Creates a new <see cref="IBrokerageModel"/> for the specified <see cref="BrokerageName"/>
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        /// <param name="brokerage">The name of the brokerage</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The model for the specified brokerage</returns>
        public static IBrokerageModel Create(IOrderProvider orderProvider, BrokerageName brokerage, AccountType accountType)
        {
            switch (brokerage)
            {
                case BrokerageName.Default:
                    return new DefaultBrokerageModel(accountType);

                case BrokerageName.InteractiveBrokersBrokerage:
                    return new InteractiveBrokersBrokerageModel(accountType);

                case BrokerageName.TradierBrokerage:
                    return new TradierBrokerageModel(accountType);

                case BrokerageName.OandaBrokerage:
                    return new OandaBrokerageModel(accountType);

                case BrokerageName.FxcmBrokerage:
                    return new FxcmBrokerageModel(accountType);

                case BrokerageName.Bitfinex:
                    return new BitfinexBrokerageModel(accountType);

                case BrokerageName.BinanceFutures:
                    return new BinanceFuturesBrokerageModel(accountType);

                case BrokerageName.BinanceCoinFutures:
                    return new BinanceCoinFuturesBrokerageModel(accountType);

                case BrokerageName.Binance:
                    return new BinanceBrokerageModel(accountType);

                case BrokerageName.BinanceUS:
                    return new BinanceUSBrokerageModel(accountType);

                case BrokerageName.GDAX:
                    return new GDAXBrokerageModel(accountType);

                case BrokerageName.AlphaStreams:
                    return new AlphaStreamsBrokerageModel(accountType);

                case BrokerageName.Zerodha:
                    return new ZerodhaBrokerageModel(accountType);

                case BrokerageName.Atreyu:
                    return new AtreyuBrokerageModel(accountType);

                case BrokerageName.TradingTechnologies:
                    return new TradingTechnologiesBrokerageModel(accountType);

                case BrokerageName.Samco:
                    return new SamcoBrokerageModel(accountType);

                case BrokerageName.Kraken:
                    return new KrakenBrokerageModel(accountType);

                case BrokerageName.Exante:
                    return new ExanteBrokerageModel(accountType);

                case BrokerageName.FTX:
                    return new FTXBrokerageModel(accountType);

                case BrokerageName.FTXUS:
                    return new FTXUSBrokerageModel(accountType);
                
                case BrokerageName.Wolverine:
                    return new WolverineBrokerageModel(accountType);

                case BrokerageName.TDAmeritrade:
                    return new TDAmeritradeBrokerageModel(accountType);

                case BrokerageName.RBI: 
                    return new RBIBrokerageModel(accountType);

                default:
                    throw new ArgumentOutOfRangeException(nameof(brokerage), brokerage, null);
            }
        }


        /// <summary>
        /// Gets the corresponding <see cref="BrokerageName"/> for the specified <see cref="IBrokerageModel"/>
        /// </summary>
        /// <param name="brokerageModel">The brokerage model</param>
        /// <returns>The <see cref="BrokerageName"/> for the specified brokerage model</returns>
        public static BrokerageName GetBrokerageName(IBrokerageModel brokerageModel)
        {
            var model = brokerageModel;
            if (brokerageModel is BrokerageModelPythonWrapper)
            {
                model = (brokerageModel as BrokerageModelPythonWrapper).GetModel();
            }

            // Case order matters to ensure we get the correct brokerage name from the inheritance chain
            switch (model)
            {
                case InteractiveBrokersBrokerageModel _:
                    return BrokerageName.InteractiveBrokersBrokerage;

                case TradierBrokerageModel _:
                    return BrokerageName.TradierBrokerage;

                case OandaBrokerageModel _:
                    return BrokerageName.OandaBrokerage;

                case FxcmBrokerageModel _:
                    return BrokerageName.FxcmBrokerage;

                case BitfinexBrokerageModel _:
                    return BrokerageName.Bitfinex;

                case BinanceUSBrokerageModel _:
                    return BrokerageName.BinanceUS;

                case BinanceBrokerageModel _:
                    return BrokerageName.Binance;

                case GDAXBrokerageModel _:
                    return BrokerageName.GDAX;

                case AlphaStreamsBrokerageModel _:
                    return BrokerageName.AlphaStreams;

                case ZerodhaBrokerageModel _:
                    return BrokerageName.Zerodha;

                case AtreyuBrokerageModel _:
                    return BrokerageName.Atreyu;

                case TradingTechnologiesBrokerageModel _:
                    return BrokerageName.TradingTechnologies;

                case SamcoBrokerageModel _:
                    return BrokerageName.Samco;

                case KrakenBrokerageModel _:
                    return BrokerageName.Kraken;

                case ExanteBrokerageModel _:
                    return BrokerageName.Exante;

                case FTXUSBrokerageModel _:
                    return BrokerageName.FTXUS;

                case FTXBrokerageModel _:
                    return BrokerageName.FTX;

                case WolverineBrokerageModel _:
                    return BrokerageName.Wolverine;

                case TDAmeritradeBrokerageModel _:
                    return BrokerageName.TDAmeritrade;

                case RBIBrokerageModel _:
                    return BrokerageName.RBI;

                case DefaultBrokerageModel _:
                    return BrokerageName.Default;

                default:
                    throw new ArgumentOutOfRangeException(nameof(brokerageModel), brokerageModel, null);
            }
        }
    }
}
