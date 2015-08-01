/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System.Reflection;
using System.Text;
using QuantConnect.Brokerages.Oanda.Framework;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
    /// <summary>
    /// Represents the Restful web response from Oanda.
    /// </summary>
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
