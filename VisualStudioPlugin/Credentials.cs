using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.VisualStudioPlugin
{
    public struct Credentials
    {
        private string _userId;
        private string _accessToken;

        public Credentials(string userId, string accessToken)
        {
            _userId = userId;
            _accessToken = accessToken;
        }

        public string UserId
        {
            get
            {
                return _userId;
            }
        }

        public string AccessToken
        {
            get
            {
                return _accessToken;
            }
        }

    }
}
