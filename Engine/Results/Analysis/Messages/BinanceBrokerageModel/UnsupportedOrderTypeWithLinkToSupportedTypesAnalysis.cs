using System.Collections.Generic;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;

namespace QuantConnect.Lean.Engine.Results.Analysis.Messages.BinanceBrokerageModel
{

    public class UnsupportedOrderTypeWithLinkToSupportedTypesAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "The Binance brokerage does not support ",
            " order type. Supported order types are: ",
        ];


        protected override List<string> PotentialSolutions(Language _) =>
        [
            "The Binance brokerage model does not support this order type. " +
            "Only submit order types that Binance supports. " +
            "See https://www.quantconnect.com/docs/v2/writing-algorithms/reality-modeling/brokerages/supported-models/binance for supported order types.",
        ];
    }
}
