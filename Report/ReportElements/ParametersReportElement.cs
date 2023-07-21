using QuantConnect.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Report.ReportElements
{
    public class ParametersReportElement : ReportElement
    {
        private IReadOnlyDictionary<string, string> _parameters;
        public ParametersReportElement(string name, string key, AlgorithmConfiguration backtest, AlgorithmConfiguration live)
        {
            if (backtest == null)
            {
                _parameters = live.Parameters;
            }
            else
            {
                _parameters = backtest.Parameters;
            }
        }

        public override string Render()
        {
            var template = @"
<tr>

"
            foreach (var kvp in _parameters)
            {

            }
        }
    }
}
