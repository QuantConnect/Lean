using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace QuantConnect.Brokerages.Oanda.Session
{
    /// <summary>
    /// Initialise an events sessions for Oanda Brokerage.
    /// </summary>
    public class EventsSession : StreamSession<Event>
    {
        public EventsSession(int accountId)
            : base(accountId)
        {
        }

        protected override async Task<WebResponse> GetSession()
        {
            return await OandaBrokerage.StartEventsSession(new List<int> {_accountId});
        }
    }
}