using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;
using QuantConnect.Brokerages.Oanda.DataType.Communications;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
    public class TransactionsResponse : Response
    {
        public List<Transaction> transactions;
        public string nextPage;
    }
}
