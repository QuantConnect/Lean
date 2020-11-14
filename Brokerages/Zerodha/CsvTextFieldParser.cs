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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantConnect.Brokerages.Zerodha
{
    

   
        /// <summary>
        /// Parses comma-delimited text files.
        /// </summary>
        /// <remarks>
        /// Based on <code>Microsoft.VisualBasic.FileIO.TextFieldParser</code>.
        /// </remarks>
        public class CsvTextFieldParser : IDisposable
        {
            private TextReader reader;
            private readonly bool leaveOpen = false;
            private string peekedLine = null;
            private int peekedEmptyLineCount = 0;
            private long lineNumber = 0;

            private char delimiterChar = ',';
            private char quoteChar = '"';
            private char quoteEscapeChar = '"';

            /// <summary>
            /// Constructs a parser from the specified input stream.
            /// </summary>
            public CsvTextFieldParser(Stream stream)
                : this(new StreamReader(stream)) { }

            /// <summary>
            /// Constructs a parser from the specified input stream with the specified encoding.
            /// </summary>
            public CsvTextFieldParser(Stream stream, Encoding encoding)
                : this(new StreamReader(stream, encoding)) { }

            /// <summary>
            /// Constructs a parser from the specified input stream with the specified encoding and byte order mark detection option.
            /// </summary>
            public CsvTextFieldParser(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
                : this(new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks)) { }

            /// <summary>
            /// Constructs a parser from the specified input stream with the specified encoding and byte order mark detection option, and optionally leaves the stream open.
            /// </summary>
            public CsvTextFieldParser(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, bool leaveOpen)
                : this(new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks))
            {
                this.leaveOpen = leaveOpen;
            }

            /// <summary>
            /// Constructs a parser from the specified input file path.
            /// </summary>
            public CsvTextFieldParser(string path)
                : this(new StreamReader(path)) { }

            /// <summary>
            /// Constructs a parser from the specified input file path with the specified encoding.
            /// </summary>
            public CsvTextFieldParser(string path, Encoding encoding)
                : this(new StreamReader(path, encoding)) { }

            /// <summary>
            /// Constructs a parser from the specified input file path with the specified encoding and byte order mark detection option.
            /// </summary>
            public CsvTextFieldParser(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
                : this(new StreamReader(path, encoding, detectEncodingFromByteOrderMarks)) { }

            /// <summary>
            /// Constructs a parser from the specified input text reader.
            /// </summary>
            public CsvTextFieldParser(TextReader reader)
            {
                if (reader == null) throw new ArgumentNullException(nameof(reader));
                this.reader = reader;
            }

            /// <summary>
            /// True if there are non-empty lines between the current cursor position and the end of the file.
            /// </summary>
            public bool EndOfData => !HasNextLine();

            /// <summary>
            /// Reads all fields on the current line, returns them as an array of strings, and advances the cursor to the next line containing data.
            /// </summary>
            /// <returns>An array of strings that contains field values for the current line, or null if <see cref="EndOfData"/> is true.</returns>
            /// <exception cref="CsvMalformedLineException">if the parse of the current line failed</exception>
            public string[] ReadFields()
            {
                if (reader == null) return null;

                var line = ReadNextLineWithTrailingEol(ignoreEmptyLines: true);
                if (line == null)
                {
                    return null;
                }

                var fields = new List<string>();
                int startIndex = 0;
                while (startIndex < line.Length)
                {
                    int nextStartIndex;
                    fields.Add(ParseField(ref line, startIndex, out nextStartIndex));
                    startIndex = nextStartIndex;
                }

                // If the last char is the delimiter, then we need to add an extra empty field.
                if (line.Length == 0 || line[line.Length - 1] == delimiterChar)
                {
                    fields.Add(string.Empty);
                }

                return fields.ToArray();
            }

            private string ParseField(ref string line, int startIndex, out int nextStartIndex)
            {
                if (HasFieldsEnclosedInQuotes && line[startIndex] == quoteChar)
                {
                    return ParseFieldAfterOpeningQuote(ref line, startIndex + 1, out nextStartIndex);
                }
                if ((CompatibilityMode || TrimWhiteSpace) && HasFieldsEnclosedInQuotes && char.IsWhiteSpace(line[startIndex]))
                {
                    int leadingWhitespaceCount = line.Skip(startIndex).TakeWhile(ch => char.IsWhiteSpace(ch)).Count();
                    int nonWhitespaceStartIndex = startIndex + leadingWhitespaceCount;
                    if (nonWhitespaceStartIndex < line.Length && line[nonWhitespaceStartIndex] == quoteChar)
                    {
                        return ParseFieldAfterOpeningQuote(ref line, nonWhitespaceStartIndex + 1, out nextStartIndex);
                    }
                }

                string field;
                var delimiterIndex = line.IndexOf(delimiterChar, startIndex);
                if (delimiterIndex >= 0)
                {
                    field = line.Substring(startIndex, delimiterIndex - startIndex);
                    nextStartIndex = delimiterIndex + 1;
                }
                else
                {
                    field = line.Substring(startIndex).TrimEnd('\r', '\n');
                    nextStartIndex = line.Length;
                }
                if (TrimWhiteSpace)
                {
                    field = field.Trim();
                }
                return field;
            }

            private string ParseFieldAfterOpeningQuote(ref string line, int startIndex, out int nextStartIndex)
            {
                var sb = new StringBuilder();
                long currentLineNumber = lineNumber;
                int i = startIndex;
                bool isQuoteClosed = false;

                do
                {
                    while (i < line.Length)
                    {
                        // If we have the escape char and then a quote (or another escape char),
                        // then we need to skip the escape char and just add the char it was escaping.
                        if (line[i] == quoteEscapeChar && i + 1 < line.Length && (line[i + 1] == quoteChar || line[i + 1] == quoteEscapeChar))
                        {
                            sb.Append(line[i + 1]);
                            i += 2;
                            continue;
                        }

                        // If we have an (unescaped) quote char, then we're done parsing this field.
                        if (line[i] == quoteChar)
                        {
                            isQuoteClosed = true;
                            i++;
                            break;
                        }

                        sb.Append(line[i]);
                        i++;
                    }

                    // If the quote is still open and we've read through the entire line, then the field value might contain an EOL character.
                    // That means we need to add the next line to our input and continue parsing this field.
                    if (!isQuoteClosed && i >= line.Length)
                    {
                        // Ignoring empty lines is the wrong thing to do here (we'd be stripping out EOL characters in the middle of a quoted field).
                        // But that's how the VB version works, so we'll do the same in compatibility mode.
                        string nextLine = ReadNextLineWithTrailingEol(ignoreEmptyLines: CompatibilityMode);
                        if (nextLine == null)
                        {
                            // If we're out of lines and still haven't found a closing quote, then this whole line is malformed.
                            throw CreateMalformedLineException(
                                message: $"Line {currentLineNumber} cannot be parsed because a quoted field is not closed.",
                                errorLine: line.TrimEnd('\r', '\n'),
                                errorLineNumber: currentLineNumber
                            );
                        }
                        line += nextLine;
                    }
                } while (!isQuoteClosed && i < line.Length);

                // A line can be considered malformed if there are extra characters after the closing quote.
                bool isMalformed;
                if (isQuoteClosed)
                {
                    if (i >= line.Length)
                    {
                        isMalformed = false;
                    }
                    else if (line[i] == delimiterChar)
                    {
                        isMalformed = false;
                    }
                    else if (line[i] == '\r' || line[i] == '\n')
                    {
                        // If the field ends in an EOL character, then we need to increment past it
                        if (line[i] == '\r' && i + 1 < line.Length && line[i + 1] == '\n')
                        {
                            i++;
                        }
                        i++;
                        isMalformed = false;
                    }
                    else if ((CompatibilityMode || TrimWhiteSpace) && char.IsWhiteSpace(line[i]))
                    {
                        isMalformed = true;

                        // The VB parser allows extra whitespace after the closing quote.
                        // And if that happens at the end of the file, this causes an extra blank space to be added.
                        int nextDelimiterOrEolIndex = line.IndexOfAny(new char[] { delimiterChar, '\r', '\n' }, i);
                        int remainingFieldLength = (nextDelimiterOrEolIndex >= 0 ? nextDelimiterOrEolIndex : line.Length) - i;
                        var isAllRemainingWhitespace = string.IsNullOrWhiteSpace(line.Substring(i, remainingFieldLength));
                        if (isAllRemainingWhitespace)
                        {
                            isMalformed = false;
                            i = nextDelimiterOrEolIndex;
                            if (i < 0)
                            {
                                line += ',';
                                i = line.Length - 1;
                            }
                            else if (line[i] == '\r' || line[i] == '\n')
                            {
                                // If the field ends in an EOL character, then we need to increment past it
                                if (line[i] == '\r' && i + 1 < line.Length && line[i + 1] == '\n')
                                {
                                    i++;
                                }
                                i++;
                            }
                        }
                    }
                    else
                    {
                        isMalformed = true;
                    }
                }
                else
                {
                    isMalformed = true;
                }

                if (isMalformed)
                {
                    throw CreateMalformedLineException(
                        message: $"Line {currentLineNumber} cannot be parsed because of trailing characters after an end quote.",
                        errorLine: line.TrimEnd('\r', '\n'),
                        errorLineNumber: currentLineNumber
                    );
                }
                nextStartIndex = i + 1;
                string field = sb.ToString();
                if (TrimWhiteSpace)
                {
                    field = field.Trim();
                }
                return field;
            }

            private bool HasNextLine()
            {
                if (peekedLine != null)
                {
                    return true;
                }
                if (peekedEmptyLineCount > 0)
                {
                    return false;
                }

                int ignoredEmptyLineCount;
                var nextLine = ReadNextLineWithTrailingEol(ignoreEmptyLines: true, ignoredEmptyLineCount: out ignoredEmptyLineCount);
                if (nextLine == null)
                {
                    if (ignoredEmptyLineCount > 0)
                    {
                        peekedEmptyLineCount = ignoredEmptyLineCount;
                    }
                    return false;
                }

                peekedLine = nextLine;
                peekedEmptyLineCount = ignoredEmptyLineCount;
                return true;
            }

            private string ReadNextLineWithTrailingEol(bool ignoreEmptyLines)
            {
                int ignoredEmptyLineCount;
                var line = ReadNextLineWithTrailingEol(ignoreEmptyLines, out ignoredEmptyLineCount);
                return line;
            }

            private string ReadNextLineWithTrailingEol(bool ignoreEmptyLines, out int ignoredEmptyLineCount)
            {
                var line = ReadNextLineOrWhitespaceWithTrailingEol();

                if (line == null && peekedEmptyLineCount > 0)
                {
                    peekedEmptyLineCount = 0;
                }

                ignoredEmptyLineCount = 0;
                if (ignoreEmptyLines)
                {
                    while (line != null && IsLineEmpty(line))
                    {
                        ignoredEmptyLineCount++;
                        line = ReadNextLineOrWhitespaceWithTrailingEol();
                    }
                }

                return line;
            }

            private string ReadNextLineOrWhitespaceWithTrailingEol()
            {
                if (reader == null) return null;

                if (peekedLine != null)
                {
                    var temp = peekedLine;
                    peekedLine = null;
                    peekedEmptyLineCount = 0;
                    return temp;
                }

                var sb = new StringBuilder();
                while (true)
                {
                    int ch = reader.Read();
                    if (ch == -1) break;

                    sb.Append((char)ch);
                    if (ch == '\r' || ch == '\n')
                    {
                        if (ch == '\r' && reader.Peek() == '\n')
                        {
                            sb.Append((char)reader.Read());
                        }
                        break;
                    }
                }

                if (sb.Length == 0)
                {
                    return null;
                }

                var line = sb.ToString();
                lineNumber++;
                return line;
            }

            private bool IsLineEmpty(string line)
            {
                if (string.IsNullOrEmpty(line)) return true;
                if (line.All(ch => ch == '\r' || ch == '\n')) return true;
                if ((CompatibilityMode || TrimWhiteSpace) && string.IsNullOrWhiteSpace(line)) return true;
                return false;
            }

            /// <summary>
            /// The number of the line that will be returned by <see cref="ReadFields()"/> (starting at 1), or -1 if there are no more lines.
            /// </summary>
            public long LineNumber
            {
                get
                {
                    if (!HasNextLine())
                    {
                        // The VB version will return a line number if there's an empty line at the end of the file.
                        // That's inconsitent with the HasNextLine method, so we'll only do that in compatibility mode.
                        if (peekedEmptyLineCount > 0)
                        {
                            return lineNumber - peekedEmptyLineCount + 1;
                        }
                        return -1;
                    }
                    return lineNumber - peekedEmptyLineCount;
                }
            }

            /// <summary>
            /// Closes the current <see cref="CsvTextFieldParser"/> object.
            /// </summary>
            public void Close()
            {
                if (reader != null)
                {
                    if (!leaveOpen)
                    {
                        reader.Close();
                    }
                    reader = null;
                }

                if (!CompatibilityMode)
                {
                    ErrorLine = string.Empty;
                    ErrorLineNumber = -1L;
                }
            }

            /// <summary>
            /// Closes and disposes the current <see cref="CsvTextFieldParser"/> object.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Disposes of the current <see cref="CsvTextFieldParser"/> object.
            /// </summary>
            /// <param name="disposing">true if called from <see cref="Dispose()"/>, or false if called from a finalizer</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Close();
                }
            }

            #region Error Handling

            /// <summary>
            /// The line that caused the most recent <see cref="CsvMalformedLineException"/>.
            /// </summary>
            /// <remarks>
            /// If no <see cref="CsvMalformedLineException"/> exceptions have been thrown, an empty string is returned.
            /// The <see cref="ErrorLineNumber"/> property can be used to display the number of the line that caused the exception.
            /// </remarks>
            public string ErrorLine { get; private set; } = string.Empty;

            /// <summary>
            /// Returns the number of the line that caused the most recent <see cref="CsvMalformedLineException"/> exception.
            /// </summary>
            /// <remarks>
            /// If no <see cref="CsvMalformedLineException"/> exceptions have been thrown, -1 is returned.
            /// The <see cref="ErrorLine"/> property can be used to display the number of the line that caused the exception.
            /// Blank lines and comments are not ignored when determining the line number.
            /// </remarks>
            public long ErrorLineNumber { get; private set; } = -1;

            private CsvMalformedLineException CreateMalformedLineException(string message, string errorLine, long errorLineNumber)
            {
                ErrorLine = errorLine;
                ErrorLineNumber = errorLineNumber;
                return new CsvMalformedLineException(
                    message: message,
                    lineNumber: errorLineNumber
                );
            }

            #endregion

            #region Configuration

            /// <summary>
            /// True if this parser should exactly reproduce the behavior of the <code>Microsoft.VisualBasic.FileIO.TextFieldParser</code>.
            /// Defaults to <code>false</code>.
            /// </summary>
            public bool CompatibilityMode { get; set; } = false;

            /// <summary>
            /// Defines the delimiters for a text file.
            /// Default is a comma.
            /// </summary>
            /// <remarks>
            /// This is defined as an array of strings for compatibility with <code>Microsoft.VisualBasic.FileIO.TextFieldParser</code>,
            /// but this parser only supports one single-character delimiter.
            /// </remarks>
            /// <exception cref="ArgumentException">A delimiter value is set to a newline character, an empty string, or null.</exception>
            /// <exception cref="NotSupportedException">The delimiters are set to an array that does not contain exactly one element with exactly one character.</exception>
            public string[] Delimiters
            {
                get
                {
                    return new string[] { delimiterChar.ToString(CultureInfo.InvariantCulture) };
                }
                set
                {
                    if (value == null || !value.Any())
                    {
                        throw new NotSupportedException("This parser requires a delimiter");
                    }
                    if (value.Length > 1)
                    {
                        throw new NotSupportedException("This parser does not support multiple delimiters.");
                    }

                    var delimiterString = value.Single();
                    if (string.IsNullOrEmpty(delimiterString))
                    {
                        throw new ArgumentException("A delimiter cannot be null or an empty string.");
                    }
                    if (delimiterString.Length > 1)
                    {
                        throw new NotSupportedException("This parser does not support a delimiter with multiple characters.");
                    }
                    SetDelimiter(delimiterString.Single());
                }
            }

            /// <summary>
            /// Sets the delimiter character used by this parser.
            /// Default is a comma.
            /// </summary>
            /// <exception cref="ArgumentException">The delimiter character is set to a newline character.</exception>
            public void SetDelimiter(char delimiterChar)
            {
                if (delimiterChar == '\n' || delimiterChar == '\r')
                {
                    throw new ArgumentException("This parser does not support delimiters that contain end-of-line characters");
                }
                this.delimiterChar = delimiterChar;
            }

            /// <summary>
            /// Sets the quote character used by this parser, and also sets the quote escape character to match if it previously matched.
            /// Default is a double quote character.
            /// </summary>
            /// <exception cref="ArgumentException">The quote character is set to a newline character.</exception>
            public void SetQuoteCharacter(char quoteChar)
            {
                if (quoteChar == '\n' || quoteChar == '\r')
                {
                    throw new ArgumentException("This parser does not support end-of-line characters as a quote character");
                }

                // If the quote and escape characters currently match, then make sure they still match after we change the quote character.
                if (this.quoteChar == this.quoteEscapeChar)
                {
                    this.quoteEscapeChar = quoteChar;
                }
                this.quoteChar = quoteChar;
            }

            /// <summary>
            /// Sets the quote escape character used by this parser.
            /// Default is the same as the quote character, a double quote character.
            /// </summary>
            /// <exception cref="ArgumentException">The quote escape character is set to a newline character.</exception>
            public void SetQuoteEscapeCharacter(char quoteEscapeChar)
            {
                if (quoteEscapeChar == '\n' || quoteEscapeChar == '\r')
                {
                    throw new ArgumentException("This parser does not support end-of-line characters as a quote escape character");
                }
                this.quoteEscapeChar = quoteEscapeChar;
            }

            /// <summary>
            /// Denotes whether fields are enclosed in quotation marks when a CSV file is being parsed.
            /// Defaults to <code>true</code>.
            /// </summary>
            public bool HasFieldsEnclosedInQuotes { get; set; } = true;

            /// <summary>
            /// Indicates whether leading and trailing white space should be trimmed from field values.
            /// Defaults to <code>false</code>.
            /// </summary>
            public bool TrimWhiteSpace { get; set; } = false;

            #endregion
        }

        /// <summary>
        /// An exception that is thrown when the <see cref="CsvTextFieldParser.ReadFields"/> method cannot parse a row using the specified format.
        /// </summary>
        /// <remarks>
        /// Based on <code>Microsoft.VisualBasic.FileIO.MalformedLineException.MalformedLineException</code>.
        /// </remarks>
        public class CsvMalformedLineException : FormatException
        {
            /// <summary>
            /// Constructs an exception with a specified message and a line number.
            /// </summary>
            public CsvMalformedLineException(string message, long lineNumber)
                : base(message)
            {
                LineNumber = lineNumber;
            }

            /// <summary>
            /// Constructs an exception with a specified message, a line number, and a reference to the inner exception that is the cause of this exception.
            /// </summary>
            public CsvMalformedLineException(string message, long lineNumber, Exception innerException)
                : base(message, innerException)
            {
                LineNumber = lineNumber;
            }

            /// <summary>
            /// The line number of the malformed line.
            /// </summary>
            public long LineNumber { get; }
        }
}
