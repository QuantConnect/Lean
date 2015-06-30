using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
	public class CommitmentsOfTraders
	{
		public int oi;
		public int ncl;
		public double price;
		public long date;
		public int ncs;
		public string unit;
	}

	public class CommitmentsOfTradersResponse
	{
		public List<CommitmentsOfTraders> AUD_USD;
		public List<CommitmentsOfTraders> GBP_USD;
		public List<CommitmentsOfTraders> USD_CAD;
		public List<CommitmentsOfTraders> EUR_USD;
		public List<CommitmentsOfTraders> USD_JPY;
		public List<CommitmentsOfTraders> USD_MXN;
		public List<CommitmentsOfTraders> NZD_USD;
		public List<CommitmentsOfTraders> USD_CHF;
		public List<CommitmentsOfTraders> XAU_USD;
		public List<CommitmentsOfTraders> XAG_USD;

		public List<CommitmentsOfTraders>  GetData()
		{
			// Built in assumption, there's only one HprData in this object (since we can only request data for one instrument at a time)
			foreach (var field in typeof(CommitmentsOfTradersResponse).GetTypeInfo().DeclaredFields)
			{
				var cotData = (List<CommitmentsOfTraders>)field.GetValue(this);
				if (cotData != null)
				{
					return cotData;
				}
			}
			return null;
		}
	}
}
