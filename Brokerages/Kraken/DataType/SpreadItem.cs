using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType {

    public class SpreadItem {
        public int Time;
        public decimal Bid;
        public decimal Ask;
    }

}
