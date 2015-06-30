using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
	public class HprData
	{
		public List<List<string>> data;
		public string label;

		public List<HistoricalPositionRatio> GetData()
		{
			var result = new List<HistoricalPositionRatio>();
			foreach (var list in data)
			{
				var hpr = new HistoricalPositionRatio()
					{
						exchangeRate = double.Parse(list[2]),
						longPositionRatio = double.Parse(list[1]),
						timestamp = long.Parse(list[0])
					};
				result.Add(hpr);
			}
			return result;
		}
	}

	public class InnerHprResponse
	{
		public HprData AUD_JPY;
		public HprData AUD_USD;
		public HprData EUR_AUD;
		public HprData EUR_CHF;
		public HprData EUR_GBP;
		public HprData EUR_JPY;
		public HprData EUR_USD;
		public HprData GBP_CHF;
		public HprData GBP_JPY;
		public HprData GBP_USD;
		public HprData NZD_USD;
		public HprData USD_CAD;
		public HprData USD_CHF;
		public HprData USD_JPY;
		public HprData XAU_USD;
		public HprData XAG_USD;
	}

	public class HistoricalPositionRatio
	{
		public long timestamp;
		public double longPositionRatio;
		public double exchangeRate;
	}

	public class HistoricalPositionRatioResponse
	{
		public InnerHprResponse data;

		public List<HistoricalPositionRatio> GetData()
		{
			// Built in assumption, there's only one HprData in this object (since we can only request data for one instrument at a time)
			foreach (var field in typeof(InnerHprResponse).GetTypeInfo().DeclaredFields)
			{
				var hprData = (HprData)field.GetValue(data);
				if (hprData != null)
				{
					return hprData.GetData();
				}
			}
			return null;
		}
	}
}
