
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Custom;

namespace QuantConnect.Algorithm.CSharp
{
    public class DailySentimentScoreAlgorithm : QCAlgorithm
    {
        private Symbol _aapl;

        public override void Initialize()
        {
            SetStartDate(2024, 1, 2);
            SetEndDate(2024, 1, 31);
            SetCash(100000);

            _aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
            AddData<DailySentimentScore>(_aapl, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            var sentiment = data.Get<DailySentimentScore>(_aapl);
            if (sentiment != null)
            {
                if (sentiment.Score > 0 && !Portfolio[_aapl].Invested)
                    SetHoldings(_aapl, 1.0);
                else if (sentiment.Score <= 0 && Portfolio[_aapl].Invested)
                    Liquidate(_aapl);
            }
        }
    }
}
