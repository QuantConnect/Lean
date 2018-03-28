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
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides extensions methods for <see cref="IDisposable"/>
    /// </summary>
    public static class DisposableExtensions
    {
        /// <summary>
        /// Calls <see cref="IDisposable.Dispose"/> within a try/catch and logs any errors.
        /// </summary>
        /// <param name="disposable">The <see cref="IDisposable"/> to be disposed</param>
        /// <returns>True if the object was successfully disposed, false if an error was thrown</returns>
        public static bool DisposeSafely(this IDisposable disposable)
        {
            return disposable.DisposeSafely(error => Log.Error(error));
        }

        /// <summary>
        /// Calls <see cref="IDisposable.Dispose"/> within a try/catch and invokes the
        /// <paramref name="errorHandler"/> on any errors.
        /// </summary>
        /// <param name="disposable">The <see cref="IDisposable"/> to be disposed</param>
        /// <param name="errorHandler">Error handler delegate invoked if an exception is thrown
        /// while calling <see cref="IDisposable.Dispose"/></param> on <paramref name="disposable"/>
        /// <returns>True if the object was successfully disposed, false if an error was thrown or
        /// the specified disposable was null</returns>
        public static bool DisposeSafely(this IDisposable disposable, Action<Exception> errorHandler)
        {
            if (disposable == null)
            {
                return false;
            }

            try
            {
                disposable.Dispose();
                return true;
            }
            catch (ObjectDisposedException)
            {
                // we got what we wanted, the object has been diposed
                return true;
            }
            catch (Exception error)
            {
                errorHandler(error);
                return false;
            }
        }
    }
}
