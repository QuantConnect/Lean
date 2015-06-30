using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications.Requests
{
	public class OrderPostRequest
	{
		public SmartProperty<string> instrument;
		public SmartProperty<int> units;
		public SmartProperty<string> side;
		public SmartProperty<string> type;
		public SmartProperty<string> expiry;
		public SmartProperty<double> price;
		
		[IsOptional]
		public SmartProperty<double> lowerBound;
		
		[IsOptional]
		public SmartProperty<double> upperBound;
		
		[IsOptional]
		public SmartProperty<double> stopLoss;
		
		[IsOptional]
		public SmartProperty<double> takeProfit;
		
		[IsOptional]
		public SmartProperty<double> trailingStop;
	}
}
