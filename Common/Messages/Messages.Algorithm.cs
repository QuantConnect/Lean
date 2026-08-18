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

using System.Runtime.CompilerServices;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Algorithm"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Algorithm.QCAlgorithm"/> class and its consumers or related classes
        /// </summary>
        public static class QCAlgorithm
        {
            /// <summary>
            /// Returns a string message saying the time zone cannot be changed after the algorithm is running
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetTimeZoneAlreadyRunning()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetTimeZone")}(): Cannot change time zone after algorithm running.";
            }

            /// <summary>
            /// Returns a string message saying the benchmark cannot be changed after the algorithm is initialized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetBenchmarkAlreadyInitialized()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetBenchmark")}(): Cannot change Benchmark after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the account currency cannot be changed after the algorithm is initialized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetAccountCurrencyAlreadyInitialized()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetAccountCurrency")}(): Cannot change AccountCurrency after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the cash cannot be changed after the algorithm is initialized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetCashAlreadyInitialized()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetCash")}(): Cannot change cash available after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the start date cannot be changed after the algorithm is initialized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetStartDateAlreadyInitialized()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetStartDate")}(): Cannot change start date after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the end date cannot be changed after the algorithm is initialized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetEndDateAlreadyInitialized()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetEndDate")}(): Cannot change end date after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying SetWarmup cannot be used after the algorithm is initialized
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SetWarmupAlreadyInitialized()
            {
                return $"{AlgorithmPrefix()}.{FormatCode("SetWarmup")}(): This method cannot be used after algorithm initialized";
            }

            /// <summary>
            /// Returns a string message saying the given canonical symbol is not tradable, with guidance
            /// on what to trade instead
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CanonicalSymbolNotTradable(QuantConnect.Symbol symbol)
            {
                var guidance = symbol.SecurityType == SecurityType.Future
                    ? $"trade the currently mapped contract instead, accessible through the '{FormatCode("Mapped")}' property of the future " +
                      $"security returned by {AlgorithmPrefix()}.{FormatCode("AddFuture")}(). Note it is not set until after {FormatCode("Initialize")}, " +
                      "once the continuous contract universe makes its first selection"
                    : "select a specific contract from the chain instead";
                return $"The symbol '{symbol}' is a canonical symbol and is not tradable; {guidance}.";
            }

            /// <summary>
            /// Returns a string message for order methods receiving a null symbol, explaining the common cause:
            /// accessing <see cref="Securities.Future.Future.Mapped"/> before the first continuous contract mapping
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OrderSymbolNull()
            {
                return $"The order symbol is null. If it comes from the '{FormatCode("Mapped")}' property of a future, note it is not set " +
                    $"until after {FormatCode("Initialize")}, once the continuous contract universe makes its first selection; " +
                    $"place orders from {FormatCode("OnData")}, {FormatCode("OnSecuritiesChanged")} or scheduled events instead.";
            }

            /// <summary>
            /// Returns a string message saying the first argument to AddData must be a custom data class
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string AddDataInvalidPyObjectType(string repr)
            {
                return $"{AlgorithmPrefix()}.{FormatCode("AddData")}(): the first argument must be a custom data type (a Python class deriving from {FormatCode("PythonData")} or a CLR {FormatCode("BaseData")} type), but received {repr}. " +
                    $"To subscribe to built-in asset classes use, for example, {FormatCode("AddEquity")} or {FormatCode("AddCrypto")}.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="AlgorithmFactory.Python.Wrappers.AlgorithmPythonWrapper"/> class
        /// and its consumers or related classes
        /// </summary>
        public static class AlgorithmPythonWrapper
        {
            /// <summary>
            /// Returns a string message saying OnMarginCall must return a non-empty list of SubmitOrderRequest
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string OnMarginCallMustReturnNonEmptyList()
            {
                return $"{FormatCode("OnMarginCall")} must return a non-empty list of SubmitOrderRequest";
            }
        }
    }
}
