using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.VisualStudioPlugin
{
    class NotAuthenticatedException : Exception
    {
        public NotAuthenticatedException()
        {
        }

        public NotAuthenticatedException(string message)
        : base(message)
        {
        }

        public NotAuthenticatedException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }
}
