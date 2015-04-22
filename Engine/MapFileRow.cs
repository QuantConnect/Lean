using System;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Represents a single row in a map_file. This is a csv file ordered as {date, mapped symbol}
    /// </summary>
    public class MapFileRow
    {
        /// <summary>
        /// Gets the date associated with this data
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Gets the mapped symbol
        /// </summary>
        public string MappedSymbol { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFileRow"/> class.
        /// </summary>
        public MapFileRow(DateTime date, string mappedSymbol)
        {
            Date = date;
            MappedSymbol = mappedSymbol;
        }

        /// <summary>
        /// Reads in the map_file for the specified equity symbol
        /// </summary>
        public static IEnumerable<MapFileRow> Read(string dataFolder, string symbol)
        {
            var path = dataFolder + "equity/map_files/" + symbol.ToLower() + ".csv";
            foreach (var line in File.ReadAllLines(path))
            {
                var csv = line.Split(',');
                yield return new MapFileRow(Time.ParseDate(csv[0]), csv[1]);
            }
        }
    }
}