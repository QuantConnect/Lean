using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Custom.Intrinio
{
    /// <summary>
    ///     Auxiliary class to access all Intrinio API data.
    /// </summary>
    public static class IntrinioConfig
    {
        private static string _user;
        private static string _password;
       
        /// <summary>
        ///     Intrinio API user
        /// </summary>
        public static string User { get { return _user; } }
        /// <summary>
        ///     Intrinio API password
        /// </summary>
        public static string Password { get { return _password; } }

        /// <summary>
        ///     Check if Intrinio API user and password are not empty or null.
        /// </summary>
        public static bool IsInitialized => !string.IsNullOrWhiteSpace(_user) && !string.IsNullOrWhiteSpace(_password);

        /// <summary>
        /// Set the Intrinio API user and password.
        /// </summary>
        public static void SetUserAndPassword(string user, string password)
        {
            _user = user;
            _password = password;

            if (!IntrinioConfig.IsInitialized)
            {
                throw new NotImplementedException("Please set a valid Intrinio user and password.");
            }
        }
    }
}
