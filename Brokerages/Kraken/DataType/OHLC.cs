/*
Copyright(c) 2016 Markus Trenkwalder

Full licence contained in QuantConnect.Brokerages/Kraken/KrakenRestApi.cs
or 
at link https://github.com/trenki2/KrakenApi/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType
{

    public class OHLC
    {

        public int Time;
        public decimal Open;
        public decimal High;
        public decimal Low;
        public decimal Close;
        public decimal Vwap;
        public decimal Volume;
        public int Count;
    }
}
