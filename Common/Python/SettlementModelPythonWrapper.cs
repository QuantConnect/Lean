using Python.Runtime;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides an implementation of <see cref="ISettlementModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class SettlementModelPythonWrapper : ISettlementModel
    {
        private readonly dynamic _model;

        /// Constructor for initialising the <see cref="SettlementModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Models brokerage transactions, fees, and order</param>
        public SettlementModelPythonWrapper(PyObject model)
        {
            _model = model;
        }

        /// <summary>
        /// Applies cash settlement rules using Pyhon method
        /// </summary>
        /// <param name="applyFundsParameters">The funds application parameters</param>
        public void ApplyFunds(ApplyFundsSettlementModelParameters applyFundsParameters)
        {
            using (Py.GIL())
            {
                _model.ApplyFunds(applyFundsParameters);
            }
        }

        /// <summary>
        /// Scan for pending settlements using Python method
        /// </summary>
        /// <param name="settlementParameters">The settlement parameters</param>
        public void Scan(ScanSettlementModelParameters settlementParameters)
        {
            using (Py.GIL())
            {
                _model.Scan(settlementParameters);
            }
        }
    }
}
