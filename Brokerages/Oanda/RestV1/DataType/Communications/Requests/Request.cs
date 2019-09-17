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

namespace QuantConnect.Brokerages.Oanda.RestV1.DataType.Communications.Requests
{
#pragma warning disable 1591
    public interface ISmartProperty
	{
		bool HasValue { get; set; }
		void SetValue(object obj);
	}

	// Functionally very similar to System.Nullable, could possibly just replace this
	public struct SmartProperty<T> : ISmartProperty
	{
		private T _value;
		public bool HasValue { get; set; }

		public T Value
		{
			get { return _value; }
			set
			{
				_value = value;
				HasValue = true;
			}
		}

		public static implicit operator SmartProperty<T>(T value)
		{
			return new SmartProperty<T>() { Value = value };
		}

		public static implicit operator T(SmartProperty<T> value)
		{
			return value._value;
		}

		public void SetValue(object obj)
		{
			SetValue((T)obj);
		}
		public void SetValue(T value)
		{
			Value = value;
		}

		public override string ToString()
		{
			// This is ugly, but c'est la vie for now
			if (_value is bool)
			{	// bool values need to be lower case to be parsed correctly
				return _value.ToString().ToLowerInvariant();
			}
			return _value.ToString();
		}
	}

	public abstract class Request
	{
		public abstract string EndPoint { get; }

		public string GetRequestString()
		{
			var result = new StringBuilder();
			result.Append(EndPoint);
			bool firstJoin = true;
			foreach (var declaredField in this.GetType().GetTypeInfo().DeclaredFields)
			{
				var prop = declaredField.GetValue(this);
				var smartProp = prop as ISmartProperty;
				if (smartProp != null && smartProp.HasValue)
				{
					if (firstJoin)
					{
						result.Append("?");
						firstJoin = false;
					}
					else
					{
						result.Append("&");
					}

					result.Append(declaredField.Name + "=" + prop);
				}
			}
			return result.ToString();
		}

	}
#pragma warning restore 1591
}
