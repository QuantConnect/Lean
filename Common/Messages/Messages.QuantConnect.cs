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
        /// Provides user-facing messages for the <see cref="AlphaRuntimeStatistics"/> class and its consumers or related classes
        /// </summary>
        public static class AlphaRuntimeStatistics
        {
            /// <summary>
            /// Returns a string message saying: Return Over Maximum Drawdown
            /// </summary>
            public static string ReturnOverMaximumDrawdownKey = "Return Over Maximum Drawdown";

            /// <summary>
            /// Returns a string message saying: Portfolio Turnover
            /// </summary>
            public static string PortfolioTurnoverKey = "Portfolio Turnover";

            /// <summary>
            /// Returns a string message saying: Total Insights Generated
            /// </summary>
            public static string TotalInsightsGeneratedKey = "Total Insights Generated";

            /// <summary>
            /// Returns a string message saying: Total Insights Closed
            /// </summary>
            public static string TotalInsightsClosedKey = "Total Insights Closed";

            /// <summary>
            /// Returns a string message saying: Total Insights Analysis Completed
            /// </summary>
            public static string TotalInsightsAnalysisCompletedKey = "Total Insights Analysis Completed";

            /// <summary>
            /// Returns a string message saying: Long Insight Count
            /// </summary>
            public static string LongInsightCountKey = "Long Insight Count";

            /// <summary>
            /// Returns a string message saying: Short Insight Count
            /// </summary>
            public static string ShortInsightCountKey = "Short Insight Count";

            /// <summary>
            /// Returns a string message saying: Long/Short Ratio
            /// </summary>
            public static string LongShortRatioKey = "Long/Short Ratio";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Chart"/> class and its consumers or related classes
        /// </summary>
        public static class Chart
        {
            /// <summary>
            /// Returns a string message saying Chart series name already exists
            /// </summary>
            public static string ChartSeriesAlreadyExists = "Chart series name already exists";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.ChartPoint"/> class and its consumers or related classes
        /// </summary>
        public static class ChartPoint
        {
            /// <summary>
            /// Parses a given ChartPoint object into a string message
            /// </summary>
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
            /// <summary>
            /// Parses a given Candlestick object into a string message
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying the given value cannot be converted to a decimal number
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying the types deriving from ExtendedDictionary must implement the void Clear() method
            /// </summary>
            public static string ClearMethodNotImplemented = "Types deriving from 'ExtendedDictionary' must implement the 'void Clear() method.";

            /// <summary>
            /// Returns a string message saying the types deriving from ExtendedDictionary must implement the void Remove(Symbol) method
            /// </summary>
            public static string RemoveMethodNotImplemented =
                "Types deriving from 'ExtendedDictionary' must implement the 'void Remove(Symbol) method.";

            /// <summary>
            /// Returns a string message saying the types deriving from ExtendedDictionary must implement the T this[Symbol] method
            /// </summary>
            public static string IndexerBySymbolNotImplemented =
                "Types deriving from 'ExtendedDictionary' must implement the 'T this[Symbol] method.";

            /// <summary>
            /// Returns a string message saying Clear/clear method call is an invalid operation. It also says that the given instance
            /// is a read-only collection
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ClearInvalidOperation<T>(ExtendedDictionary<T> instance)
            {
                return $"Clear/clear method call is an invalid operation. {instance.GetType().Name} is a read-only collection.";
            }

            /// <summary>
            /// Returns a string message saying that Remove/pop call method is an invalid operation. It also says that the given instance
            /// is a read-only collection
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string RemoveInvalidOperation<T>(ExtendedDictionary<T> instance)
            {
                return $"Remove/pop method call is an invalid operation. {instance.GetType().Name} is a read-only collection.";
            }

            /// <summary>
            /// Returns a string message saying that the given ticker was not found in the SymbolCache. It also gives a recommendation
            /// for solving this problem
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TickerNotFoundInSymbolCache(string ticker)
            {
                return $"The ticker {ticker} was not found in the SymbolCache. Use the Symbol object as key instead. " +
                    "Accessing the securities collection/slice object by string ticker is only available for securities added with " +
                    "the AddSecurity-family methods. For more details, please check out the documentation.";
            }

            /// <summary>
            /// Returns a string message saying that the popitem method is not supported for the given instance
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string PopitemMethodNotSupported<T>(ExtendedDictionary<T> instance)
            {
                return $"popitem method is not supported for {instance.GetType().Name}";
            }

            /// <summary>
            /// Returns a string message saying that the given symbol wasn't found in the give instance object. It also shows
            /// a recommendation for solving this problem
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SymbolNotFoundDueToNoData<T>(ExtendedDictionary<T> instance, QuantConnect.Symbol symbol)
            {
                return $"'{symbol}' wasn't found in the {instance.GetType().Name} object, likely because there was no-data at this moment in " +
                    "time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with " +
                    $"data.ContainsKey(\"{symbol}\"). The collection is read-only, cannot set default.";
            }

            /// <summary>
            /// Returns a string message saying the update method call is an invalid operation. It also mentions that the given
            /// instance is a read-only collection
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying adjusting a symbol by an offset is currently only supported for non canonical futures
            /// </summary>
            public static string ErrorAdjustingSymbolByOffset =
                "Adjusting a symbol by an offset is currently only supported for non canonical futures";

            /// <summary>
            /// Returns a string message saying the provided DataProvider instance is null
            /// </summary>
            public static string NullDataProvider =
                $"The provided '{nameof(IDataProvider)}' instance is null. Are you missing some initialization step?";

            /// <summary>
            /// Returns a string message saying the source cannot be null or empty
            /// </summary>
            public static string NullOrEmptySourceToConvertToHexString = "Source cannot be null or empty.";

            /// <summary>
            /// Returns a string message saying the CreateOptionChain method requires an option symbol
            /// </summary>
            public static string CreateOptionChainRequiresOptionSymbol = "CreateOptionChain requires an option symbol.";

            /// <summary>
            /// Returns a string message saying the CreateFutureChain method requires a future symbol
            /// </summary>
            public static string CreateFutureChainRequiresFutureSymbol = "CreateFutureChain requires a future symbol.";

            /// <summary>
            /// Returns a string message saying the list of values cannot be empty
            /// </summary>
            public static string GreatestCommonDivisorEmptyList = "The list of values cannot be empty";

            /// <summary>
            /// Returns a string message saying the process of downloading data from the given url failed
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DownloadDataFailed(string url)
            {
                return $"failed for: '{url}'";
            }

            /// <summary>
            /// Returns a string message saying the security does not have an accurate price as it has not yet received
            /// a bar of data, as well as some recommendations
            /// </summary>
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

            /// <summary>
            /// Returns a string message saying: Waiting for the given thread to stop
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string WaitingForThreadToStopSafely(string threadName)
            {
                return $"Waiting for '{threadName}' thread to stop...";
            }

            /// <summary>
            /// Returns a string message saying: Timeout waiting for the given thread to stop
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TimeoutWaitingForThreadToStopSafely(string threadName)
            {
                return $"Timeout waiting for '{threadName}' thread to stop";
            }

            /// <summary>
            /// Returns a string message saying the given data type is missing a parameterless constructor
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DataTypeMissingParameterlessConstructor(Type type)
            {
                return $"Data type '{type.Name}' missing parameterless constructor. E.g. public {type.Name}() {{ }}";
            }

            /// <summary>
            /// Returns a string message saying the process of creating an instance of the given type failed
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToCreateInstanceOfType(Type type)
            {
                return $"Failed to create instance of type '{type.Name}'";
            }

            /// <summary>
            /// Returns a string message saying the given data type does not inherit the required BaseData
            /// methods and/or attributes
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TypeIsNotBaseData(Type type)
            {
                return $"Data type '{type.Name}' does not inherit required {nameof(Data.BaseData)}";
            }

            /// <summary>
            /// Returns a string message saying it is impossible to cast the given non-finite floating-point value
            /// as a decimal
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CannotCastNonFiniteFloatingPointValueToDecimal(double input)
            {
                return Invariant($@"It is not possible to cast a non-finite floating-point value ({
                    input}) as decimal. Please review math operations and verify the result is valid.");
            }

            /// <summary>
            /// Returns a string message saying it was not able to exactly convert the given time span to resolution
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToConvertTimeSpanToResolution(TimeSpan timeSpan)
            {
                return Invariant($"Unable to exactly convert time span ('{timeSpan}') to resolution.");
            }

            /// <summary>
            /// Returns a string message saying it was attempted to parse the given unknown security type
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToParseUnknownSecurityType(string value)
            {
                return $"Attempted to parse unknown SecurityType: {value}";
            }

            /// <summary>
            /// Returns a string message saying the given security type has no default OptionStyle, because it has no options
            /// available for it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoDefaultOptionStyleForSecurityType(SecurityType securityType)
            {
                return Invariant($"The SecurityType {securityType} has no default OptionStyle, because it has no options available for it");
            }

            /// <summary>
            /// Returns a string message saying the given OptionStyle was unexpected/unknown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionStyle(string value)
            {
                return $"Unexpected OptionStyle: {value}";
            }

            /// <summary>
            /// Returns a string message saying the given OptionStyle was unexpected/unknown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionStyle(OptionStyle value)
            {
                return $"Unexpected OptionStyle: {value}";
            }

            /// <summary>
            /// Returns a string message saying the given OptionRight was unexpected/unknown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionRight(string value)
            {
                return $"Unexpected OptionRight: {value}";
            }

            /// <summary>
            /// Returns a string message saying the given OptionRight was unexpected/unknown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownOptionRight(OptionRight value)
            {
                return $"Unexpected OptionRight: {value}";
            }

            /// <summary>
            /// Returns a string message saying the given DataMappingMode was unexpected/unknown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnknownDataMappingMode(string value)
            {
                return $"Unexpected DataMappingMode: {value}";
            }

            /// <summary>
            /// Returns a string message saying the given method cannot be used to convert a PyObject into the given type
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToDelegateCannotConverPyObjectToType(string methodName, Type type)
            {
                return $"{methodName} cannot be used to convert a PyObject into {type}.";
            }

            /// <summary>
            /// Returns a string message saying the method ConvertToDictionary cannot be used to convert a given source
            /// type into another given target type. It also specifies the reason.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToDictionaryFailed(string sourceType, string targetType, string reason)
            {
                return $"ConvertToDictionary cannot be used to convert a {sourceType} into {targetType}. Reason: {reason}";
            }

            /// <summary>
            /// Returns a string message saying the given argument type should Symbol or a list of Symbol. It also
            /// shows the given item as well as its Python type
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToSymbolEnumerableFailed(PyObject item)
            {
                return $"Argument type should be Symbol or a list of Symbol. Object: {item}. Type: {item.GetPythonType()}";
            }

            /// <summary>
            /// Returns a string message saying the given object is not a C# type
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ObjectFromPythonIsNotACSharpType(string objectRepr)
            {
                return $"{objectRepr} is not a C# Type.";
            }

            /// <summary>
            /// Returns a string message saying there was a RuntimeError at a given time in UTC. It also
            /// shows the given context
            /// </summary>
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
            /// <summary>
            /// Parses a Holding object into a string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(QuantConnect.Holding instance)
            {
                var currencySymbol = instance.CurrencySymbol;
                if (string.IsNullOrEmpty(currencySymbol))
                {
                    currencySymbol = "$";
                }
                var value = Invariant($@"{instance.Symbol?.Value}: {instance.Quantity} @ {
                    currencySymbol}{instance.AveragePrice} - Market: {currencySymbol}{instance.MarketPrice}");

                if (instance.ConversionRate.HasValue && instance.ConversionRate != 1m)
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
            /// <summary>
            /// Returns a string message saying: Strategy Equity
            /// </summary>
            public static string ChartSubscription = "Strategy Equity";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Isolator"/> class and its consumers or related classes
        /// </summary>
        public static class Isolator
        {
            /// <summary>
            /// Returns a string message saying: Execution Security Error: Memory Usage Maxed out, with the max memory capacity
            /// and a last sample of the usage
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageMaxedOut(string memoryCap, string lastSample)
            {
                return $"Execution Security Error: Memory Usage Maxed Out - {memoryCap}MB max, with last sample of {lastSample}MB.";
            }

            /// <summary>
            /// Returns a string message saying: Execution Security Error: Memory usage over 80% capacity, and the last sample taken
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageOver80Percent(double lastSample)
            {
                return Invariant($"Execution Security Error: Memory usage over 80% capacity. Sampled at {lastSample}");
            }

            /// <summary>
            /// Returns a string message with useful information about the memory usage, such us the memory used, the last sample
            /// the current memory used by the given app and the CPU usage
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MemoryUsageInfo(string memoryUsed, string lastSample, string memoryUsedByApp, TimeSpan currentTimeStepElapsed,
                int cpuUsage)
            {
                return Invariant($@"Used: {memoryUsed}, Sample: {lastSample}, App: {memoryUsedByApp}, CurrentTimeStepElapsed: {
                    currentTimeStepElapsed:mm':'ss'.'fff}. CPU: {cpuUsage}%");
            }

            /// <summary>
            /// Returns a string message saying: Execution Security Error: Operation timed out, with the maximum amount of minutes
            /// allowed
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying the market identifier is limited to positive values less than the given maximum market identifier
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidMarketIdentifier(int maxMarketIdentifier)
            {
                return $"The market identifier is limited to positive values less than {maxMarketIdentifier.ToStringInvariant()}.";
            }

            /// <summary>
            /// Returns a string message saying it was attempted to add an already added market with a different identifier
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TriedToAddExistingMarketWithDifferentIdentifier(string market)
            {
                return $"Attempted to add an already added market with a different identifier. Market: {market}";
            }

            /// <summary>
            /// Returns a string message saying it was attempted to add a market identifier that is already in use
            /// </summary>
            /// <returns></returns>
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
            /// <summary>
            /// CPU Usage string
            /// </summary>
            public static string CPUUsageKey = "CPU Usage";

            /// <summary>
            /// Used RAM (MB) string
            /// </summary>
            public static string UsedRAMKey = "Used RAM (MB)";

            /// <summary>
            /// Total RAM (MB) string
            /// </summary>
            public static string TotalRAMKey = "Total RAM (MB)";

            /// <summary>
            /// Hostname string
            /// </summary>
            public static string HostnameKey = "Hostname";

            /// <summary>
            /// LEAN Version string
            /// </summary>
            public static string LEANVersionKey = "LEAN Version";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Parse"/> class and its consumers or related classes
        /// </summary>
        public static class Parse
        {
            /// <summary>
            /// Returns a string message saying the provided input was not parseable as the given target type
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying no underlying was specified for certain identifier
            /// </summary>
            public static string NoUnderlyingForIdentifier =
                "No underlying specified for this identifier. Check that HasUnderlying is true before accessing the Underlying property.";

            /// <summary>
            /// Returns a string message saying Date is only defined for SecurityType.Equity, SecurityType.Option, SecurityType.Future, SecurityType.FutureOption, SecurityType.IndexOption, and SecurityType.Base
            /// </summary>
            public static string DateNotSupportedBySecurityType =
                "Date is only defined for SecurityType.Equity, SecurityType.Option, SecurityType.Future, SecurityType.FutureOption, SecurityType.IndexOption, and SecurityType.Base";

            /// <summary>
            /// Returns a string message saying StrikePrice is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption
            /// </summary>
            public static string StrikePriceNotSupportedBySecurityType =
                "StrikePrice is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption";

            /// <summary>
            /// Returns a string message saying OptionRight is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption
            /// </summary>
            public static string OptionRightNotSupportedBySecurityType =
                "OptionRight is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption";

            /// <summary>
            /// Returns a string message saying OptionStyle is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption
            /// </summary>
            public static string OptionStyleNotSupportedBySecurityType =
                "OptionStyle is only defined for SecurityType.Option, SecurityType.FutureOption, and SecurityType.IndexOption";

            /// <summary>
            /// Returns a string message saying SecurityIdentifier requires a non-null string 'symbol'
            /// </summary>
            public static string NullSymbol = "SecurityIdentifier requires a non-null string 'symbol'";

            /// <summary>
            /// Returns a string message saying Symbol must not contain the characters '|' or ' '
            /// </summary>
            public static string SymbolWithInvalidCharacters = "Symbol must not contain the characters '|' or ' '.";

            /// <summary>
            /// Returns a string message saying the provided properties do not match with a valid SecurityType
            /// </summary>
            public static string PropertiesDoNotMatchAnySecurityType = $"The provided properties do not match with a valid {nameof(SecurityType)}";

            /// <summary>
            /// Returns a string message saying the string must be splittable on space into two parts
            /// </summary>
            public static string StringIsNotSplittable = "The string must be splittable on space into two parts.";

            /// <summary>
            /// Returns a string message saying object must be of type SecurityIdentifier
            /// </summary>
            public static string UnexpectedTypeToCompareTo = $"Object must be of type {nameof(SecurityIdentifier)}";

            /// <summary>
            /// Returns a string message saying the given parameter must be between 0 and 99
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidSecurityType(string parameterName)
            {
                return $"{parameterName} must be between 0 and 99";
            }

            /// <summary>
            /// Returns a string message saying the given parameter must be either 0 or 1
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOptionRight(string parameterName)
            {
                return $"{parameterName} must be either 0 or 1";
            }

            /// <summary>
            /// Returns a string message saying the specified strike price's precision is too high
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidStrikePrice(decimal strikePrice)
            {
                return Invariant($"The specified strike price's precision is too high: {strikePrice}");
            }

            /// <summary>
            /// Returns a string message saying there was an error parsing SecurityIdentifier. It also says the given error and exception
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ErrorParsingSecurityIdentifier(string value, Exception exception)
            {
                return Invariant($"Error parsing SecurityIdentifier: '{value}', Exception: {exception}");
            }

            /// <summary>
            /// Returns a string message saying the given market could not be found in the markets lookup
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying StringExtensinos.ConvertInvariant does not support converting to the given TypeCode
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying there is insufficient information for creating certain future option symbol
            /// </summary>
            public static string InsufficientInformationToCreateFutureOptionSymbol =
                "Cannot create future option Symbol using this method (insufficient information). Use `CreateOption(Symbol, ...)` instead.";

            /// <summary>
            /// Returns a string message saying Canonical is only defined for SecurityType.Option, SecurityType.Future, SecurityType.FutureOption
            /// </summary>
            public static string CanonicalNotDefined =
                "Canonical is only defined for SecurityType.Option, SecurityType.Future, SecurityType.FutureOption";

            /// <summary>
            /// Returns a string message saying certain object must be of type Symbol or string
            /// </summary>
            public static string UnexpectedObjectTypeToCompareTo = "Object must be of type Symbol or string.";

            /// <summary>
            /// Returns a string message saying the given security type has not been implemented yet
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityTypeNotImplementedYet(SecurityType securityType)
            {
                return Invariant($"The security type has not been implemented yet: {securityType}");
            }

            /// <summary>
            /// Returns a string message saying the given security can not be mapped
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityTypeCannotBeMapped(SecurityType securityType)
            {
                return Invariant($"SecurityType {securityType} can not be mapped.");
            }

            /// <summary>
            /// Returns a string message saying no option type exists for the given underlying SecurityType
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoOptionTypeForUnderlying(SecurityType securityType)
            {
                return Invariant($"No option type exists for underlying SecurityType: {securityType}");
            }

            /// <summary>
            /// Returns a string message saying no underlying type exists for the given option SecurityType
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoUnderlyingForOption(SecurityType securityType)
            {
                return Invariant($"No underlying type exists for option SecurityType: {securityType}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SidNotForOption(QuantConnect.SecurityIdentifier sid)
            {
                return Invariant($"The provided SecurityIdentifier is not for an option: {sid}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnderlyingSidDoesNotMatch(QuantConnect.SecurityIdentifier sid, QuantConnect.Symbol underlying)
            {
                return Invariant($"The provided SecurityIdentifier does not match the underlying symbol: {sid} != {underlying.ID}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.SymbolCache"/> class and its consumers or related classes
        /// </summary>
        public static class SymbolCache
        {
            /// <summary>
            /// Returns a string message saying the given ticker could not be localized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToLocateTicker(string ticker)
            {
                return $"We were unable to locate the ticker '{ticker}'.";
            }

            /// <summary>
            /// Returns a string message saying mutiple potentially matching tickers were localized
            /// </summary>
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
            /// <summary>
            /// Returns a string message saying SymbolRepresentation failed to get market for the given ticker and underlying
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToGetMarketForTickerAndUnderlying(string ticker, string underlying)
            {
                return $"Failed to get market for future '{ticker}' and underlying '{underlying}'";
            }

            /// <summary>
            /// Returns a string message saying no market was found for the given ticker
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoMarketFound(string ticker)
            {
                return $"No market found for '{ticker}'";
            }

            /// <summary>
            /// Returns a string message saying an unexpected security type was received by the given method name
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedSecurityTypeForMethod(string methodName, SecurityType securityType)
            {
                return Invariant($"{methodName} expects symbol to be an option, received {securityType}.");
            }

            /// <summary>
            /// Returns a string message saying the given ticker is not in the expected OSI format
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidOSITickerFormat(string ticker)
            {
                return $"Invalid ticker format {ticker}";
            }

            /// <summary>
            /// Returns a string message saying the given security type is not implemented
            /// </summary>
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
            /// <summary>
            /// String message saying converter is write only
            /// </summary>
            public static string ConverterIsWriteOnly = "The SymbolValueJsonConverter is write-only.";

            /// <summary>
            /// String message saying converter is intended to be directly decorated in member
            /// </summary>
            public static string ConverterIsIntendedToBeDirectlyDecoratedInMember =
                "The SymbolValueJsonConverter is intended to be decorated on the appropriate member directly.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="QuantConnect.Time"/> class and its consumers or related classes
        /// </summary>
        public static class Time
        {
            /// <summary>
            /// Invalid Bar Size string message
            /// </summary>
            public static string InvalidBarSize = "barSize must be greater than TimeSpan.Zero";

            /// <summary>
            /// Returns a string message containing the number of securities
            /// </summary>
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
            /// <summary>
            /// Returns a string message for invalid total days
            /// </summary>
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
