/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2013 OANDA Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without 
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
 * Software, and to permit persons to whom the Software is furnished  to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
 * the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Reflection;
using System.Text;
using QuantConnect.Brokerages.Oanda.RestV1.Framework;

namespace QuantConnect.Brokerages.Oanda.RestV1.DataType.Communications
{
#pragma warning disable 1591
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
#pragma warning restore 1591
}
