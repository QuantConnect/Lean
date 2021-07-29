using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Python
{
    public static class PythonTestingUtils
    {
        public static dynamic GetSlices(Symbol symbol)
        {
            var slices = Enumerable
                .Range(0, 100)
                .Select(
                    i =>
                    {
                        var time = new DateTime(2013, 10, 7).AddMilliseconds(14400000 + i * 10000);
                        return new Slice(
                            time,
                            new List<BaseData> {
                                new Tick
                                {
                                    Time = time,
                                    Symbol = symbol,
                                    Value = 167 + i / 10,
                                    Quantity = 1 + i * 10,
                                    Exchange = "T"
                                }
                            }
                        );
                    }
                );
            return slices;
        }
    }
}
