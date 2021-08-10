using Deedle;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XPlot.Plotly;
using XChart = XPlot.Plotly.Chart;
using Graph = XPlot.Plotly;

namespace QuantConnect.Research
{
    public static class DataFrameExtensions
    {

        public static Frame<DateTime, string> ToDataFrame<T>(this IEnumerable<Slice> slices, Func<Slice, DataDictionary<T>> getDataDictionary)
            where T : IBar
        {
            if (getDataDictionary is null)
                return null;

            Frame<DateTime, string> dataFrame = null;
            foreach (var slice in slices)
            {
                var df = ToDataFrame(slice, getDataDictionary);

                if (dataFrame == null)
                    dataFrame = df;
                else
                {
                    //concat
                    dataFrame = dataFrame.Merge(df);
                }
            }

            dataFrame = dataFrame.IndexRows<DateTime>("Time").SortRowsByKey();

            return dataFrame;
        }

        public static Frame<DateTime, string> ToDataFrame<T>(this Slice slice, Func<Slice, DataDictionary<T>> getDataDictionary)
        {
            if (getDataDictionary is null)
                return null;

            DataDictionary<T> dataDictionary = getDataDictionary(slice);

            var enumerator = dataDictionary.GetEnumerator();

            List<KeyValuePair<DateTime, Series<string, object>>> rows = new List<KeyValuePair<DateTime, Series<string, object>>>();
            while (enumerator.MoveNext())
            {
                var symbolToData = enumerator.Current;

                var builder = new SeriesBuilder<string>();
                builder.Add(nameof(slice.Time), slice.Time);

                builder.Add(nameof(Symbol), symbolToData.Key.Value);

                if (symbolToData.Value is IBar bar)
                {
                    builder.Add(nameof(bar.Open), bar.Open);
                    builder.Add(nameof(bar.High), bar.High);
                    builder.Add(nameof(bar.Low), bar.Low);
                    builder.Add(nameof(bar.Close), bar.Close);
                }

                if (symbolToData.Value is TradeBar tradeBar)
                {
                    builder.Add(nameof(tradeBar.Volume), tradeBar.Volume);
                }

                rows.Add(new KeyValuePair<DateTime, Series<string, object>>(slice.Time, builder.Series));
            }

            return Frame.FromRows(rows);
        }

        public static void Show(this IEnumerable<PlotlyChart> charts)
        {
            XChart.ShowAll(charts);
        }

        public static IEnumerable<PlotlyChart> ToOhlcvChart(this Frame<DateTime, string> dataFrame)
        {
            var charts = new List<PlotlyChart>();

            var ohlc = ToOhlcChart(dataFrame);

            if (ohlc != null)
                charts.Add(ohlc);

            var volume = ToVolumeChart(dataFrame);

            if (volume != null)
                charts.Add(volume);

            return charts;
        }

        public static PlotlyChart ToOhlcChart(this Frame<DateTime, string> dataFrame)
        {
            if (dataFrame is null)
            {
                throw new ArgumentNullException(nameof(dataFrame));
            }

            IBar bar;
            IEnumerable<Tuple<DateTime, decimal, decimal, decimal, decimal>> chartData = dataFrame.Rows.Observations.Select(indexToRow => new Tuple<DateTime, decimal, decimal, decimal, decimal>(
            indexToRow.Key,
            (decimal)indexToRow.Value[nameof(bar.Open)],
            (decimal)indexToRow.Value[nameof(bar.High)],
            (decimal)indexToRow.Value[nameof(bar.Low)],
            (decimal)indexToRow.Value[nameof(bar.Close)]
            ));

            var chart = XChart.Candlestick(chartData);
            chart.WithLayout(new Layout.Layout
            {
                title = "OHLC",
                xaxis = new Xaxis
                {
                    title = "Date"
                },
                yaxis = new Yaxis
                {
                    title = "Price (USD)"
                }
            });
            return chart;
        }

        public static PlotlyChart ToVolumeChart(this Frame<DateTime, string> dataFrame)
        {
            TradeBar bar;
            if (dataFrame is null)
            {
                throw new ArgumentNullException(nameof(dataFrame));
            }
            else if (!dataFrame.ColumnKeys.Contains(nameof(bar.Volume)))
            {
                return null;
            }

            IEnumerable<Tuple<DateTime, decimal>> chartData = dataFrame.Rows.Observations.Select(indexToRow => new Tuple<DateTime, decimal>(
            indexToRow.Key,
            (decimal)indexToRow.Value[nameof(bar.Volume)]
            ));

            var barTrace = new Graph.Bar()
            {
                x = dataFrame.RowIndex.KeySequence,

                y = dataFrame.Columns[nameof(bar.Volume)].Values,

                //marker = new Graph.Marker { color = "rgb(0, 0, 109)" }

            };

            var chart = XChart.Plot(barTrace);
            chart.WithLayout(new Layout.Layout
            {
                title = "Volume",
                xaxis = new Xaxis
                {
                    title = "Date"
                },
                yaxis = new Yaxis
                {
                    title = "Volume"
                },
            });
            return chart;
        }
    }
}
