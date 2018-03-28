using System;
using QuantConnect.Parameters;
using QuantConnect.Util;

namespace QuantConnect.Data.Custom.Intrinio
{
    /// <summary>
    ///     Auxiliary class to access all Intrinio API data.
    /// </summary>
    public static class IntrinioConfig
    {
        /// <summary>
        /// </summary>
        public static RateGate RateGate =
            new RateGate(1, TimeSpan.FromMilliseconds(5000));

        /// <summary>
        ///     Check if Intrinio API user and password are not empty or null.
        /// </summary>
        public static bool IsInitialized => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);

        /// <summary>
        ///     Intrinio API password
        /// </summary>
        public static string Password = string.Empty;

        /// <summary>
        ///     Intrinio API user
        /// </summary>
        public static string User = string.Empty;

        /// <summary>
        ///     Set the Intrinio API user and password.
        /// </summary>
        public static void SetUserAndPassword(string user, string password)
        {
            User = user;
            Password = password;

            if (!IsInitialized)
            {
                throw new InvalidOperationException("Please set a valid Intrinio user and password.");
            }
        }
    }
}