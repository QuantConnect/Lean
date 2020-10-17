using System;
using System.Net;

namespace QuantConnect.Brokerages.Zerodha
{
    public class KiteException : Exception
    {
        HttpStatusCode Status;
        public KiteException(string Message, HttpStatusCode HttpStatus, Exception innerException = null) : base(Message, innerException) { Status = HttpStatus; }
    }

    public class GeneralException : KiteException
    {
        public GeneralException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.InternalServerError, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }

    public class TokenException : KiteException
    {
        public TokenException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.Forbidden, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }


    public class PermissionException : KiteException
    {
        public PermissionException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.Forbidden, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }

    public class OrderException : KiteException
    {
        public OrderException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.BadRequest, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }

    public class InputException : KiteException
    {
        public InputException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.BadRequest, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }

    public class DataException : KiteException
    {
        public DataException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.BadGateway, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }

    public class NetworkException : KiteException
    {
        public NetworkException(string Message, HttpStatusCode HttpStatus = HttpStatusCode.ServiceUnavailable, Exception innerException = null) : base(Message, HttpStatus, innerException) { }
    }

}
