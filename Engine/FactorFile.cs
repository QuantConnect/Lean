using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Represents an entire factor file for a specified symbol
    /// </summary>
    public class FactorFile
    {
        private readonly SortedDictionary<DateTime, FactorFileRow> _data;
        
        /// <summary>
        /// Gets the symbol this factor file represents
        /// </summary>
        public string Symbol { get; private set; }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="FactorFile"/> class.
        /// </summary>
        public FactorFile(string symbol, IEnumerable<FactorFileRow> data)
        {
            Symbol = symbol;
            _data = new SortedDictionary<DateTime, FactorFileRow>(data.ToDictionary(x => x.Date));
        }

        /// <summary>
        /// Reads a FactorFile in from the DataFolder
        /// </summary>
        public static FactorFile Read(string symbol)
        {
            return new FactorFile(symbol, FactorFileRow.Read(Engine.DataFolder, symbol));
        }

        /// <summary>
        /// Gets the time price factor for the specified search date
        /// </summary>
        public decimal GetTimePriceFactor(DateTime searchDate)
        {
            decimal factor = 1;
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _data.Keys.Reverse())
            {
                if (splitDate.Date < searchDate.Date) break;
                factor = _data[splitDate].TimePriceFactor;
            }
            return factor;
        }

        /// <summary>
        /// Checks whether or not a symbol has scaling factors
        /// </summary>
        public static bool HasScalingFactors(string dataFolder, string symbol)
        {
            // check for factor files
            if (File.Exists(dataFolder + "equity/factor_files/" + symbol.ToLower() + ".csv"))
            {
                return true;
            }
            return false;
        }
    }
}