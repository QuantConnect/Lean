using System;

namespace QuantConnect.Indicators
{
	/// <summary>
    	/// This indicator computes the n-period expected return.
    	/// </summary>
	public class expReturn : WindowIndicator<IndicatorDataPoint>
	{
		private decimal _rollingSum;

		/// <summary>
        	/// Initializes a new instance of the <see cref="ExpectedReturn"/> class using the specified period.
        	/// </summary> 
        	/// <param name="period">The period of the indicator</param>
		public expReturn(string name, int period)
			:base(name, period)
		{
		}
		
		/// <summary>
        	/// Initializes a new instance of the <see cref="ExpectedReturn"/> class using the specified name and period.
        	/// </summary> 
        	/// <param name="name">The name of this indicator</param>
        	/// <param name="period">The period of the indicator</param>
		public LogReturn(int period)
			: base("EXPRET" + period, period)
		{
		}

		/// <summary>
        	/// Gets a flag indicating when this indicator is ready and fully initialized
        	/// </summary>
        	public override bool IsReady
        	{
            		get { return Samples >= Period; }
        	}

		/// <summary>
        	/// Computes the next value of this indicator from the given state
        	/// </summary>
        	/// <param name="input">The input given to the indicator</param>
        	/// <param name="window">The window for the input history</param>
        	/// <returns>A new value for this indicator</returns>
		protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
		{
			_rollingSum += input.Value;
			if(Samples < Period)
				return 0m;
			var meanValue = _rollingSum / Period;
			var remvedValue = window[Period -1];
			_rollingSum -= removedValue;
			return meanValue;
		}
	}
}
