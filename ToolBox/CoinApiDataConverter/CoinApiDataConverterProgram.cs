using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    public static class CoinApiDataConverterProgram
    {
        public static void CoinApiDataProgram(string date, string market, string rawDataFolder, string destinationFolder)
        {
            var processignDate = DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
            var converter  = new CoinApiDataConverter(processignDate, market, rawDataFolder, destinationFolder);
            converter.Run();
        }
    }
}
