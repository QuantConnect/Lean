using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Conversion
{
    public class Converter
    {
        private Graph G;
        private List<Path> Paths;

        public Converter()
        {
            G = new Graph();

            Paths = new List<Path>();
        }
        
        public void AddPair(string PairSymbol)
        {   

        }
    }
}