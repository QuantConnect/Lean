using Microsoft.Data.Analysis;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Research
{
    public static class DataFrameExtensions
    {
        public static DataFrame ToDataFrame<T>(this IEnumerable<Slice> slices, Func<DataDictionary<T>> getDataDictionary)
            where T : IBar
        {
            if (getDataDictionary is null)
                return null;

            DataFrame dataFrame = null;
            foreach (var slice in slices)
            {
                var df = ToDataFrame(slice, getDataDictionary);

                if (dataFrame == null)
                    dataFrame = df;
                else
                {
                    //concat
                    foreach (var row in df.Rows)
                    {
                        dataFrame.Append(row, true);
                    }
                }
            }

            return dataFrame;
        }

        public static DataFrame ToDataFrame<T>(this Slice slice, Func<DataDictionary<T>> getDataDictionary)
            where T : IBar
        {
            if (getDataDictionary is null)
                return null;

            DataDictionary<T> dataDictionary = getDataDictionary();

            var enumerator = dataDictionary.GetEnumerator();

            var dataFrame = new DataFrame();
            while (!enumerator.MoveNext())
            {
                var symbolToData = enumerator.Current;

                var dataFrameRow = ToDataFrameRow(slice, () => symbolToData);

                dataFrame.Append(dataFrameRow, true);
            }

            return dataFrame;
        }

        public static DataFrameRow ToDataFrameRow<T>(this Slice slice, Func<KeyValuePair<Symbol, T>> getSymbolToBar)
            where T : IBar
        {
            if (getSymbolToBar is null)
                return null;

            PrimitiveDataFrameColumn<DateTime> time = new PrimitiveDataFrameColumn<DateTime>(nameof(slice.Time), new List<DateTime> { slice.Time });

            KeyValuePair<Symbol, T> symbolToData = getSymbolToBar();
            IBar bar = symbolToData.Value;

            StringDataFrameColumn symbol = new StringDataFrameColumn(nameof(Symbol), new List<string> { symbolToData.Key.Value });
            PrimitiveDataFrameColumn<decimal> open = new PrimitiveDataFrameColumn<decimal>(nameof(bar.Open), new List<decimal> { bar.Open });
            PrimitiveDataFrameColumn<decimal> high = new PrimitiveDataFrameColumn<decimal>(nameof(bar.High), new List<decimal> { bar.High });
            PrimitiveDataFrameColumn<decimal> low = new PrimitiveDataFrameColumn<decimal>(nameof(bar.Low), new List<decimal> { bar.Low });
            PrimitiveDataFrameColumn<decimal> close = new PrimitiveDataFrameColumn<decimal>(nameof(bar.Close), new List<decimal> { bar.Close });

            var dataframe = new DataFrame(time, symbol, open, high, low, close);

            return dataframe.Rows[0];
        }
    }
}
