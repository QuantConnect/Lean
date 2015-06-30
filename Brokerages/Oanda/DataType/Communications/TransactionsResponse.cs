using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
    public class TransactionsResponse : Response
    {
        public List<Transaction> transactions;
        public string nextPage;
    }
}
