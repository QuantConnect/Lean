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
using System;

namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents whether a property is optional.
    /// </summary>
	public class IsOptionalAttribute : Attribute
	{
		public override string ToString()
		{
			return "Is Optional";
		}
	}

    /// <summary>
    /// Represents maximum value of a property.
    /// </summary>
	public class MaxValueAttribute : Attribute
	{
		public object Value { get; set; }
		public MaxValueAttribute(int i)
		{
			Value = i;
		}
	}

    /// <summary>
    /// Represents minimum value of a property.
    /// </summary>
	public class MinValueAttribute : Attribute
	{
		public object Value { get; set; }
		public MinValueAttribute(int i)
		{
			Value = i;
		}
	}

    /// <summary>
    /// Represents a financial instrument / product provided by Oanda.
    /// </summary>
	public class Instrument
    {
		public bool HasInstrument;
	    private string _instrument;
        public string instrument 
		{
			get { return _instrument; }
			set 
			{ 
				_instrument = value;
				HasInstrument = true;
			}
		}

		public bool HasdisplayName;
		private string _displayName;
		public string displayName
		{
			get { return _displayName; }
			set
			{
				_displayName = value;
				HasdisplayName = true;
			}
		}

		public bool Haspip;
		private string _pip;
		public string pip
		{
			get { return _pip; }
			set
			{
				_pip = value;
				Haspip = true;
			}
		}

		[IsOptional]
		public bool HaspipLocation;
		private int _pipLocation;
		public int pipLocation
		{
			get { return _pipLocation; }
			set
			{
				_pipLocation = value;
				HaspipLocation = true;
			}
		}

		[IsOptional]
		public bool HasextraPrecision;
		private int _extraPrecision;
		public int extraPrecision
		{
			get { return _extraPrecision; }
			set
			{
				_extraPrecision = value;
				HasextraPrecision = true;
			}
		}

		public bool HasmaxTradeUnits;
		private int _maxTradeUnits;
		public int maxTradeUnits
		{
			get { return _maxTradeUnits; }
			set
			{
				_maxTradeUnits = value;
				HasmaxTradeUnits = true;
			}
		}
    }
}
