using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
	public class SpreadPeriod
	{
		public long timestamp;
		public double max;
		public double min;
		public double avg;
	}

	public class SpreadsResponse
	{
		public List<List<string>> max;
		public List<List<string>> min;
		public List<List<string>> avg;

		public List<SpreadPeriod> GetData()
		{
			var results = new SortedDictionary<long, SpreadPeriod>();
			AddValues(results, max, (x, y) => x.max = y);
			AddValues(results, min, (x, y) => x.min = y);
			AddValues(results, avg, (x, y) => x.avg = y);

			double lastMax = 0;
			double lastMin = 0;
			double lastAvg = 0;
			foreach (var period in results.Select(pair => pair.Value))
			{
				if (period.max == 0)
				{
					period.max = lastMax;
				}
				if (period.min == 0)
				{
					period.min = lastMin;
				}
				if (period.avg == 0)
				{
					period.avg = lastAvg;
				}
				lastMax = period.max;
				lastMin = period.min;
				lastAvg = period.avg;
			}

			return results.Values.ToList();
		}

		private void AddValues(SortedDictionary<long, SpreadPeriod> results, List<List<string>> valueList, Func<SpreadPeriod, double, double> setValue )
		{
			foreach (var list in valueList)
			{
				long timestamp = Convert.ToInt64(list[0]);
				double value = Convert.ToDouble(list[1]);

				if (!results.ContainsKey(timestamp))
				{
					results[timestamp] = new SpreadPeriod {timestamp = timestamp};
				}
				setValue(results[timestamp], value);
			}
		}
	}
}
