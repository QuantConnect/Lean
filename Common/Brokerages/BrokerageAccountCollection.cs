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
using System.Collections.ObjectModel;
using System.Linq;

namespace QuantConnect.Brokerages
{
    internal static class BrokerageAccountCollection
    {
        public static IReadOnlyList<string> CopyIdentifiers(
            IEnumerable<string> identifiers,
            string parameterName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var identifier in identifiers ?? Enumerable.Empty<string>())
            {
                ValidateIdentifier(identifier, parameterName);
                if (!result.Add(identifier))
                {
                    throw new ArgumentException(
                        $"Identifier '{identifier}' is duplicated.",
                        parameterName);
                }
            }
            return Array.AsReadOnly(
                result.OrderBy(identifier => identifier, StringComparer.OrdinalIgnoreCase).ToArray());
        }

        public static IReadOnlyDictionary<string, TValue> CopyDictionary<TValue>(
            IReadOnlyDictionary<string, TValue> source,
            string parameterName,
            Func<TValue, string> valueIdentifier = null,
            StringComparer comparer = null)
        {
            var result = new Dictionary<string, TValue>(
                comparer ?? StringComparer.OrdinalIgnoreCase);
            if (source == null)
            {
                return new ReadOnlyDictionary<string, TValue>(result);
            }

            foreach (var pair in source)
            {
                ValidateIdentifier(pair.Key, parameterName);
                var identifier = pair.Key;
                if (valueIdentifier != null)
                {
                    var expectedIdentifier = valueIdentifier(pair.Value);
                    if (string.IsNullOrEmpty(expectedIdentifier) ||
                        !identifier.Equals(expectedIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(
                            $"Dictionary key '{identifier}' does not match its value identifier '{expectedIdentifier}'.",
                            parameterName);
                    }
                }

                if (!result.TryAdd(identifier, pair.Value))
                {
                    throw new ArgumentException(
                        $"Dictionary identifier '{identifier}' is duplicated.",
                        parameterName);
                }
            }

            return new ReadOnlyDictionary<string, TValue>(result);
        }

        public static void ValidateIdentifier(string identifier, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(identifier) ||
                !identifier.Equals(identifier.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Identifiers must be non-empty and contain no leading or trailing whitespace.",
                    parameterName);
            }
        }
    }
}
