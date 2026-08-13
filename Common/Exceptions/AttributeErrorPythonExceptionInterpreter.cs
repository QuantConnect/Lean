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
using System.Text.RegularExpressions;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Interprets Python AttributeError exceptions caused by accessing an attribute of the wrong bar type,
    /// e.g. reading 'volume' off a QuoteBar (quote-only subscriptions deliver QuoteBars in the slice) or
    /// bid/ask attributes off a TradeBar. Only fires when it has a targeted hint for the failed attribute;
    /// all other AttributeErrors keep the default interpretation.
    /// </summary>
    public class AttributeErrorPythonExceptionInterpreter : PythonExceptionInterpreter
    {
        // Python renders these errors as "'QuoteBar' object has no attribute 'volume'",
        // optionally followed by a "Did you mean: ..." suggestion (Python 3.10+)
        private static readonly Regex AttributeErrorRegex = new(
            @"'(?<type>\w+)' object has no attribute '(?<attribute>\w+)'", RegexOptions.Compiled);

        /// <summary>
        /// Determines the order that an instance of this class should be called
        /// </summary>
        public override int Order => 0;

        /// <summary>
        /// Determines if this interpreter should be applied to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be interpreted, false otherwise</returns>
        public override bool CanInterpret(Exception exception)
        {
            var pythonException = exception as PythonException;
            if (pythonException == null)
            {
                return false;
            }

            using (Py.GIL())
            {
                if (!base.CanInterpret(exception) ||
                    !pythonException.Type.Name.Contains("AttributeError", StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return TryGetHint(pythonException.Message, out _);
        }

        /// <summary>
        /// Interprets the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be interpreted</param>
        /// <param name="innerInterpreter">An interpreter that should be applied to the inner exception.</param>
        /// <returns>The interpreted exception</returns>
        public override Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            var pe = (PythonException)exception;

            TryGetHint(pe.Message, out var hint);
            var message = $"{pe.Message.Trim()} {hint}";
            message += PythonUtil.PythonExceptionStackParser(pe.StackTrace);

            return new MissingMemberException(message, pe);
        }

        /// <summary>
        /// Gets the wrong-bar-type hint for the given AttributeError message, if there is one
        /// </summary>
        private static bool TryGetHint(string exceptionMessage, out string hint)
        {
            hint = null;
            var match = AttributeErrorRegex.Match(exceptionMessage ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var type = match.Groups["type"].Value;
            // both snake cased ('bid_size') and C# style ('BidSize') accesses raise the same error shape
            var attribute = match.Groups["attribute"].Value;
            var normalizedAttribute = attribute.Replace("_", string.Empty, StringComparison.InvariantCulture).ToLowerInvariant();

            if (type == "QuoteBar" && normalizedAttribute == "volume")
            {
                hint = Messages.AttributeErrorPythonExceptionInterpreter.QuoteBarHasNoTradeData(attribute);
                return true;
            }

            if (type == "TradeBar" && normalizedAttribute is "bid" or "ask" or "bidprice" or "askprice"
                or "bidsize" or "asksize" or "lastbidsize" or "lastasksize")
            {
                hint = Messages.AttributeErrorPythonExceptionInterpreter.TradeBarHasNoQuoteData(attribute);
                return true;
            }

            return false;
        }
    }
}
