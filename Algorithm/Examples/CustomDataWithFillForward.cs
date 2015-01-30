using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Test;

namespace QuantConnect.Algorithm.Examples
{
    public class CustomDataWithFillForward : QCAlgorithm
    {
        public List<string> CustomSymbols = new List<string>(); 
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);
            SetEndDate(2014, 01, 01);

            int count = 100;
            for (int i = 0; i < count; i++)
            {
                CustomSymbols.Add("SYM" + i.ToString("000"));
            }

            foreach (var symbol in CustomSymbols)
            {
                AddData<FakeTradeBarCustom>(symbol, Resolution.Minute);
            }
        }

        public void OnData(FakeTradeBarCustom custom)
        {
            
        }
    }
}
