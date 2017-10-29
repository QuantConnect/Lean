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

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides convenience of linq extension methods for <see cref="IEnumerator{T}"/> types
    /// </summary>
    public static class EnumeratorExtensions
    {
        /// <summary>
        /// Filter the enumerator using the specified predicate
        /// </summary>
        public static IEnumerator<T> Where<T>(this IEnumerator<T> enumerator, Func<T, bool> predicate)
        {
            using (enumerator)
            {
                while (enumerator.MoveNext())
                {
                    if (predicate(enumerator.Current))
                    {
                        yield return enumerator.Current;
                    }
                }
            }
        }

        /// <summary>
        /// Project the enumerator using the specified selector
        /// </summary>
        public static IEnumerator<TResult> Select<T, TResult>(this IEnumerator<T> enumerator, Func<T, TResult> selector)
        {
            using (enumerator)
            {
                while (enumerator.MoveNext())
                {
                    yield return selector(enumerator.Current);
                }
            }
        }

        /// <summary>
        /// Project the enumerator using the specified selector
        /// </summary>
        public static IEnumerator<TResult> SelectMany<T, TResult>(this IEnumerator<T> enumerator, Func<T, IEnumerator<TResult>> selector)
        {
            using (enumerator)
            {
                while (enumerator.MoveNext())
                {
                    using (var inner = selector(enumerator.Current))
                    {
                        while (inner.MoveNext())
                        {
                            yield return inner.Current;
                        }
                    }
                }
            }
        }
    }
}
