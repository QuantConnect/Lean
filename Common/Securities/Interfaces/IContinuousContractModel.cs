using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Interfaces
{
    /// <summary>
    /// Enum defines types of possible privce adjustments in continuous contract modeling. 
    /// ForwardAdjusted - new quotes are adjusted as new data comes
    /// BackAdjusted - old quotes are retrospectively adjusted as new data comes
    /// </summary>
    public enum AdjustmentType
    {
        ForwardAdjusted,
        BackAdjusted
    };

    /// <summary>
    /// Continuous contract model interface. Interfaces is implemented by different classes 
    /// realizing various methods for modeling continuous security series. Primarily, modeling of continuous futures. 
    /// Continuous contracts are used in backtesting of otherwise expiring derivative contracts. 
    /// Continuous contracts are not traded, and are not products traded on exchanges.
    /// </summary>
    public interface IContinuousContractModel
    {
        /// <summary>
        /// Returns adjustment type, implemented by the model
        /// </summary>
        AdjustmentType GetAdjustmentType();

        /// <summary>
        /// Returns current symbol name that corresponds to the current continuous model, 
        /// or null if none.
        /// </summary>
        Symbol GetCurrentSymbol();


        /// <summary>
        /// Method initializes 
        /// </summary>
        /// <param name="initialSeries"></param>
        IEnumerator<BaseData> GetHistory(Dictionary<Symbol, IEnumerator<BaseData>> initialSeries);


        /// <summary>
        /// Method initializes 
        /// </summary>
        /// <param name="initialSeries"></param>
        IEnumerator<BaseData> ModelHistory(Dictionary<Symbol, IEnumerator<BaseData>> initialSeries);


        /// <summary>
        /// This method is a workhorse of the continuous contract model. It returns enumerator of stitched continuous quotes,
        /// produced by the model.
        /// </summary>
        /// <returns>enumerator of stitched continuous quotes</returns>
        IEnumerator<BaseData> GetContinuousData();

    }
}
