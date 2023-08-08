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
using System.Runtime.CompilerServices;

using Python.Runtime;

using QuantConnect.Interfaces;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="QuantConnect"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.AlphaRuntimeStatistics"/> class and its consumers or related classes
        /// </summary>
        public static class AlphaRuntimeStatistics
        {
            public static string ReturnOverMaximumDrawdownKey = "Return Over Maximum Drawdown";

            public static string PortfolioTurnoverKey = "Portfolio Turnover";

            public static string TotalInsightsGeneratedKey = "Total Insights Generated";

            public static string TotalInsightsClosedKey = "Total Insights Closed";

            public static string TotalInsightsAnalysisCompletedKey = "Total Insights Analysis Completed";

            public static string LongInsightCountKey = "Long Insight Count";

            public static string ShortInsightCountKey = "Short Insight Count";

            public static string LongShortRatioKey = "Long/Short Ratio";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Chart"/> class and its consumers or related classes
        /// </summary>
        public static class Chart
        {
            public static string ChartSeriesAlreadyExists = "Chart series name already exists";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.ChartPoint"/> class and its consumers or related classes
        /// </summary>
        public static class ChartPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(QuantConnect.ChartPoint instance)
            {
                return Invariant($"{instance.Time:o} - {instance.y}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Candlestick"/> class and its consumers or related classes
        /// </summary>
        public static class Candlestick
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(QuantConnect.Candlestick instance)
            {
                return Invariant($@"{instance.Time:o} - (O:{instance.Open} H: {instance.High} L: {
                    instance.Low} C: {instance.Close})");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Currencies"/> class and its consumers or related classes
        /// </summary>
        public static class Currencies
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedConversionToDecimal(string value)
            {
                return $"The value {value} cannot be converted to a decimal number";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.ExtendedDictionary{T}"/> class and its consumers or related classes
        /// </summary>
        public static class ExtendedDictionary
        {
            public static string ClearMethodNotImplemented = "Types deriving from 'ExtendedDictionary' must implement the 'void Clear() method.";

            public static string RemoveMethodNotImplemented =
                "Types deriving from 'ExtendedDictionary' must implement the 'void Remove(Symbol) method.";

            public static string IndexerBySymbolNotImplemented =
                "Types deriving from 'ExtendedDictionary' must implement the 'T this[Symbol] method.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ClearInvalidOperation<T>(ExtendedDictionary<T> instance)
            {
                return $"Clear/clear method call is an invalid operation. {instance.GetType().Name} is a read-only collection.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string RemoveInvalidOperation<T>(ExtendedDictionary<T> instance)
            {
                return $"Remove/pop method call is an invalid operation. {instance.GetType().Name} is a read-only collection.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TickerNotFoundInSymbolCache(string ticker)
            {
                return $"The ticker {ticker} was not found in the SymbolCache. Use the Symbol object as key instead. " +
                    "Accessing the securities collection/slice object by string ticker is only available for securities added with " +
                    "the AddSecurity-family methods. For more details, please check out the documentation.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string PopitemMethodNotSupported<T>(ExtendedDictionary<T> instance)
            {
                return $"popitem method is not supported for {instance.GetType().Name}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SymbolNotFoundDueToNoData<T>(ExtendedDictionary<T> instance, QuantConnect.Symbol symbol)
            {
                return $"'{symbol}' wasn't found in the {instance.GetType().Name} object, likely because there was no-data at this moment in " +
                    "time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with " +
                    $"data.ContainsKey(\"{symbol}\"). The collection is read-only, cannot set default.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UpdateInvalidOperation<T>(ExtendedDictionary<T> instance)
            {
                return $"update method call is an invalid operation. {instance.GetType().Name} is a read-only collection.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Extensions"/> class and its consumers or related classes
        /// </summary>
        public static class Extensions
        {
            public static string ErrorAdjustingSymbolByOffset =
                "Adjusting a symbol by an offset is currently only supported for non canonical futures";

            public static string NullDataProvider =
                $"The provided '{nameof(IDataProvider)}' instance is null. Are you missing some initialization step?";

            public static string NullOrEmptySourceToConvertToHexString = "Source cannot be null or empty.";

            public static string CreateOptionChainRequiresOptionSymbol = "CreateOptionChain requires an option symbol.";

            public static string CreateFutureChainRequiresFutureSymbol = "CreateFutureChain requires a future symbol.";

            public static string GreatestCommonDivisorEmptyList = "The list of values cannot be empty";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DownloadDataFailed(string url)
            {
                return $"failed for: '{url}'";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ZeroPriceForSecurity(QuantConnect.Symbol symbol)
            {
                return $"{symbol}: The security does not have an accurate price as it has not yet received a bar of data. " +
                    "Before placing a trade (or using SetHoldings) warm up your algorithm with SetWarmup, or use slice.Contains(symbol) " +
                    "to confirm the Slice object has price before using the data. Data does not necessarily all arrive at the same " +
                    "time so your algorithm should confirm the data is ready before using it. In live trading this can mean you do " +
                    "not have an active subscription to the asset class you're trying to trade. If using custom data make sure you've " +
                    "set the 'Value' property.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string WaitingForThreadToStopSafely(string threadName)
            {
                return $"Waiting for '{threadName}' thread to stop...";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TimeoutWaitingForThreadToStopSafely(string threadName)
            {
                return $"Timeout waiting for '{threadName}' thread to stop";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DataTypeMissingParameterlessConstructor(Type type)
            {
                return $"Data type '{type.Name}' missing parameterless constructor. E.g. public {type.Name}() {{ }}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToCreateInstanceOfType(Type type)
            {
                return $"Failed to create instance of type '{type.Name}'";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TypeIsNotBaseData(Type type)
            {
                return $"Data type '{type.Name}' does not inherit required {nameof(Data.BaseData)}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CannotCastNonFiniteFloatingPointValueToDecimal(double input)
            {
                return Invariant($@"It is not possible to cast a non-finite floating-point value ({
                    input}) as decimal. Please review math operations and verify the result is valid.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToConvertTimeSpanToResolution(TimeSpan timeSpan)
            {
                return Invariant($"Unable to exactly convert time span ('{timeSpan}') to resolution.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToParseUnknownSecurityType(string value)
            {
                return $"Attempted to parse unknown SecurityType: {value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoDefaultOptionStyleForSecurityType(SecurityType securityType)
            {
                return Invariant($"The SecurityType {securityType} has no default OptionStyle, because it has no options available for it");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionStyle(string value)
            {
                return $"Unexpected OptionStyle: {value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionStyle(OptionStyle value)
            {
                return $"Unexpected OptionStyle: {value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionRight(string value)
            {
                return $"Unexpected OptionRight: {value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionRight(OptionRight value)
            {
                return $"Unexpected OptionRight: {value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownDataMappingMode(string value)
            {
                return $"Unexpected DataMappingMode: {value}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToDelegateCannotConverPyObjectToType(string methodName, Type type)
            {
                return $"{methodName} cannot be used to convert a PyObject into {type}.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToDictionaryFailed(string sourceType, string targetType, string reason)
            {
                return $"ConvertToDictionary cannot be used to convert a {sourceType} into {targetType}. Reason: {reason}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToSymbolEnumerableFailed(PyObject item)
            {
                return $"Argument type should be Symbol or a list of Symbol. Object: {item}. Type: {item.GetPythonType()}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ObjectFromPythonIsNotACSharpType(string objectRepr)
            {
                return $"{objectRepr} is not a C# Type.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string RuntimeError(IAlgorithm algorithm, string context)
            {
                return Invariant($"RuntimeError at {algorithm.UtcTime} UTC. Context: {context}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Holding"/> class and its consumers or related classes
        /// </summary>
        public static class Holding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(QuantConnect.Holding instance)
            {
                var value = Invariant($@"{instance.Symbol.Value}: {instance.Quantity} @ {
                    instance.CurrencySymbol}{instance.AveragePrice} - Market: {instance.CurrencySymbol}{instance.MarketPrice}");

                if (instance.ConversionRate != 1m)
                {
                    value += Invariant($" - Conversion: {instance.ConversionRate}");
                }

                return value;
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.AlgorithmControl"/> class and its consumers or related classes
        /// </summary>
        public static class AlgorithmControl
        {
            public static string ChartSubscription = "Strategy Equity";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Isolator"/> class and its consumers or related classes
        /// </summary>
        public static class Isolator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageMaxedOut(string memoryCap, string lastSample)
            {
                return $"Execution Security Error: Memory Usage Maxed Out - {memoryCap}MB max, with last sample of {lastSample}MB.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageOver80Percent(double lastSample)
            {
                return Invariant($"Execution Security Error: Memory usage over 80% capacity. Sampled at {lastSample}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageInfo(string memoryUsed, string lastSample, string memoryUsedByApp, TimeSpan currentTimeStepElapsed,
                int cpuUsage)
            {
                return Invariant($@"Used: {memoryUsed}, Sample: {lastSample}, App: {memoryUsedByApp}, CurrentTimeStepElapsed: {
                    currentTimeStepElapsed:mm':'ss'.'fff}. CPU: {cpuUsage}%");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageMonitorTaskTimedOut(TimeSpan timeout)
            {
                return $@"Execution Security Error: Operation timed out - {
                    timeout.TotalMinutes.ToStringInvariant()} minutes max. Check for recursive loops.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Market"/> class and its consumers or related classes
        /// </summary>
        public static class Market
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidMarketIdentifier(int maxMarketIdentifier)
            {
                return $"The market identifier is limited to positive values less than {maxMarketIdentifier.ToStringInvariant()}.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TriedToAddExistingMarketWithDifferentIdentifier(string market)
            {
                return $"Attempted to add an already added market with a different identifier. Market: {market}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TriedToAddExistingMarketIdentifier(string market, string existingMarket)
            {
                return $"Attempted to add a market identifier that is already in use. New Market: {market} Existing Market: {existingMarket}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.OS"/> class and its consumers or related classes
        /// </summary>
        public static class OS
        {
            public static string CPUUsageKey = "CPU Usage";
            public static string UsedRAMKey = "Used RAM (MB)";
            public static string TotalRAMKey = "Total RAM (MB)";
            public static string HostnameKey = "Hostname";
            public static string LEANVersionKey = "LEAN Version";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Parse"/> class and its consumers or related classes
        /// </summary>
        public static class Parse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ValueIsNotParseable(string input, Type targetType)
            {
                return $"The provided value ({input}) was not parseable as {targetType.Name}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.SecurityIdentifier"/> class and its consumers or related classes
        /// </summary>
        public static class SecurityIdentifier
        {
            public static string NoUnderlyingForIdentifier =
                "No underlying specified for this identifier. Check that HasUnderlying is true before accessing the Underlying property.";

            public static string DateNotSupportedBySecurityType =
                "Date is only defined for SecurityType.Equity, SecurityType.Option, SecurityType.Future, SecurityType.FutureOption, SecurityType.IndexOption, and SecurityType.Base";

            public static string StrikePriceNotSupportedBySecurityType =
                "StrikePrice is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption";

            public static string OptionRightNotSupportedBySecurityType =
                "OptionRight is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption";

            public static string OptionStyleNotSupportedBySecurityType =
                "OptionStyle is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption";

            public static string NullSymbol = "SecurityIdentifier requires a non-null string 'symbol'";

            public static string SymbolWithInvalidCharacters = "Symbol must not contain the characters '|' or ' '.";

            public static string PropertiesDoNotMatchAnySecurityType = $"The provided properties do not match with a valid {nameof(SecurityType)}";

            public static string StringIsNotSplittable = "The string must be splittable on space into two parts.";

            public static string UnexpectedTypeToCompareTo = $"Object must be of type {nameof(SecurityIdentifier)}";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityType(string parameterName)
            {
                return $"{parameterName} must be between 0 and 99";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOptionRight(string parameterName)
            {
                return $"{parameterName} must be either 0 or 1";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidStrikePrice(decimal strikePrice)
            {
                return Invariant($"The specified strike price's precision is too high: {strikePrice}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ErrorParsingSecurityIdentifier(string value, Exception exception)
            {
                return Invariant($"Error parsing SecurityIdentifier: '{value}', Exception: {exception}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MarketNotFound(string market)
            {
                return $@"The specified market wasn't found in the markets lookup. Requested: {
                    market}. You can add markets by calling QuantConnect.Market.Add(string,int)";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.StringExtensions"/> class and its consumers or related classes
        /// </summary>
        public static class StringExtensions
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertInvariantCannotConvertTo(TypeCode targetTypeCode)
            {
                return $"StringExtensions.ConvertInvariant does not support converting to TypeCode.{targetTypeCode}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Symbol"/> class and its consumers or related classes
        /// </summary>
        public static class Symbol
        {
            public static string InsufficientInformationToCreateFutureOptionSymbol =
                "Cannot create future option Symbol using this method (insufficient information). Use `CreateOption(Symbol, ...)` instead.";

            public static string CanonicalNotDefined =
                "Canonical is only defined for SecurityType.Option, SecurityType.Future, SecurityType.FutureOption";

            public static string UnexpectedObjectTypeToCompareTo = "Object must be of type Symbol or string.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityTypeNotImplementedYet(SecurityType securityType)
            {
                return Invariant($"The security type has not been implemented yet: {securityType}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityTypeCannotBeMapped(SecurityType securityType)
            {
                return Invariant($"SecurityType {securityType} can not be mapped.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoOptionTypeForUnderlying(SecurityType securityType)
            {
                return Invariant($"No option type exists for underlying SecurityType: {securityType}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoUnderlyingForOption(SecurityType securityType)
            {
                return Invariant($"No underlying type exists for option SecurityType: {securityType}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.SymbolCache"/> class and its consumers or related classes
        /// </summary>
        public static class SymbolCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToLocateTicker(string ticker)
            {
                return $"We were unable to locate the ticker '{ticker}'.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MultipleMatchingTickersLocated(IEnumerable<string> tickers)
            {
                return "We located multiple potentially matching tickers. " +
                    "For custom data, be sure to append a dot followed by the custom data type name. " +
                    $"For example: 'BTC.Bitcoin'. Potential Matches: {string.Join(", ", tickers)}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.SymbolRepresentation"/> class and its consumers or related classes
        /// </summary>
        public static class SymbolRepresentation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToGetMarketForTickerAndUnderlying(string ticker, string underlying)
            {
                return $"Failed to get market for future '{ticker}' and underlying '{underlying}'";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoMarketFound(string ticker)
            {
                return $"No market found for '{ticker}'";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedSecurityTypeForMethod(string methodName, SecurityType securityType)
            {
                return Invariant($"{methodName} expects symbol to be an option, received {securityType}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedOptionRightFormatForParseOptionTickerOSI(string ticker)
            {
                return $"Expected 12th character to be 'C' or 'P' for OptionRight: {ticker} but was '{ticker[12]}'";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityTypeNotImplemented(SecurityType securityType)
            {
                return Invariant($"Security type {securityType} not implemented");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.SymbolValueJsonConverter"/> class and its consumers or related classes
        /// </summary>
        public static class SymbolValueJsonConverter
        {
            public static string ConverterIsWriteOnly = "The SymbolValueJsonConverter is write-only.";

            public static string ConverterIsIntendedToBeDirectlyDecoratedInMember =
                "The SymbolValueJsonConverter is intended to be decorated on the appropriate member directly.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Time"/> class and its consumers or related classes
        /// </summary>
        public static class Time
        {
            public static string InvalidBarSize = "barSize must be greater than TimeSpan.Zero";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityCount(int count)
            {
                return $"Security Count: {count}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.TradingCalendar"/> class and its consumers or related classes
        /// </summary>
        public static class TradingCalendar
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidTotalDays(int totalDays)
            {
                return Invariant($@"Total days is negative ({
                    totalDays
                    }), indicating reverse start and end times. Check your usage of TradingCalendar to ensure proper arrangement of variables");
            }
        }
    }
}
