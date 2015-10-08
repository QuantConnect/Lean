using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications.Requests
{
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
				return _value.ToString().ToLower();
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

		public abstract EServer GetServer();
	}
}
