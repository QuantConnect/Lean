using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantConnect.Algorithm.Framework.Portfolio;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using QuantConnect.Data.Market;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Sends positions holdings to a 3rd party API
    /// </summary>
    public interface ISignalExportTarget
    {
        /// <summary>
        /// Sends the positions holdings the user have defined to certain 3rd party API
        /// </summary>
        /// <param name="holdings">Holdings the user have defined to be sent to certain 3rd party API</param>
        string Send(List<PortfolioTarget> holdings);
    }
}
