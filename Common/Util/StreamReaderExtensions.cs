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
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace QuantConnect.Util
{
    /// <summary>
    /// Extension methods to fetch data from a <see cref="StreamReader"/> instance
    /// </summary>
    /// <remarks>The value of these methods is performance. The objective is to avoid  using
    /// <see cref="StreamReader.ReadLine"/> and having to create intermediate substrings, parsing and splitting</remarks>
    public static class StreamReaderExtensions
    {
        // we use '-1' value as a flag to determine whether we have decimal places or not, so we avoid having another variable required
        private const int NoDecimalPlaces = -1;
        private const char NoMoreData = unchecked((char)-1);
        private const char DefaultDelimiter = ',';

        /// <summary>
        /// Gets a decimal from the provided stream reader
        /// </summary>
        /// <param name="stream">The data stream</param>
        /// <param name="delimiter">The data delimiter character to use, default is ','</param>
        /// <returns>The decimal read from the stream</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimal(this StreamReader stream, char delimiter = DefaultDelimiter)
        {
            return GetDecimal(stream, out _, delimiter);
        }

        /// <summary>
        /// Gets a decimal from the provided stream reader
        /// </summary>
        /// <param name="stream">The data stream</param>
        /// <param name="delimiter">The data delimiter character to use, default is ','</param>
        /// <param name="pastEndLine">True if end line was past, useful for consumers to know a line ended</param>
        /// <returns>The decimal read from the stream</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimal(this StreamReader stream, out bool pastEndLine, char delimiter = DefaultDelimiter)
        {
            long value = 0;
            var decimalPlaces = NoDecimalPlaces;
            var current = (char)stream.Read();

            while (current == ' ')
            {
                current = (char)stream.Read();
            }

            var isNegative = current == '-';
            if (isNegative)
            {
                current = (char)stream.Read();
            }

            pastEndLine = current == '\n' || current == '\r' && (stream.Peek() != '\n' || stream.Read() == '\n') || current == NoMoreData;
            while (!(current == delimiter || pastEndLine || current == ' '))
            {
                if (current == '.')
                {
                    decimalPlaces = 0;
                }
                else
                {
                    value = value * 10 + (current - '0');
                    if (decimalPlaces != NoDecimalPlaces)
                    {
                        decimalPlaces++;
                    }
                }
                current = (char)stream.Read();
                pastEndLine = current == '\n' || current == '\r' && (stream.Peek() != '\n' || stream.Read() == '\n') || current == NoMoreData;
            }

            var lo = (int)value;
            var mid = (int)(value >> 32);
            return new decimal(lo, mid, 0, isNegative, (byte)(decimalPlaces != NoDecimalPlaces ? decimalPlaces : 0));
        }

        /// <summary>
        /// Gets a date time instance from a stream reader
        /// </summary>
        /// <param name="stream">The data stream</param>
        /// <param name="format">The format in which the date time is</param>
        /// <param name="delimiter">The data delimiter character to use, default is ','</param>
        /// <returns>The date time instance read</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime GetDateTime(this StreamReader stream, string format = DateFormat.TwelveCharacter, char delimiter = DefaultDelimiter)
        {
            var current = (char)stream.Read();
            while (current == ' ')
            {
                current = (char)stream.Read();
            }

            var index = 0;
            // we know the exact format we want to parse so we can allocate the char array and not use an expensive string builder
            var data = new char[format.Length];
            while (!(current == delimiter || current == '\n' || current == '\r' && (stream.Peek() != '\n' || stream.Read() == '\n') || current == NoMoreData))
            {
                data[index++] = current;
                current = (char)stream.Read();
            }

            return DateTime.ParseExact(data,
                format,
                CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets an integer from a stream reader
        /// </summary>
        /// <param name="stream">The data stream</param>
        /// <param name="delimiter">The data delimiter character to use, default is ','</param>
        /// <returns>The integer instance read</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt32(this StreamReader stream, char delimiter = DefaultDelimiter)
        {
            var result = 0;
            var current = (char)stream.Read();

            while (current == ' ')
            {
                current = (char)stream.Read();
            }

            var isNegative = current == '-';
            if (isNegative)
            {
                current = (char)stream.Read();
            }

            while (!(current == delimiter || current == '\n' || current == '\r' && (stream.Peek() != '\n' || stream.Read() == '\n') || current == NoMoreData || current == ' '))
            {
                result = (current - '0') + result * 10;
                current = (char)stream.Read();
            }
            return isNegative ? result * -1 : result;
        }

        private readonly static ConcurrentBag<StringBuilder> StringBuilders = new();

        /// <summary>
        /// Gets a string from a stream reader
        /// </summary>
        /// <param name="stream">The data stream</param>
        /// <param name="delimiter">The data delimiter character to use, default is ','</param>
        /// <returns>The string instance read</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetString(this StreamReader stream, char delimiter = DefaultDelimiter)
        {
            if (!StringBuilders.TryTake(out var builder))
            {
                builder = new();
            }

            try
            {
                var current = (char)stream.Read();

                while (!(current == delimiter || current == '\n' || current == '\r' && (stream.Peek() != '\n' || stream.Read() == '\n') || current == NoMoreData))
                {
                    builder.Append(current);
                    current = (char)stream.Read();
                }
                return builder.ToString();
            }
            finally
            {
                builder.Clear();
                StringBuilders.Add(builder);
            }
        }

        /// <summary>
        /// Gets a character from a stream reader
        /// </summary>
        /// <param name="stream">The data stream</param>
        /// <param name="delimiter">The data delimiter character to use, default is ','</param>
        /// <returns>The string instance read</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetChar(this StreamReader stream, char delimiter = DefaultDelimiter)
        {
            var current = (char)stream.Read();
            var next = (char)stream.Peek();

            if (current == delimiter || current == '\n' || current == '\r' && (next != '\n' || stream.Read() == '\n') || current == NoMoreData)
            {
                return '\0';
            }

            if (next == delimiter || next == '\n' || next == '\r' && stream.Read() == '\r' && stream.Peek() == '\n' || next == NoMoreData)
            {
                // Consume the delimiter
                stream.Read();
            }

            return current;
        }
    }
}
