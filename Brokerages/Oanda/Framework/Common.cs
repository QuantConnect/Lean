using System;
using System.Reflection;

namespace QuantConnect.Brokerages.Oanda.Framework
{
    /// <summary>
    /// Common reflection helper methods for Oanda data types.
    /// </summary>
	public class Common
	{
		public static object GetDefault(Type t)
		{
			return typeof(Common).GetTypeInfo().GetDeclaredMethod("GetDefaultGeneric").MakeGenericMethod(t).Invoke(null, null);
		}

		public static T GetDefaultGeneric<T>()
		{
			return default(T);
		}
	}
}
