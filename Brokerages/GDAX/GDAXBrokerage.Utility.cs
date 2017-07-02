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
    public partial class GDAXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {

        public AuthenticationToken GetAuthenticationToken(IRestRequest request)
        {
            var body = request.Parameters.SingleOrDefault(b => b.Type == ParameterType.RequestBody);
            return GetAuthenticationToken(body == null ? "" : body.Value.ToString(), request.Method.ToString().ToUpper(), request.Resource);
        }

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

        public static Symbol ConvertProductId(string productId)
        {
            return Symbol.Create(productId.Replace("-", ""), SecurityType.Forex, Market.GDAX);
        }

        public static string ConvertSymbol(Symbol symbol)
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
