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
            public static string SetTimeZoneAlreadyRunning()
            {
                return $"Algorithm.{FormatCode("SetTimeZone")}(): Cannot change time zone after algorithm running.";
            }

            /// <summary>
            /// Returns a string message saying the benchmark cannot be changed after the algorithm is initialized
            /// </summary>
            public static string SetBenchmarkAlreadyInitialized()
            {
                return $"Algorithm.{FormatCode("SetBenchmark")}(): Cannot change Benchmark after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the account currency cannot be changed after the algorithm is initialized
            /// </summary>
            public static string SetAccountCurrencyAlreadyInitialized()
            {
                return $"Algorithm.{FormatCode("SetAccountCurrency")}(): Cannot change AccountCurrency after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the cash cannot be changed after the algorithm is initialized
            /// </summary>
            public static string SetCashAlreadyInitialized()
            {
                return $"Algorithm.{FormatCode("SetCash")}(): Cannot change cash available after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the start date cannot be changed after the algorithm is initialized
            /// </summary>
            public static string SetStartDateAlreadyInitialized()
            {
                return $"Algorithm.{FormatCode("SetStartDate")}(): Cannot change start date after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying the end date cannot be changed after the algorithm is initialized
            /// </summary>
            public static string SetEndDateAlreadyInitialized()
            {
                return $"Algorithm.{FormatCode("SetEndDate")}(): Cannot change end date after algorithm initialized.";
            }

            /// <summary>
            /// Returns a string message saying SetWarmup cannot be used after the algorithm is initialized
            /// </summary>
            public static string SetWarmupAlreadyInitialized()
            {
                return $"QCAlgorithm.{FormatCode("SetWarmup")}(): This method cannot be used after algorithm initialized";
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
            public static string OnMarginCallMustReturnNonEmptyList()
            {
                return $"{FormatCode("OnMarginCall")} must return a non-empty list of SubmitOrderRequest";
            }
        }
    }
}
