using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.Framework;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
	public class Response
	{
		public override string ToString()
		{
			// use reflection to display all the properties that have non default values
			StringBuilder result = new StringBuilder();
			var props = this.GetType().GetTypeInfo().DeclaredProperties;
			result.AppendLine("{");
			foreach (var prop in props)
			{
				if (prop.Name != "Content" && prop.Name != "Subtitle" && prop.Name != "Title" && prop.Name != "UniqueId")
				{
					object value = prop.GetValue(this);
					bool valueIsNull = value == null;
					object defaultValue = Common.GetDefault(prop.PropertyType);
					bool defaultValueIsNull = defaultValue == null;
					if ((valueIsNull != defaultValueIsNull) // one is null when the other isn't
						|| (!valueIsNull && (value.ToString() != defaultValue.ToString()))) // both aren't null, so compare as strings
					{
						result.AppendLine(prop.Name + " : " + prop.GetValue(this));
					}
				}
			}
			result.AppendLine("}");
			return result.ToString();
		}
	}
}
