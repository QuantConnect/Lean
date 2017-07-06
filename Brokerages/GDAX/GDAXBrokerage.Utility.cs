using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.GDAX
{

    /// <summary>
    /// Utility methods for GDAX brokerage
    /// </summary>
    public partial class GDAXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {

        private const string _header = "CB-ACCESS-SIGN";


        /// <summary>
        /// Creates an auth token and adds to the request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public AuthenticationToken GetAuthenticationToken(IRestRequest request)
        {
            var body = request.Parameters.SingleOrDefault(b => b.Type == ParameterType.RequestBody);
            var token = GetAuthenticationToken(body == null ? "" : body.Value.ToString(), request.Method.ToString().ToUpper(), request.Resource);
            request.AddHeader(_header, token.Signature);

            return token;
        }

        /// <summary>
        /// Creates an auth token to sign a request
        /// </summary>
        /// <param name="body">the request cody as json</param>
        /// <param name="method">the http method</param>
        /// <param name="url">the request url</param>
        /// <returns></returns>
        public AuthenticationToken GetAuthenticationToken(string body, string method, string url)
        {
            var token = new AuthenticationToken
            {
                Key = ApiKey,
                Passphrase = _passPhrase,
                Timestamp = Time.DateTimeToUnixTimeStamp(DateTime.UtcNow).ToString(System.Globalization.CultureInfo.InvariantCulture)
            };

            byte[] data = Convert.FromBase64String(ApiSecret);
            var prehash = token.Timestamp + method + url + body;

            byte[] bytes = Encoding.UTF8.GetBytes(prehash);
            using (var hmac = new HMACSHA256(data))
            {
                byte[] hash = hmac.ComputeHash(bytes);
                token.Signature = Convert.ToBase64String(hash);
            }

            return token;
        }

        private static string ConvertOrderType(Orders.OrderType orderType)
        {
            if (orderType == Orders.OrderType.Limit || orderType == Orders.OrderType.Market)
            {
                return orderType.ToString().ToLower();
            }
            else if (orderType == Orders.OrderType.StopMarket)
            {
                return "stop";
            }

            throw new Exception("Unsupported order type:" + orderType.ToString());
        }

        /// <summary>
        /// Converts a product id to a symbol
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static Symbol ConvertProductId(string productId)
        {
            return Symbol.Create(productId.Replace("-", ""), SecurityType.Forex, Market.GDAX);
        }

        /// <summary>
        /// Converts a symbol to a product id
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        protected static string ConvertSymbol(Symbol symbol)
        {
            return symbol.Value.Substring(0, 3).ToLower() + "-" + symbol.Value.Substring(3, 3).ToLower();
        }

        private static Orders.OrderStatus ConvertOrderStatus(Messages.Order order)
        {
            if (order.Size != 0 && order.FilledSize != order.Size)
            {
                return Orders.OrderStatus.PartiallyFilled;
            }       
            else if (order.Status == "open" || order.Status == "pending" || order.Status == "active")
            {
                return Orders.OrderStatus.Submitted;
            }
            else if (order.Status == "done" || order.Status == "settled")
            {
                return Orders.OrderStatus.Filled;
            }

            return Orders.OrderStatus.None;
        }

    }
}
