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

namespace QuantConnect.Indicators
{

    public abstract partial class IndicatorBase<T>
    {
        /// <summary>
        /// Returns the current value of this instance
        /// </summary>
        /// <param name="instance">The indicator instance</param>
        /// <returns>The current value of the indicator</returns>
        public static implicit operator decimal(IndicatorBase<T> instance)
        {
            return instance.Current;
        }

        /// <summary>
        /// Determines if the indicator's current value is greater than the specified value
        /// </summary>
        public static bool operator >(IndicatorBase<T> left, double right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value > (decimal)right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than the specified value
        /// </summary>
        public static bool operator <(IndicatorBase<T> left, double right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value < (decimal)right;
        }

        /// <summary>
        /// Determines if the specified value is greater than the indicator's current value
        /// </summary>
        public static bool operator >(double left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left > right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than the indicator's current value
        /// </summary>
        public static bool operator <(double left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left < right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is greater than or equal to the specified value
        /// </summary>
        public static bool operator >=(IndicatorBase<T> left, double right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value >= (decimal)right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than or equal to the specified value
        /// </summary>
        public static bool operator <=(IndicatorBase<T> left, double right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value <= (decimal)right;
        }

        /// <summary>
        /// Determines if the specified value is greater than or equal to the indicator's current value
        /// </summary>
        public static bool operator >=(double left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left >= right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than or equal to the indicator's current value
        /// </summary>
        public static bool operator <=(double left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left <= right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is equal to the specified value
        /// </summary>
        public static bool operator ==(IndicatorBase<T> left, double right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value == (decimal)right;
        }

        /// <summary>
        /// Determines if the indicator's current value is not equal to the specified value
        /// </summary>
        public static bool operator !=(IndicatorBase<T> left, double right)
        {
            if (ReferenceEquals(left, null)) return true;
            return left.Current.Value != (decimal)right;
        }

        /// <summary>
        /// Determines if the specified value is equal to the indicator's current value
        /// </summary>
        public static bool operator ==(double left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left == right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is not equal to the indicator's current value
        /// </summary>
        public static bool operator !=(double left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return true;
            return (decimal)left != right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is greater than the specified value
        /// </summary>
        public static bool operator >(IndicatorBase<T> left, float right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value > (decimal)right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than the specified value
        /// </summary>
        public static bool operator <(IndicatorBase<T> left, float right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value < (decimal)right;
        }

        /// <summary>
        /// Determines if the specified value is greater than the indicator's current value
        /// </summary>
        public static bool operator >(float left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left > right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than the indicator's current value
        /// </summary>
        public static bool operator <(float left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left < right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is greater than or equal to the specified value
        /// </summary>
        public static bool operator >=(IndicatorBase<T> left, float right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value >= (decimal)right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than or equal to the specified value
        /// </summary>
        public static bool operator <=(IndicatorBase<T> left, float right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value <= (decimal)right;
        }

        /// <summary>
        /// Determines if the specified value is greater than or equal to the indicator's current value
        /// </summary>
        public static bool operator >=(float left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left >= right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than or equal to the indicator's current value
        /// </summary>
        public static bool operator <=(float left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left <= right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is equal to the specified value
        /// </summary>
        public static bool operator ==(IndicatorBase<T> left, float right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value == (decimal)right;
        }

        /// <summary>
        /// Determines if the indicator's current value is not equal to the specified value
        /// </summary>
        public static bool operator !=(IndicatorBase<T> left, float right)
        {
            if (ReferenceEquals(left, null)) return true;
            return left.Current.Value != (decimal)right;
        }

        /// <summary>
        /// Determines if the specified value is equal to the indicator's current value
        /// </summary>
        public static bool operator ==(float left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return (decimal)left == right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is not equal to the indicator's current value
        /// </summary>
        public static bool operator !=(float left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return true;
            return (decimal)left != right.Current.Value;
        }
        /// <summary>
        /// Determines if the indicator's current value is greater than the specified value
        /// </summary>
        public static bool operator >(IndicatorBase<T> left, int right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value > right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than the specified value
        /// </summary>
        public static bool operator <(IndicatorBase<T> left, int right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value < right;
        }

        /// <summary>
        /// Determines if the specified value is greater than the indicator's current value
        /// </summary>
        public static bool operator >(int left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left > right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than the indicator's current value
        /// </summary>
        public static bool operator <(int left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left < right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is greater than or equal to the specified value
        /// </summary>
        public static bool operator >=(IndicatorBase<T> left, int right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value >= right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than or equal to the specified value
        /// </summary>
        public static bool operator <=(IndicatorBase<T> left, int right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value <= right;
        }

        /// <summary>
        /// Determines if the specified value is greater than or equal to the indicator's current value
        /// </summary>
        public static bool operator >=(int left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left >= right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than or equal to the indicator's current value
        /// </summary>
        public static bool operator <=(int left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left <= right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is equal to the specified value
        /// </summary>
        public static bool operator ==(IndicatorBase<T> left, int right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value == right;
        }

        /// <summary>
        /// Determines if the indicator's current value is not equal to the specified value
        /// </summary>
        public static bool operator !=(IndicatorBase<T> left, int right)
        {
            if (ReferenceEquals(left, null)) return true;
            return left.Current.Value != right;
        }

        /// <summary>
        /// Determines if the specified value is equal to the indicator's current value
        /// </summary>
        public static bool operator ==(int left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left == right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is not equal to the indicator's current value
        /// </summary>
        public static bool operator !=(int left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return true;
            return left != right.Current.Value;
        }
        /// <summary>
        /// Determines if the indicator's current value is greater than the specified value
        /// </summary>
        public static bool operator >(IndicatorBase<T> left, long right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value > right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than the specified value
        /// </summary>
        public static bool operator <(IndicatorBase<T> left, long right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value < right;
        }

        /// <summary>
        /// Determines if the specified value is greater than the indicator's current value
        /// </summary>
        public static bool operator >(long left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left > right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than the indicator's current value
        /// </summary>
        public static bool operator <(long left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left < right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is greater than or equal to the specified value
        /// </summary>
        public static bool operator >=(IndicatorBase<T> left, long right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value >= right;
        }

        /// <summary>
        /// Determines if the indicator's current value is less than or equal to the specified value
        /// </summary>
        public static bool operator <=(IndicatorBase<T> left, long right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value <= right;
        }

        /// <summary>
        /// Determines if the specified value is greater than or equal to the indicator's current value
        /// </summary>
        public static bool operator >=(long left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left >= right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is less than or equal to the indicator's current value
        /// </summary>
        public static bool operator <=(long left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left <= right.Current.Value;
        }

        /// <summary>
        /// Determines if the indicator's current value is equal to the specified value
        /// </summary>
        public static bool operator ==(IndicatorBase<T> left, long right)
        {
            if (ReferenceEquals(left, null)) return false;
            return left.Current.Value == right;
        }

        /// <summary>
        /// Determines if the indicator's current value is not equal to the specified value
        /// </summary>
        public static bool operator !=(IndicatorBase<T> left, long right)
        {
            if (ReferenceEquals(left, null)) return true;
            return left.Current.Value != right;
        }

        /// <summary>
        /// Determines if the specified value is equal to the indicator's current value
        /// </summary>
        public static bool operator ==(long left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return false;
            return left == right.Current.Value;
        }

        /// <summary>
        /// Determines if the specified value is not equal to the indicator's current value
        /// </summary>
        public static bool operator !=(long left, IndicatorBase<T> right)
        {
            if (ReferenceEquals(right, null)) return true;
            return left != right.Current.Value;
        }
    }
}
