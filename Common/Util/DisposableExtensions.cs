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
            catch (Exception error)
            {
                errorHandler(error);
                return false;
            }
        }
    }
}
