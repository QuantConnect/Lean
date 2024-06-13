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
using System.Runtime.CompilerServices;
using System.Text;

using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Securities"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.AccountEvent"/> class and its consumers or related classes
        /// </summary>
        public static class AccountEvent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.AccountEvent accountEvent)
            {
                return Invariant($"Account {accountEvent.CurrencySymbol} Balance: {accountEvent.CashBalance:0.00}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.BuyingPowerModel"/> class and its consumers or related classes
        /// </summary>
        public static class BuyingPowerModel
        {
            public static string InvalidInitialMarginRequirement = "Initial margin requirement must be between 0 and 1";

            public static string InvalidMaintenanceMarginRequirement = "Maintenance margin requirement must be between 0 and 1";

            public static string InvalidFreeBuyingPowerPercentRequirement = "Free Buying Power Percent requirement must be between 0 and 1";

            public static string InvalidLeverage = "Leverage must be greater than or equal to 1.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InsufficientBuyingPowerDueToNullOrderTicket(Orders.Order order)
            {
                return Invariant($"Null order ticket for id: {order.Id}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InsufficientBuyingPowerDueToUnsufficientMargin(Orders.Order order,
                decimal initialMarginRequiredForRemainderOfOrder, decimal freeMargin)
            {
                return Invariant($@"Id: {order.Id}, Initial Margin: {
                    initialMarginRequiredForRemainderOfOrder.Normalize()}, Free Margin: {freeMargin.Normalize()}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TargetOrderMarginNotAboveMinimum(decimal absDifferenceOfMargin, decimal minimumValue)
            {
                return Invariant($"The target order margin {absDifferenceOfMargin} is less than the minimum {minimumValue}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TargetOrderMarginNotAboveMinimum()
            {
                return "Warning: Portfolio rebalance result ignored as it resulted in a single share trade recommendation which can generate high fees." +
                    " To disable minimum order size checks please set Settings.MinimumOrderMarginPortfolioPercentage = 0.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderQuantityLessThanLotSize(Securities.Security security, decimal targetOrderMargin)
            {
                return Invariant($@"The order quantity is less than the lot size of {
                    security.SymbolProperties.LotSize} and has been rounded to zero. Target order margin {targetOrderMargin}. ");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToConvergeOnTheTargetMargin(GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters,
                decimal signedTargetFinalMarginValue, decimal orderFees)
            {
                return Invariant($@"GetMaximumOrderQuantityForTargetBuyingPower failed to converge on the target margin: {
                    signedTargetFinalMarginValue}; the following information can be used to reproduce the issue. Total Portfolio Cash: {
                    parameters.Portfolio.Cash}; Security : {parameters.Security.Symbol.ID}; Price : {parameters.Security.Close}; Leverage: {
                    parameters.Security.Leverage}; Order Fee: {orderFees}; Lot Size: {
                    parameters.Security.SymbolProperties.LotSize}; Current Holdings: {parameters.Security.Holdings.Quantity} @ {
                    parameters.Security.Holdings.AveragePrice}; Target Percentage: %{parameters.TargetBuyingPower * 100};");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToConvergeOnTheTargetMarginUnderlyingSecurityInfo(Securities.Security underlying)
            {
                return Invariant($@"Underlying Security: {underlying.Symbol.ID}; Underlying Price: {
                    underlying.Close}; Underlying Holdings: {underlying.Holdings.Quantity} @ {underlying.Holdings.AveragePrice};");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MarginBeingAdjustedInTheWrongDirection(decimal targetMargin, decimal marginForOneUnit, Securities.Security security)
            {
                return Invariant(
                    $@"Margin is being adjusted in the wrong direction. Reproduce this issue with the following variables, Target Margin: {
                        targetMargin}; MarginForOneUnit: {marginForOneUnit}; Security Holdings: {security.Holdings.Quantity} @ {
                        security.Holdings.AveragePrice}; LotSize: {security.SymbolProperties.LotSize}; Price: {security.Close}; Leverage: {
                        security.Leverage}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MarginBeingAdjustedInTheWrongDirectionUnderlyingSecurityInfo(Securities.Security underlying)
            {
                return Invariant($@"Underlying Security: {underlying.Symbol.ID}; Underlying Price: {
                    underlying.Close}; Underlying Holdings: {underlying.Holdings.Quantity} @ {underlying.Holdings.AveragePrice};");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.PositionGroupBuyingPowerModel"/> class and its consumers or related classes
        /// </summary>
        public static class PositionGroupBuyingPowerModel
        {

            public static string DeltaCannotBeApplied = "No buying power used, delta cannot be applied";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ComputedZeroInitialMargin(IPositionGroup positionGroup)
            {
                return Invariant($"Computed zero initial margin requirement for {positionGroup.GetUserFriendlyName()}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string PositionGroupQuantityRoundedToZero(decimal targetOrderMargin)
            {
                return Invariant($"The position group order quantity has been rounded to zero. Target order margin {targetOrderMargin}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToConvergeOnTargetMargin(decimal targetMargin, decimal positionGroupQuantity, decimal orderFees,
                GetMaximumLotsForTargetBuyingPowerParameters parameters)
            {
                return Invariant($@"Failed to converge on the target margin: {targetMargin}; the following information can be used to reproduce the issue. Total Portfolio Cash: {parameters.Portfolio.Cash}; Position group: {parameters.PositionGroup.GetUserFriendlyName()}; Position group order quantity: {positionGroupQuantity} Order Fee: {orderFees}; Current Holdings: {parameters.PositionGroup.Quantity}; Target Percentage: %{parameters.TargetBuyingPower * 100};");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.Cash"/> class and its consumers or related classes
        /// </summary>
        public static class Cash
        {
            public static string NullOrEmptyCashSymbol = "Cash symbols cannot be null or empty.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoTradablePairFoundForCurrencyConversion(string cashCurrencySymbol, string accountCurrency,
                IEnumerable<KeyValuePair<SecurityType, string>> marketMap)
            {
                return Invariant($@"No tradeable pair was found for currency {cashCurrencySymbol}, conversion rate to account currency ({
                    accountCurrency}) will be set to zero. Markets: [{string.Join(",", marketMap.Select(x => $"{x.Key}:{x.Value}"))}]");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string AddingSecuritySymbolForCashCurrencyFeed(QuantConnect.Symbol symbol, string cashCurrencySymbol)
            {
                return Invariant($"Adding {symbol.Value} {symbol.ID.Market} for cash {cashCurrencySymbol} currency feed");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.Cash cash, string accountCurrency)
            {
                // round the conversion rate for output
                var rate = cash.ConversionRate;
                rate = rate < 1000 ? rate.RoundToSignificantDigits(5) : Math.Round(rate, 2);
                return Invariant($@"{cash.Symbol}: {cash.CurrencySymbol}{cash.Amount,15:0.00} @ {rate,10:0.00####} = {
                    QuantConnect.Currencies.GetCurrencySymbol(accountCurrency)}{Math.Round(cash.ValueInAccountCurrency, 2)}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.CashBook"/> class and its consumers or related classes
        /// </summary>
        public static class CashBook
        {
            public static string UnexpectedRequestForNullCurrency = "Unexpected request for NullCurrency Cash instance";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConversionRateNotFound(string currency)
            {
                return Invariant($"The conversion rate for {currency} is not available.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.CashBook cashBook)
            {
                var sb = new StringBuilder();
                sb.AppendLine(Invariant($"Symbol {"Quantity",13}    {"Conversion",10} = Value in {cashBook.AccountCurrency}"));
                foreach (var value in cashBook.Values)
                {
                    sb.AppendLine(value.ToString(cashBook.AccountCurrency));
                }
                sb.AppendLine("-------------------------------------------------");
                sb.AppendLine(Invariant($@"CashBook Total Value:                {
                    QuantConnect.Currencies.GetCurrencySymbol(cashBook.AccountCurrency)}{
                    Math.Round(cashBook.TotalValueInAccountCurrency, 2).ToStringInvariant()}"));

                return sb.ToString();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CashSymbolNotFound(string symbol)
            {
                return $"This cash symbol ({symbol}) was not found in your cash book.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToRemoveRecord(string symbol)
            {
                return $"Failed to remove the cash book record for symbol {symbol}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.CashBuyingPowerModel"/> class and its consumers or related classes
        /// </summary>
        public static class CashBuyingPowerModel
        {
            public static string UnsupportedLeverage = "CashBuyingPowerModel does not allow setting leverage. Cash accounts have no leverage.";

            public static string GetMaximumOrderQuantityForDeltaBuyingPowerNotImplemented =
                $@"The {nameof(CashBuyingPowerModel)} does not require '{
                    nameof(Securities.CashBuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower)}'.";

            public static string ShortingNotSupported = "The cash model does not allow shorting.";

            public static string InvalidSecurity = $"The security type must be {nameof(SecurityType.Crypto)}or {nameof(SecurityType.Forex)}.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedSecurity(Securities.Security security)
            {
                return $@"The '{security.Symbol.Value}' security is not supported by this cash model. Currently only {
                    nameof(SecurityType.Crypto)} and {nameof(SecurityType.Forex)} are supported.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SellOrderShortHoldingsNotSupported(decimal totalQuantity, decimal openOrdersReservedQuantity, decimal orderQuantity,
                IBaseCurrencySymbol baseCurrency)
            {
                return Invariant($@"Your portfolio holds {totalQuantity.Normalize()} {
                    baseCurrency.BaseCurrency.Symbol}, {openOrdersReservedQuantity.Normalize()} {
                    baseCurrency.BaseCurrency.Symbol} of which are reserved for open orders, but your Sell order is for {
                    orderQuantity.Normalize()} {baseCurrency.BaseCurrency.Symbol
                    }. Cash Modeling trading does not permit short holdings so ensure you only sell what you have, including any additional open orders.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string BuyOrderQuantityGreaterThanMaxForBuyingPower(decimal totalQuantity, decimal maximumQuantity,
                decimal openOrdersReservedQuantity, decimal orderQuantity, IBaseCurrencySymbol baseCurrency, Securities.Security security,
                Orders.Order order)
            {
                return Invariant($@"Your portfolio holds {totalQuantity.Normalize()} {
                    security.QuoteCurrency.Symbol}, {openOrdersReservedQuantity.Normalize()} {
                    security.QuoteCurrency.Symbol} of which are reserved for open orders, but your Buy order is for {
                    order.AbsoluteQuantity.Normalize()} {baseCurrency.BaseCurrency.Symbol}. Your order requires a total value of {
                    orderQuantity.Normalize()} {security.QuoteCurrency.Symbol}, but only a total value of {
                    Math.Abs(maximumQuantity).Normalize()} {security.QuoteCurrency.Symbol} is available.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoDataInInternalCashFeedYet(Securities.Security security, Securities.SecurityPortfolioManager portfolio)
            {
                return Invariant($@"The internal cash feed required for converting {security.QuoteCurrency.Symbol} to {
                    portfolio.CashBook.AccountCurrency} does not have any data yet (or market may be closed).");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ZeroContractMultiplier(Securities.Security security)
            {
                return $@"The contract multiplier for the {
                    security.Symbol.Value} security is zero. The symbol properties database may be out of date.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderQuantityLessThanLotSize(Securities.Security security)
            {
                return Invariant($@"The order quantity is less than the lot size of {
                    security.SymbolProperties.LotSize} and has been rounded to zero.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderQuantityLessThanLotSizeOrderDetails(decimal targetOrderValue, decimal orderQuantity, decimal orderFees)
            {
                return Invariant($"Target order value {targetOrderValue}. Order fees {orderFees}. Order quantity {orderQuantity}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToConvergeOnTargetOrderValue(decimal targetOrderValue, decimal currentOrderValue, decimal orderQuantity,
                decimal orderFees, Securities.Security security)
            {
                return Invariant($@"GetMaximumOrderQuantityForTargetBuyingPower failed to converge to target order value {
                    targetOrderValue}. Current order value is {currentOrderValue}. Order quantity {orderQuantity}. Lot size is {
                    security.SymbolProperties.LotSize}. Order fees {orderFees}. Security symbol {security.Symbol}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.DefaultMarginCallModel"/> class and its consumers or related classes
        /// </summary>
        public static class DefaultMarginCallModel
        {
            public static string MarginCallOrderTag = "Margin Call";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.DynamicSecurityData"/> class and its consumers or related classes
        /// </summary>
        public static class DynamicSecurityData
        {
            public static string PropertiesCannotBeSet =
                "DynamicSecurityData is a view of the SecurityCache. It is readonly, properties can not be set";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string PropertyNotFound(string name)
            {
                return $"Property with name '{name}' does not exist.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedTypesForGetAll(Type type, object data)
            {
                return $"Expected a list with type '{type.GetBetterTypeName()}' but found type '{data.GetType().GetBetterTypeName()}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.EquityPriceVariationModel"/> class and its consumers or related classes
        /// </summary>
        public static class EquityPriceVariationModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityType(Securities.Security security)
            {
                return Invariant($"Invalid SecurityType: {security.Type}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.ErrorCurrencyConverter"/> class and its consumers or related classes
        /// </summary>
        public static class ErrorCurrencyConverter
        {
            public static string AccountCurrencyUnexpectedUsage = "Unexpected usage of ErrorCurrencyConverter.AccountCurrency";

            public static string ConvertToAccountCurrencyPurposefullyThrow =
                $@"This method purposefully throws as a proof that a test does not depend on {
                    nameof(ICurrencyConverter)}. If this exception is encountered, it means the test DOES depend on {
                     nameof(ICurrencyConverter)} and should be properly updated to use a real implementation of {nameof(ICurrencyConverter)}.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.FuncSecuritySeeder"/> class and its consumers or related classes
        /// </summary>
        public static class FuncSecuritySeeder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SeededSecurityInfo(BaseData seedData)
            {
                return $"Seeded security: {seedData.Symbol.Value}: {seedData.GetType()} {seedData.Value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToSeedSecurity(Securities.Security security)
            {
                return $"Unable to seed security: {security.Symbol.Value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToSecurityPrice(Securities.Security security)
            {
                return $"Could not seed price for security {security.Symbol}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.IdentityCurrencyConverter"/> class and its consumers or related classes
        /// </summary>
        public static class IdentityCurrencyConverter
        {
            public static string UnableToHandleCashInNonAccountCurrency =
                $"The {nameof(Securities.IdentityCurrencyConverter)} can only handle CashAmounts in units of the account currency";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.InitialMarginParameters"/> class and its consumers or related classes
        /// </summary>
        public static class InitialMarginParameters
        {
            public static string ForUnderlyingOnlyInvokableForIDerivativeSecurity =
                "ForUnderlying is only invokable for IDerivativeSecurity (Option|Future)";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.LocalMarketHours"/> class and its consumers or related classes
        /// </summary>
        public static class LocalMarketHours
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.LocalMarketHours instance)
            {
                if (instance.IsClosedAllDay)
                {
                    return "Closed All Day";
                }

                if (instance.IsOpenAllDay)
                {
                    return "Open All Day";
                }

                return Invariant($"{instance.DayOfWeek}: {string.Join(" | ", instance.Segments)}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.MaintenanceMarginParameters"/> class and its consumers or related classes
        /// </summary>
        public static class MaintenanceMarginParameters
        {
            public static string ForUnderlyingOnlyInvokableForIDerivativeSecurity =
                "ForUnderlying is only invokable for IDerivativeSecurity (Option|Future)";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.MarketHoursDatabase"/> class and its consumers or related classes
        /// </summary>
        public static class MarketHoursDatabase
        {
            public static string FutureUsaMarketTypeNoLongerSupported =
                "Future.Usa market type is no longer supported as we mapped each ticker to its actual exchange. " +
                "Please find your specific market in the symbol-properties database.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ExchangeHoursNotFound(Securities.SecurityDatabaseKey key,
                IEnumerable<Securities.SecurityDatabaseKey> availableKeys = null)
            {
                var keys = "";
                if (availableKeys != null)
                {
                    keys = " Available keys: " + string.Join(", ", availableKeys);
                }

                return $"Unable to locate exchange hours for {key}.{keys}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SuggestedMarketBasedOnTicker(string market)
            {
                return $"Suggested market based on the provided ticker 'Market.{market.ToUpperInvariant()}'.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.MarketHoursSegment"/> class and its consumers or related classes
        /// </summary>
        public static class MarketHoursSegment
        {
            public static string InvalidExtendedMarketOpenTime = "Extended market open time must be less than or equal to market open time.";

            public static string InvalidMarketCloseTime = "Market close time must be after market open time.";

            public static string InvalidExtendedMarketCloseTime = "Extended market close time must be greater than or equal to market close time.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.MarketHoursSegment instance)
            {
                return $"{instance.State}: {instance.Start.ToStringInvariant(null)}-{instance.End.ToStringInvariant(null)}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.RegisteredSecurityDataTypesProvider"/> class and its consumers or related classes
        /// </summary>
        public static class RegisteredSecurityDataTypesProvider
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TwoDifferentTypesDetectedForTheSameTypeName(Type type, Type existingType)
            {
                return $"Two different types were detected trying to register the same type name: {existingType} - {type}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.Security"/> class and its consumers or related classes
        /// </summary>
        public static class Security
        {
            public static string ValidSymbolPropertiesInstanceRequired = "Security requires a valid SymbolProperties instance.";

            public static string UnmatchingQuoteCurrencies = "symbolProperties.QuoteCurrency must match the quoteCurrency.Symbol";

            public static string SetLocalTimeKeeperMustBeCalledBeforeUsingLocalTime =
                "Security.SetLocalTimeKeeper(LocalTimeKeeper) must be called in order to use the LocalTime property.";

            public static string UnmatchingSymbols = "Symbols must match.";

            public static string UnmatchingExchangeTimeZones = "ExchangeTimeZones must match.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityDatabaseKey"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityDatabaseKey
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string KeyNotInExpectedFormat(string key)
            {
                return $"The specified key was not in the expected format: {key}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.SecurityDatabaseKey instance)
            {
                return Invariant($"{instance.SecurityType}-{instance.Market}-{instance.Symbol}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityDefinitionSymbolResolver"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityDefinitionSymbolResolver
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoSecurityDefinitionsLoaded(string securitiesDefinitionKey)
            {
                return $"No security definitions data loaded from file: {securitiesDefinitionKey}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityExchangeHours"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityExchangeHours
        {
            public static string UnableToLocateNextMarketOpenInTwoWeeks = "Unable to locate next market open within two weeks.";

            public static string UnableToLocateNextMarketCloseInTwoWeeks = "Unable to locate next market close within two weeks.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string LastMarketOpenNotFound(DateTime localDateTime, bool isMarketAlwaysOpen)
            {
                return $"Did not find last market open for {localDateTime}. IsMarketAlwaysOpen: {isMarketAlwaysOpen}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityHolding"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityHolding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.SecurityHolding instance)
            {
                return Invariant($"{instance.Symbol.Value}: {instance.Quantity} @ {instance.AveragePrice}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityManager"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityManager
        {
            /// <summary>
            /// Returns a string message saying the given symbol was not found in the user security list
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SymbolNotFoundInSecurities(QuantConnect.Symbol symbol)
            {
                return Invariant($@"This asset symbol ({
                    symbol}) was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(""{
                    QuantConnect.SymbolCache.GetTicker(symbol)}"")'");
            }

            /// <summary>
            /// Returns a string message saying the given symbol could not be overwritten
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToOverwriteSecurity(QuantConnect.Symbol symbol)
            {
                return Invariant($"Unable to overwrite existing Security: {symbol}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityPortfolioManager"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityPortfolioManager
        {
            /// <summary>
            /// Returns a string message saying Portfolio object is an adaptor for Security Manager and that to add a new asset
            /// the required data should added during initialization
            /// </summary>
            public static string DictionaryAddNotImplemented =
                "Portfolio object is an adaptor for Security Manager. To add a new asset add the required data during initialization.";

            /// <summary>
            /// Returns a string message saying the Portfolio object object is an adaptor for Security Manager and cannot be cleared
            /// </summary>
            public static string DictionaryClearNotImplemented = "Portfolio object is an adaptor for Security Manager and cannot be cleared.";

            /// <summary>
            /// Returns a string message saying the Portfolio object is an adaptor for Security Manager and objects cannot be removed
            /// </summary>
            public static string DictionaryRemoveNotImplemented = "Portfolio object is an adaptor for Security Manager and objects cannot be removed.";

            /// <summary>
            /// Returns a string message saying the AccountCurrency cannot be changed after adding a Security and that the method
            /// SetAccountCurrency() should be moved before AddSecurity()
            /// </summary>
            public static string CannotChangeAccountCurrencyAfterAddingSecurity =
                "Cannot change AccountCurrency after adding a Security. Please move SetAccountCurrency() before AddSecurity().";

            /// <summary>
            /// Returns a string message saying the AccountCurrency cannot be changed after setting cash and that the method
            /// SetAccountCurrency() should be moved before SetCash()
            /// </summary>
            public static string CannotChangeAccountCurrencyAfterSettingCash =
                "Cannot change AccountCurrency after setting cash. Please move SetAccountCurrency() before SetCash().";

            /// <summary>
            /// Returns a string message saying the AccountCurrency has already been set and that the new value for this property
            /// will be ignored
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string AccountCurrencyAlreadySet(Securities.CashBook cashBook, string newAccountCurrency)
            {
                return $"account currency has already been set to {cashBook.AccountCurrency}. Will ignore new value {newAccountCurrency}";
            }

            /// <summary>
            /// Returns a string message saying the AccountCurrency is being set to the given account currency
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SettingAccountCurrency(string accountCurrency)
            {
                return $"setting account currency to {accountCurrency}";
            }

            /// <summary>
            /// Returns a string message saying the total margin information, this is, the total margin used as well as the
            /// margin remaining
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TotalMarginInformation(decimal totalMarginUsed, decimal marginRemaining)
            {
                return Invariant($"Total margin information: TotalMarginUsed: {totalMarginUsed:F2}, MarginRemaining: {marginRemaining:F2}");
            }

            /// <summary>
            /// Returns a string message saying the order request margin information, this is, the margin used and the margin remaining
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderRequestMarginInformation(decimal marginUsed, decimal marginRemaining)
            {
                return Invariant($"Order request margin information: MarginUsed: {marginUsed:F2}, MarginRemaining: {marginRemaining:F2}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityService"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityService
        {
            /// <summary>
            /// Returns a string message saying the given Symbol could not be found in the Symbol Properties Database
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SymbolNotFoundInSymbolPropertiesDatabase(QuantConnect.Symbol symbol)
            {
                return $"Symbol could not be found in the Symbol Properties Database: {symbol.Value}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SecurityTransactionManager"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityTransactionManager
        {
            /// <summary>
            /// Returns a string message saying CancelOpenOrders operation is not allowed in Initialize or during warm up
            /// </summary>
            public static string CancelOpenOrdersNotAllowedOnInitializeOrWarmUp =
                "This operation is not allowed in Initialize or during warm up: CancelOpenOrders. Please move this code to the OnWarmupFinished() method.";

            /// <summary>
            /// Returns a string message saying the order was canceled by the CancelOpenOrders() at the given time
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderCanceledByCancelOpenOrders(DateTime time)
            {
                return Invariant($"Canceled by CancelOpenOrders() at {time:o}");
            }

            /// <summary>
            /// Returns a string message saying the ticket for the given order ID could not be localized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToLocateOrderTicket(int orderId)
            {
                return Invariant($"Unable to locate ticket for order: {orderId}");
            }

            /// <summary>
            /// Returns a string message saying the order did not fill within the given amount of seconds
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderNotFilledWithinExpectedTime(TimeSpan fillTimeout)
            {
                return Invariant($"Order did not fill within {fillTimeout.TotalSeconds} seconds.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SymbolProperties"/> class and its consumers or related classes
        /// </summary>
        public static class SymbolProperties
        {
            /// <summary>
            /// String message saying the SymbolProperties LotSize can not be less than or equal to 0
            /// </summary>
            public static string InvalidLotSize = "SymbolProperties LotSize can not be less than or equal to 0";

            /// <summary>
            /// String message saying the SymbolProperties PriceMagnifier can not be less than or equal to 0
            /// </summary>
            public static string InvalidPriceMagnifier = "SymbolProprties PriceMagnifier can not be less than or equal to 0";

            /// <summary>
            /// String message saying the SymbolProperties StrikeMultiplier can not be less than or equal to 0
            /// </summary>
            public static string InvalidStrikeMultiplier = "SymbolProperties StrikeMultiplier can not be less than or equal to 0";

            /// <summary>
            /// Parses a given SymbolProperties object into a string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Securities.SymbolProperties instance)
            {
                var marketTicker = ",";
                var minimumOrderSize = marketTicker;
                var priceMagnifier = marketTicker;
                if (!string.IsNullOrEmpty(instance.MarketTicker))
                {
                    marketTicker = $",{instance.MarketTicker}";
                }
                if (instance.MinimumOrderSize != null)
                {
                    minimumOrderSize = Invariant($",{instance.MinimumOrderSize}");
                }
                if (instance.PriceMagnifier != 1)
                {
                    priceMagnifier = Invariant($",{instance.PriceMagnifier}");
                }

                return Invariant($@"{instance.Description},{instance.QuoteCurrency},{instance.ContractMultiplier},{
                    instance.MinimumPriceVariation},{instance.LotSize}{marketTicker}{minimumOrderSize}{priceMagnifier}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Securities.SymbolPropertiesDatabase"/> class and its consumers or related classes
        /// </summary>
        public static class SymbolPropertiesDatabase
        {
            //public static string InvalidLotSize = "SymbolProperties LotSize can not be less than or equal to 0";

            /// <summary>
            /// Returns a string saying a duplicated key was found while processing the given file
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DuplicateKeyInFile(string file, Securities.SecurityDatabaseKey key)
            {
                return $"Encountered duplicate key while processing file: {file}. Key: {key}";
            }

            /// <summary>
            /// Returns a string saying the given symbol properties file could not be localized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DatabaseFileNotFound(string file)
            {
                return $"Unable to locate symbol properties file: {file}";
            }
        }
    }
}
