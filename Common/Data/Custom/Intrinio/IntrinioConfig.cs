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
        /// <summary>
        ///     Intrinio API user
        /// </summary>
        public static string User { get; set; }
        /// <summary>
        ///     Intrinio API password
        /// </summary>
        public static string Password { get; set; }

        /// <summary>
        ///     Check if Intrinio API user and password are not empty or null.
        /// </summary>
        public static bool AreUserAndPasswordSet => !(string.IsNullOrWhiteSpace(User) && string.IsNullOrWhiteSpace(Password));
    }
}
