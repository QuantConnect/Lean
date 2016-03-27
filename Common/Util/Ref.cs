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

namespace QuantConnect.Util
{
    /// <summary>
    /// Represents a read-only reference to any value, T
    /// </summary>
    /// <typeparam name="T">The data type the reference points to</typeparam>
    public interface IReadOnlyRef<out T>
    {
        /// <summary>
        /// Gets the current value this reference points to
        /// </summary>
        T Value { get; }
    }

    /// <summary>
    /// Represents a reference to any value, T
    /// </summary>
    /// <typeparam name="T">The data type the reference points to</typeparam>
    public sealed class Ref<T> : IReadOnlyRef<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ref{T}"/> class
        /// </summary>
        /// <param name="getter">A function delegate to get the current value</param>
        /// <param name="setter">A function delegate to set the current value</param>
        public Ref(Func<T> getter, Action<T> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        /// <summary>
        /// Gets or sets the value of this reference
        /// </summary>
        public T Value
        {
            get { return _getter(); }
            set { _setter(value); }
        }

        /// <summary>
        /// Returns a read-only version of this instance
        /// </summary>
        /// <returns>A new instance with read-only semantics/gaurantees</returns>
        public IReadOnlyRef<T> AsReadOnly()
        {
            return new Ref<T>(_getter, value =>
            {
                throw new InvalidOperationException("This instance is read-only.");
            });
        }
    }

    /// <summary>
    /// Provides some helper methods that leverage C# type inference
    /// </summary>
    public static class Ref
    {
        /// <summary>
        /// Creates a new <see cref="Ref{T}"/> instance
        /// </summary>
        public static Ref<T> Create<T>(Func<T> getter, Action<T> setter)
        {
            return new Ref<T>(getter, setter);
        }
        /// <summary>
        /// Creates a new <see cref="IReadOnlyRef{T}"/> instance
        /// </summary>
        public static IReadOnlyRef<T> CreateReadOnly<T>(Func<T> getter)
        {
            return new Ref<T>(getter, null).AsReadOnly();
        }
        /// <summary>
        /// Creates a new <see cref="Ref{T}"/> instance by closing over
        /// the specified <paramref name="initialValue"/> variable.
        /// NOTE: This won't close over the variable input to the function,
        /// but rather a copy of the variable. This reference will use it's
        /// own storage.
        /// </summary>
        public static Ref<T> Create<T>(T initialValue)
        {
            return new Ref<T>(() => initialValue, value => { initialValue = value; });
        }
    }
}
