using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides a base class for python consolidators, necessary to use event handler.
    /// </summary>
    public class PythonConsolidator
    {
        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Function to invoke the event handler
        /// </summary>
        /// <param name="consolidator">Reference to the consolidator itself</param>
        /// <param name="data">The finished data from the consolidator</param>
        public void OnDataConsolidated(PyObject consolidator, IBaseData data)
        {
            DataConsolidated(consolidator, data);
        }
    }
}
