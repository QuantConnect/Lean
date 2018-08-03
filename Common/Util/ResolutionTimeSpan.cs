using System;

namespace QuantConnect.Util
{
    /// <summary>
    /// This class allows implicit conversions between Resolution and TimeSpan
    /// </summary>
    public class ResolutionTimeSpan
    {
        public ResolutionTimeSpan(TimeSpan timeSpan) { val = timeSpan; }
        public TimeSpan val;

        // User-defined conversion from ResolutionTimeSpan to TimeSpan
        public static implicit operator TimeSpan(ResolutionTimeSpan resolutionTimeSpan)
        {
            return resolutionTimeSpan.val;
        }

        //  User-defined conversion from TimeSpan to ResolutionTimeSpan
        public static implicit operator ResolutionTimeSpan(TimeSpan timeSpan)
        {
            return new ResolutionTimeSpan(timeSpan);
        }

        //  User-defined conversion from TimeSpan to ResolutionTimeSpan
        public static implicit operator ResolutionTimeSpan(Resolution resolution)
        {
            return new ResolutionTimeSpan(resolution.ToTimeSpan());
        }
    }
}
