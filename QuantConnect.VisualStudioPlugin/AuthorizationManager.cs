using QuantConnect.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Singleton that stores a reference to an authenticated Api instance
    /// </summary>
    class AuthorizationManager
    {
        private static AuthorizationManager _authorizationManager = new AuthorizationManager();
        private Api.Api _api;

        /// <summary>
        /// Get singleton authorization manager instance accessable from multiple commands
        /// </summary>
        /// <returns>singleton authorization instance</returns>
        public static AuthorizationManager GetInstance()
        {
            return _authorizationManager;
        }

        /// <summary>
        /// Get an authenticated API instance. 
        /// </summary>
        /// <returns>Authenticated API instance</returns>
        /// <exception cref="NotAuthenticatedException">It API is not authenticated</exception>
        public Api.Api GetApi()
        {
            return _api;
        }

        /// <summary>
        /// Check if the API is authenticated
        /// </summary>
        /// <returns>true if API is authenticated, false otherwise</returns>
        public bool IsLoggedIn()
        {
            return false;
        }

        /// <summary>
        /// Authenticate API 
        /// </summary>
        /// <param name="userId">User id to authenticate the API</param>
        /// <param name="accessToken">Access token to authenticate the API</param>
        /// <returns>true if successfully authenticated API, false otherwise</returns>
        public bool LogIn(string userId, string accessToken)
        {
            return false;
        }

        /// <summary>
        /// Log out the API
        /// </summary>
        public void LogOut()
        {
            _api = null;
        }

    }
}
