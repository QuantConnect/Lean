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
using QuantConnect.Commands;
using QuantConnect.Orders;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Commands"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Commands.BaseCommand"/> class and its consumers or related classes
        /// </summary>
        public static class BaseCommand
        {
            /// <summary>
            /// Returns a string message saying: Please provide values for: Ticker, Market and SecurityType
            /// </summary>
            public static string MissingValuesToGetSymbol = "Please provide values for: Ticker, Market & SecurityType";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Commands.BaseCommandHandler"/> class and its consumers or related classes
        /// </summary>
        public static class BaseCommandHandler
        {
            /// <summary>
            /// Returns a string with the given command
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ExecutingCommand(ICommand command)
            {
                return $"Executing {command}";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Commands.FileCommandHandler"/> class and its consumers or related classes
        /// </summary>
        public static class FileCommandHandler
        {
            /// <summary>
            /// Returns a string message saying: Command Id is null or empty, will skip writing result file
            /// </summary>
            public static string NullOrEmptyCommandId = "Command Id is null or empty, will skip writing result file";

            /// <summary>
            /// Returns a string message saying the given commandFilePath is being read
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ReadingCommandFile(string commandFilePath)
            {
                return $"Reading command file {commandFilePath}";
            }

            /// <summary>
            /// Returns a string message saying the given commandFilePath does not exists
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CommandFileDoesNotExist(string commandFilePath)
            {
                return $"File {commandFilePath} does not exists";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Commands.OrderCommand"/> class and its consumers or related classes
        /// </summary>
        public static class OrderCommand
        {
            /// <summary>
            /// Returns a string message with basic information about a command, such us:
            /// order type, symbol, quantity and response
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CommandInfo(OrderType orderType, QuantConnect.Symbol symbol, decimal quantity, Orders.OrderResponse response)
            {
                return Invariant($"{orderType} for {quantity} units of {symbol}: {response}");
            }
        }
    }
}
