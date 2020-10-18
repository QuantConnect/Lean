using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QuantConnect.Brokerages.Zerodha
{
    /// *************************************************************************
    /// ;TextFieldParser
    /// <summary>
    /// Port of .NET Framework's TextFieldParser to .NET Core.
    /// Original source: https://github.com/microsoft/referencesource/blob/master/Microsoft.VisualBasic/runtime/msvbalib/FileIO/TextFieldParser.vb
    /// Enables parsing very large delimited or fixed width field files
    /// </summary>
    /// <remarks></remarks>
    public class TextFieldParser : IDisposable
    {

        // ==PUBLIC**************************************************************

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse the passed in file
        /// </summary>
        /// <param name="path">The path of the file to be parsed</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(string path)
        {

            // Default to UTF-8 and detect encoding
            InitializeFromPath(path, Encoding.UTF8, true);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse the passed in file
        /// </summary>
        /// <param name="path">The path of the file to be parsed</param>
        /// <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(string path, Encoding defaultEncoding)
        {

            // Default to detect encoding
            InitializeFromPath(path, defaultEncoding, true);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse the passed in file
        /// </summary>
        /// <param name="path">The path of the file to be parsed</param>
        /// <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        /// <param name="detectEncoding">Indicates whether or not to try to detect the encoding from the BOM</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(string path, Encoding defaultEncoding, bool detectEncoding)
        {
            InitializeFromPath(path, defaultEncoding, detectEncoding);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse a file represented by the passed in stream
        /// </summary>
        /// <param name="stream"></param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(Stream stream)
        {

            // Default to UTF-8 and detect encoding
            InitializeFromStream(stream, Encoding.UTF8, true);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse a file represented by the passed in stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(Stream stream, Encoding defaultEncoding)
        {

            // Default to detect encoding
            InitializeFromStream(stream, defaultEncoding, true);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse a file represented by the passed in stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        /// <param name="detectEncoding">Indicates whether or not to try to detect the encoding from the BOM</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(Stream stream, Encoding defaultEncoding, bool detectEncoding)
        {
            InitializeFromStream(stream, defaultEncoding, detectEncoding);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse a file represented by the passed in stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="defaultEncoding">The decoding to default to if encoding isn't determined from file</param>
        /// <param name="detectEncoding">Indicates whether or not to try to detect the encoding from the BOM</param>
        /// <param name="leaveOpen">Indicates whether or not to leave the passed in stream open</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(Stream stream, Encoding defaultEncoding, bool detectEncoding, bool leaveOpen)
        {
            m_LeaveOpen = leaveOpen;
            InitializeFromStream(stream, defaultEncoding, detectEncoding);
        }

        /// *********************************************************************
        /// ;New
        /// <summary>
        /// Creates a new TextFieldParser to parse a stream or file represented by the passed in TextReader
        /// </summary>
        /// <param name="reader">The TextReader that does the reading</param>
        /// <remarks></remarks>
        //[HostProtection(Resources = HostProtectionResource.ExternalProcessMgmt)]
        public TextFieldParser(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            m_Reader = reader;

            ReadToBuffer();
        }

        /// **********************************************************************
        /// ;CommentTokens
        /// <summary>
        /// An array of the strings that indicate a line is a comment
        /// </summary>
        /// <value>An array of comment indicators</value>
        /// <remarks>Returns an empty array if not set</remarks>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public string[] CommentTokens
        {
            get
            {
                return m_CommentTokens;
            }
            set
            {
                CheckCommentTokensForWhitespace(value);
                m_CommentTokens = value;
                m_NeedPropertyCheck = true;
            }
        }

        /// *******************************************************************
        /// ;EndOfData
        /// <summary>
        /// Indicates whether or not there is any data (non ignorable lines) left to read in the file
        /// </summary>
        /// <value>True if there's more data to read, otherwise False</value>
        /// <remarks>Ignores comments and blank lines</remarks>
        public bool EndOfData
        {
            get
            {
                if (m_EndOfData)
                    return m_EndOfData;

                // Make sure we're not at end of file
                if (m_Reader == null | m_Buffer == null)
                {
                    m_EndOfData = true;
                    return true;
                }

                // See if we can get a data line
                if (PeekNextDataLine() != null)
                    return false;

                m_EndOfData = true;
                return true;
            }
        }

        /// *******************************************************************
        /// ;LineNumber
        /// <summary>
        /// The line to the right of the cursor.
        /// </summary>
        /// <value>The number of the line</value>
        /// <remarks>LineNumber returns the location in the file and has nothing to do with rows or fields</remarks>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public long LineNumber
        {
            get
            {
                if (m_LineNumber != (long)-1)
                {

                    // See if we're at the end of file
                    if ((m_Reader.Peek() == -1) & (m_Position == m_CharsRead))
                        CloseReader();
                }

                return m_LineNumber;
            }
        }

        /// *******************************************************************
        /// ;ErrorLine
        /// <summary>
        /// Returns the last malformed line if there is one.
        /// </summary>
        /// <value>The last malformed line</value>
        /// <remarks></remarks>
        public string ErrorLine
        {
            get
            {
                return m_ErrorLine;
            }
        }

        /// *******************************************************************
        /// ;ErrorLineNumber
        /// <summary>
        /// Returns the line number of last malformed line if there is one.
        /// </summary>
        /// <value>The last malformed line line number</value>
        /// <remarks></remarks>
        public long ErrorLineNumber
        {
            get
            {
                return m_ErrorLineNumber;
            }
        }

        /// *******************************************************************
        /// ;TextFieldType
        /// <summary>
        /// Indicates the type of file being read, either fixed width or delimited
        /// </summary>
        /// <value>The type of fields in the file</value>
        /// <remarks></remarks>
        public FieldType TextFieldType
        {
            get
            {
                return m_TextFieldType;
            }
            set
            {
                ValidateFieldTypeEnumValue(value, "value");
                m_TextFieldType = value;
                m_NeedPropertyCheck = true;
            }
        }

        /// ******************************************************************
        /// ;FieldWidths
        /// <summary>
        /// Gets or sets the widths of the fields for reading a fixed width file
        /// </summary>
        /// <value>An array of the widths</value>
        /// <remarks></remarks>
        public int[] FieldWidths
        {
            get
            {
                return m_FieldWidths;
            }
            set
            {
                if (value != null)
                {
                    ValidateFieldWidthsOnInput(value);

                    // Keep a copy so we can determine if the user changes elements of the array
                    m_FieldWidthsCopy = (int[])value.Clone();
                }
                else
                    m_FieldWidthsCopy = null;

                m_FieldWidths = value;
                m_NeedPropertyCheck = true;
            }
        }

        /// ********************************************************************
        /// ;Delimiters
        /// <summary>
        /// Gets or sets the delimiters used in a file
        /// </summary>
        /// <value>An array of the delimiters</value>
        /// <remarks></remarks>
        public string[] Delimiters
        {
            get
            {
                return m_Delimiters;
            }
            set
            {
                if (value != null)
                {
                    ValidateDelimiters(value);

                    // Keep a copy so we can determine if the user changes elements of the array
                    m_DelimitersCopy = (string[])value.Clone();
                }
                else
                    m_DelimitersCopy = null;

                m_Delimiters = value;

                m_NeedPropertyCheck = true;

                // Force rebuilding of regex
                m_BeginQuotesRegex = null;
            }
        }

        /// *******************************************************************
        /// ;SetDelimiters
        /// <summary>
        /// Helper function to enable setting delimiters without diming an array
        /// </summary>
        /// <param name="delimiters">A list of the delimiters</param>
        /// <remarks></remarks>
        public void SetDelimiters(params string[] delimiters)
        {
            this.Delimiters = delimiters;
        }

        /// *******************************************************************
        /// ;SetFieldWidths
        /// <summary>
        /// Helper function to enable setting field widths without diming an array
        /// </summary>
        /// <param name="fieldWidths">A list of field widths</param>
        /// <remarks></remarks>
        public void SetFieldWidths(params int[] fieldWidths)
        {
            this.FieldWidths = fieldWidths;
        }

        /// *******************************************************************
        /// ;TrimWhiteSpace
        /// <summary>
        /// Indicates whether or not leading and trailing white space should be removed when returning a field
        /// </summary>
        /// <value>True if white space should be removed, otherwise False</value>
        /// <remarks></remarks>
        public bool TrimWhiteSpace
        {
            get
            {
                return m_TrimWhiteSpace;
            }
            set
            {
                m_TrimWhiteSpace = value;
            }
        }

        /// ********************************************************************
        /// ;ReadLine
        /// <summary>
        /// Reads and returns the next line from the file
        /// </summary>
        /// <returns>The line read or Nothing if at the end of the file</returns>
        /// <remarks>This is data unaware method. It simply reads the next line in the file.</remarks>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public string ReadLine()
        {
            if (m_Reader == null | m_Buffer == null)
                return null;

            string Line;

            // Set the method to be used when we reach the end of the buffer
            ChangeBufferFunction BufferFunction = new ChangeBufferFunction(ReadToBuffer);

            Line = ReadNextLine(ref m_Position, BufferFunction);

            if (Line == null)
            {
                FinishReading();
                return null;
            }
            else
            {
                m_LineNumber += 1;
                return Line.TrimEnd((char)13, (char)10);
            }
        }

        /// *******************************************************************
        /// ;ReadFields
        /// <summary>
        /// Reads a non ignorable line and parses it into fields
        /// </summary>
        /// <returns>The line parsed into fields</returns>
        /// <remarks>This is a data aware method. Comments and blank lines are ignored.</remarks>
        public string[] ReadFields()
        {
            if (m_Reader == null | m_Buffer == null)
                return null;

            ValidateReadyToRead();

            switch (m_TextFieldType)
            {
                case FieldType.FixedWidth:
                    {
                        return ParseFixedWidthLine();
                    }

                case FieldType.Delimited:
                    {
                        return ParseDelimitedLine();
                    }

                default:
                    {
                        Console.WriteLine("The TextFieldType is not supported");
                        break;
                    }
            }
            return null;
        }

        /// ********************************************************************
        /// ;PeekChars
        /// <summary>
        /// Enables looking at the passed in number of characters of the next data line without reading the line
        /// </summary>
        /// <param name="numberOfChars"></param>
        /// <returns>A string consisting of the first NumberOfChars characters of the next line</returns>
        /// <remarks>If numberOfChars is greater than the next line, only the next line is returned</remarks>
        public string PeekChars(int numberOfChars)
        {
            if (numberOfChars <= 0)
                throw new ArgumentException("", nameof(numberOfChars)); //GetArgumentExceptionWithArgName["numberOfChars", ResID.MyID.TextFieldParser_NumberOfCharsMustBePositive, "numberOfChars"];

            if (m_Reader == null | m_Buffer == null)
                return null;

            // If we know there's no more data return Nothing
            if (m_EndOfData)
                return null;

            // Get the next line without reading it
            string Line = PeekNextDataLine();

            if (Line == null)
            {
                m_EndOfData = true;
                return null;
            }

            // Strip of end of line chars
            Line = Line.TrimEnd((char)13, (char)10);

            // If the number of chars is larger than the line, return the whole line. Otherwise
            // return the NumberOfChars characters from the beginning of the line
            if (Line.Length < numberOfChars)
                return Line;
            else
            {
                //StringInfo info = new StringInfo(Line);
                //return info.SubstringByTextElements(0, numberOfChars);
                return Line.Substring(0, numberOfChars);
            }
        }

        /// ********************************************************************
        /// ;ReadToEnd
        /// <summary>
        /// Reads the file starting at the current position and moving to the end of the file
        /// </summary>
        /// <returns>The contents of the file from the current position to the end of the file</returns>
        /// <remarks>This is not a data aware method. Everything in the file from the current position to the end is read</remarks>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public string ReadToEnd()
        {
            if (m_Reader == null | m_Buffer == null)
                return null;


            System.Text.StringBuilder Builder = new System.Text.StringBuilder(m_Buffer.Length);

            // Get the lines in the Buffer first
            Builder.Append(m_Buffer, m_Position, m_CharsRead - m_Position);

            // Add what we haven't read
            Builder.Append(m_Reader.ReadToEnd());

            FinishReading();

            return Builder.ToString();
        }

        /// *********************************************************************
        /// ;HasFieldsEnclosedInQuotes
        /// <summary>
        /// Indicates whether or not to handle quotes in a csv friendly way
        /// </summary>
        /// <value>True if we escape quotes otherwise false</value>
        /// <remarks></remarks>
        //[EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool HasFieldsEnclosedInQuotes
        {
            get
            {
                return m_HasFieldsEnclosedInQuotes;
            }
            set
            {
                m_HasFieldsEnclosedInQuotes = value;
            }
        }

        /// **********************************************************************
        /// ;Close
        /// <summary>
        /// Closes the StreamReader
        /// </summary>
        /// <remarks></remarks>
        public void Close()
        {
            CloseReader();
        }

        /// **********************************************************************
        /// ;Dispose
        /// <summary>
        /// Closes the StreamReader
        /// </summary>
        /// <remarks></remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // ==PROTECTED**************************************************************

        /// ***********************************************************************
        /// ;Dispose
        /// <summary>
        /// Standard implementation of IDisposable.Dispose for non sealed classes. Classes derived from
        /// TextFieldParser should override this method. After doing their own cleanup, they should call
        /// this method (MyBase.Dispose(disposing))
        /// </summary>
        /// <param name="disposing">Indicates we are called by Dispose and not GC</param>
        /// <remarks></remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.m_Disposed)
                    Close();
                this.m_Disposed = true;
            }
        }

        /// **************************************************************************
        /// ;ValidateFieldTypeEnumValue
        /// <summary>
        /// Validates that the value being passed as an AudioPlayMode enum is a legal value
        /// </summary>
        /// <param name="value"></param>
        /// <remarks></remarks>
        private void ValidateFieldTypeEnumValue(FieldType value, string paramName)
        {
            if ((int)value < (int)FieldType.Delimited || (int)value > (int)FieldType.FixedWidth)
                throw new ArgumentException("", nameof(value)); //(paramName, (int)value, typeof(FieldType));
        }


        /// *******************************************************************************
        /// ;Finalize
        /// <summary>
        /// Clean up following dispose pattern
        /// </summary>
        /// <remarks></remarks>
        ~TextFieldParser()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(false);
        }

        // ==PRIVATE**************************************************************

        /// **********************************************************************
        /// ;CloseReader
        /// <summary>
        /// Closes the StreamReader
        /// </summary>
        /// <remarks></remarks>
        private void CloseReader()
        {
            FinishReading();
            if (m_Reader != null)
            {
                if (!m_LeaveOpen)
                    m_Reader.Dispose();
                m_Reader = null;
            }
        }

        /// **********************************************************************
        /// ;FinishReading
        /// <summary>
        /// Cleans up managed resources except the StreamReader and indicates reading is finished
        /// </summary>
        /// <remarks></remarks>
        private void FinishReading()
        {
            m_LineNumber = -1;
            m_EndOfData = true;
            m_Buffer = null;
            m_DelimiterRegex = null;
            m_BeginQuotesRegex = null;
        }

        /// ;InitializeFromPath
        /// <summary>
        /// Creates a StreamReader for the passed in Path
        /// </summary>
        /// <param name="path">The passed in path</param>
        /// <param name="defaultEncoding">The encoding to default to if encoding can't be detected</param>
        /// <param name="detectEncoding">Indicates whether or not to detect encoding from the BOM</param>
        /// <remarks>We validate the arguments here for the three Public constructors that take a Path</remarks>
        private void InitializeFromPath(string path, System.Text.Encoding defaultEncoding, bool detectEncoding)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (defaultEncoding == null)
                throw new ArgumentNullException(nameof(defaultEncoding));

            string fullPath = ValidatePath(path);
            FileStream fileStreamTemp = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            m_Reader = new StreamReader(fileStreamTemp, defaultEncoding, detectEncoding);

            ReadToBuffer();
        }

        /// *********************************************************************
        /// ;InitializeFromStream
        /// <summary>
        /// Creates a StreamReader for a passed in stream
        /// </summary>
        /// <param name="stream">The passed in stream</param>
        /// <param name="defaultEncoding">The encoding to default to if encoding can't be detected</param>
        /// <param name="detectEncoding">Indicates whether or not to detect encoding from the BOM</param>
        /// <remarks>We validate the arguments here for the three Public constructors that take a Stream</remarks>
        private void InitializeFromStream(Stream stream, System.Text.Encoding defaultEncoding, bool detectEncoding)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("", nameof(stream)); //GetArgumentExceptionWithArgName["stream", ResID.MyID.TextFieldParser_StreamNotReadable, "stream"];

            if (defaultEncoding == null)
                throw new ArgumentNullException(nameof(defaultEncoding));

            m_Reader = new StreamReader(stream, defaultEncoding, detectEncoding);

            ReadToBuffer();
        }


        /// **********************************************************************
        /// ;ValidatePath
        /// <summary>
        /// Gets full name and path from passed in path.
        /// </summary>
        /// <param name="path">The path to be validated</param>
        /// <returns>The full name and path</returns>
        /// <remarks>Throws if the file doesn't exist or if the path is malformed</remarks>
        private string ValidatePath(string path)
        {

            // Validate and get full path
            string fullPath = path; // FileSystem.NormalizeFilePath(path, "path");

            // Make sure the file exists
            if (!File.Exists(fullPath))
                throw new System.IO.FileNotFoundException(fullPath);//GetResourceString[ResID.MyID.IO_FileNotFound_Path, fullPath]);

            return fullPath;
        }

        /// ***********************************************************************
        /// ;IgnoreLine
        /// <summary>
        /// Indicates whether or not the passed in line should be ignored
        /// </summary>
        /// <param name="line">The line to be tested</param>
        /// <returns>True if the line should be ignored, otherwise False</returns>
        /// <remarks>Lines to ignore are blank lines and comments</remarks>
        private bool IgnoreLine(string line)
        {

            // If the Line is Nothing, it has meaning (we've reached the end of the file) so don't
            // ignore it
            if (line == null)
                return false;

            // Ignore empty or whitespace lines
            string TrimmedLine = line.Trim();
            if (TrimmedLine.Length == 0)
                return true;

            // Ignore comments
            if (m_CommentTokens != null)
            {
                foreach (string Token in m_CommentTokens)
                {
                    if (string.IsNullOrEmpty(Token))
                        continue;

                    if (TrimmedLine.StartsWith(Token, StringComparison.Ordinal))
                        return true;

                    // Test original line in case whitespace char is a coment token
                    if (line.StartsWith(Token, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        /// ***********************************************************************
        /// ;ReadToBuffer
        /// <summary>
        /// Reads characters from the file into the buffer
        /// </summary>
        /// <returns>The number of Chars read. If no Chars are read, we're at the end of the file</returns>
        /// <remarks></remarks>
        private int ReadToBuffer()
        {
            //Debug.Assert(m_Buffer != null, "There's no buffer");
            //Debug.Assert(m_Reader != null, "There's no StreamReader");

            // Set cursor to beginning of buffer
            m_Position = 0;
            int BufferLength = m_Buffer.Length;
            //Debug.Assert(BufferLength >= DEFAULT_BUFFER_LENGTH, "Buffer shrunk to below default");

            // If the buffer has grown, shrink it back to the default size
            if (BufferLength > DEFAULT_BUFFER_LENGTH)
            {
                BufferLength = DEFAULT_BUFFER_LENGTH;
                m_Buffer = new char[BufferLength - 1 + 1];
            }

            // Read from the stream
            m_CharsRead = m_Reader.Read(m_Buffer, 0, BufferLength);

            // Return the number of Chars read
            return m_CharsRead;
        }

        /// ************************************************************************
        /// ;SlideCursorToStartOfBuffer
        /// <summary>
        /// Moves the cursor and all the data to the right of the cursor to the front of the buffer. It
        /// then fills the remainder of the buffer from the file
        /// </summary>
        /// <returns>The number of Chars read in filling the remainder of the buffer</returns>
        /// <remarks>
        /// This should be called when we want to make maximum use of the space in the buffer. Characters
        /// to the left of the cursor have already been read and can be discarded.
        /// </remarks>
        private int SlideCursorToStartOfBuffer()
        {
            //Debug.Assert(m_Buffer != null, "There's no buffer");
            //Debug.Assert(m_Reader != null, "There's no StreamReader");
            //Debug.Assert((m_Position >= 0) & (m_Position <= m_Buffer.Length), "The cursor is out of range");

            // No need to slide if we're already at the beginning
            if (m_Position > 0)
            {
                int BufferLength = m_Buffer.Length;
                char[] TempArray = new char[BufferLength - 1 + 1];
                Array.Copy(m_Buffer, m_Position, TempArray, 0, BufferLength - m_Position);

                // Fill the rest of the buffer
                int CharsRead = m_Reader.Read(TempArray, BufferLength - m_Position, m_Position);
                m_CharsRead = (m_CharsRead - m_Position) + CharsRead;

                m_Position = 0;
                m_Buffer = TempArray;

                return CharsRead;
            }

            return 0;
        }

        /// *********************************************************************
        /// ;IncreaseBufferSize
        /// <summary>
        /// Increases the size of the buffer. Used when we are at the end of the buffer, we need
        /// to read more data from the file, and we can't discard what we've already read.
        /// </summary>
        /// <returns>The number of characters read to fill the new buffer</returns>
        /// <remarks>This is needed for PeekChars and EndOfData</remarks>
        private int IncreaseBufferSize()
        {
            //Debug.Assert(m_Buffer != null, "There's no buffer");
            //Debug.Assert(m_Reader != null, "There's no StreamReader");

            // Set cursor
            m_PeekPosition = m_CharsRead;

            // Create a larger buffer and copy our data into it
            int BufferSize = m_Buffer.Length + DEFAULT_BUFFER_LENGTH;

            // Make sure the buffer hasn't grown too large
            if (BufferSize > m_MaxBufferSize)
                throw new InvalidOperationException();// GetInvalidOperationException[ResID.MyID.TextFieldParser_BufferExceededMaxSize];

            char[] TempArray = new char[BufferSize - 1 + 1];

            Array.Copy(m_Buffer, TempArray, m_Buffer.Length);
            int CharsRead = m_Reader.Read(TempArray, m_Buffer.Length, DEFAULT_BUFFER_LENGTH);
            m_Buffer = TempArray;
            m_CharsRead += CharsRead;

            //Debug.Assert(m_CharsRead <= BufferSize, "We've read more chars than we have space for");

            return CharsRead;
        }

        /// **********************************************************************
        /// ;ReadNextDataLine
        /// <summary>
        /// Returns the next line of data or nothing if there's no more data to be read
        /// </summary>
        /// <returns>The next line of data</returns>
        /// <remarks>Moves the cursor past the line read</remarks>
        private string ReadNextDataLine()
        {
            string Line = default(string);

            // Set function to use when we reach the end of the buffer
            ChangeBufferFunction BufferFunction = new ChangeBufferFunction(ReadToBuffer);

            do
            {
                Line = ReadNextLine(ref m_Position, BufferFunction);
                m_LineNumber += 1;
            }
            while (IgnoreLine(Line));

            if (Line == null)
                CloseReader();

            return Line;
        }

        /// ***********************************************************************
        /// ;PeekNextDataLine
        /// <summary>
        /// Returns the next data line but doesn't move the cursor
        /// </summary>
        /// <returns>The next data line, or Nothing if there's no more data</returns>
        /// <remarks></remarks>
        private string PeekNextDataLine()
        {
            string Line = default(string);

            // Set function to use when we reach the end of the buffer
            ChangeBufferFunction BufferFunction = new ChangeBufferFunction(IncreaseBufferSize);

            // Slide the data to the left so that we make maximum use of the buffer
            SlideCursorToStartOfBuffer();
            m_PeekPosition = 0;

            do
                Line = ReadNextLine(ref m_PeekPosition, BufferFunction);
            while (IgnoreLine(Line));

            return Line;
        }

        /// *********************************************************************
        /// ;ChangeBufferFunction
        /// <summary>
        /// Function to call when we're at the end of the buffer. We either re fill the buffer
        /// or change the size of the buffer
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        private delegate int ChangeBufferFunction();

        /// **********************************************************************
        /// ;ReadNextLine
        /// <summary>
        /// Gets the next line from the file and moves the pased in cursor past the line
        /// </summary>
        /// <param name="Cursor">Indicates the current position in the buffer</param>
        /// <param name="ChangeBuffer">Function to call when we've reached the end of the buffer</param>
        /// <returns>The next line in the file</returns>
        /// <remarks>Returns Nothing if we are at the end of the file</remarks>
        private string ReadNextLine(ref int Cursor, ChangeBufferFunction ChangeBuffer)
        {
            //Debug.Assert(m_Buffer != null, "There's no buffer");
            //Debug.Assert((Cursor >= 0) & (Cursor <= m_CharsRead), "The cursor is out of range");

            // Check to see if the cursor is at the end of the chars in the buffer. If it is, re fill the buffer
            if (Cursor == m_CharsRead)
            {
                if (ChangeBuffer() == 0)

                    // We're at the end of the file
                    return null;
            }

            StringBuilder Builder = null;
            do
            {
                var loopTo = m_CharsRead - 1;
                // Walk through buffer looking for the end of a line. End of line can be vbLf (\n), vbCr (\r) or vbCrLf (\r\n)
                for (int i = Cursor; i <= loopTo; i++)
                {
                    char Character = m_Buffer[i];
                    if (Character == '\r' || Character == '\n')
                    {

                        // We've found the end of a line so add everything we've read so far to the
                        // builder. We include the end of line char because we need to know what it is
                        // in case it's embedded in a field.
                        if (Builder != null)
                            Builder.Append(m_Buffer, Cursor, (i - Cursor) + 1);
                        else
                        {
                            Builder = new StringBuilder(i + 1);
                            Builder.Append(m_Buffer, Cursor, (i - Cursor) + 1);
                        }

                        Cursor = i + 1;

                        // See if vbLf should be added as well
                        if (Character == '\r')
                        {
                            if (Cursor < m_CharsRead)
                            {
                                if (m_Buffer[Cursor] == '\n')
                                {
                                    Cursor += 1;
                                    Builder.Append('\n');
                                }
                            }
                            else if (ChangeBuffer() > 0)
                            {
                                if (m_Buffer[Cursor] == '\n')
                                {
                                    Cursor += 1;
                                    Builder.Append('\n');
                                }
                            }
                        }

                        return Builder.ToString();
                    }
                }

                // We've searched the whole buffer and haven't found an end of line. Save what we have, and read more to the buffer.
                int Size = m_CharsRead - Cursor;
                if (Builder == null)
                    Builder = new StringBuilder(Size + DEFAULT_BUILDER_INCREASE);
                Builder.Append(m_Buffer, Cursor, Size);
            }
            while (ChangeBuffer() > 0);

            return Builder.ToString();
        }

        /// ***************************************************************
        /// ;ParseDelimitedLine
        /// <summary>
        /// Gets the next data line and parses it with the delimiters
        /// </summary>
        /// <returns>An array of the fields in the line</returns>
        /// <remarks></remarks>
        private string[] ParseDelimitedLine()
        {
            string Line = ReadNextDataLine();
            if (Line == null)
                return null;

            // The line number is that of the line just read
            long CurrentLineNumber = m_LineNumber - (long)1;

            int Index = 0;
            System.Collections.Generic.List<string> Fields = new System.Collections.Generic.List<string>();
            string Field = default(string);
            int LineEndIndex = GetEndOfLineIndex(Line);

            while (Index <= LineEndIndex)
            {

                // Is the field delimited in quotes? We only care about this if
                // EscapedQuotes is True
                Match MatchResult = null;
                bool QuoteDelimited = false;

                if (m_HasFieldsEnclosedInQuotes)
                {
                    MatchResult = BeginQuotesRegex.Match(Line, Index);
                    QuoteDelimited = MatchResult.Success;
                }

                if (QuoteDelimited)
                {

                    // Move the Index beyond quote
                    Index = MatchResult.Index + MatchResult.Length;
                    // Look for the closing "
                    QuoteDelimitedFieldBuilder EndHelper = new QuoteDelimitedFieldBuilder(m_DelimiterWithEndCharsRegex, m_SpaceChars);
                    EndHelper.BuildField(Line, Index);

                    if (EndHelper.MalformedLine)
                    {
                        m_ErrorLine = Line.TrimEnd((char)13, (char)10);
                        m_ErrorLineNumber = CurrentLineNumber;
                        throw new FormatException(); //MalformedLineException(GetResourceString[ResID.MyID.TextFieldParser_MalFormedDelimitedLine, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)], CurrentLineNumber);
                    }

                    if (EndHelper.FieldFinished)
                    {
                        Field = EndHelper.Field;
                        Index = EndHelper.Index + EndHelper.DelimiterLength;
                    }
                    else
                    {
                        // We may have an embedded line end character, so grab next line
                        string NewLine = default(string);
                        int EndOfLine = default(int);

                        do
                        {
                            EndOfLine = Line.Length;
                            // Get the next data line
                            NewLine = ReadNextDataLine();

                            // If we didn't get a new line, we're at the end of the file so our original line is mal formed
                            if (NewLine == null)
                            {
                                m_ErrorLine = Line.TrimEnd((char)13, (char)10);
                                m_ErrorLineNumber = CurrentLineNumber;
                                throw new FormatException(); // MalformedLineException(GetResourceString[ResID.MyID.TextFieldParser_MalFormedDelimitedLine, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)], CurrentLineNumber);
                            }

                            if ((Line.Length + NewLine.Length) > m_MaxLineSize)
                            {
                                m_ErrorLine = Line.TrimEnd((char)13, (char)10);
                                m_ErrorLineNumber = CurrentLineNumber;
                                throw new FormatException(); // MalformedLineException(GetResourceString[ResID.MyID.TextFieldParser_MaxLineSizeExceeded, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)], CurrentLineNumber);
                            }

                            Line += NewLine;
                            LineEndIndex = GetEndOfLineIndex(Line);
                            EndHelper.BuildField(Line, EndOfLine);
                            if (EndHelper.MalformedLine)
                            {
                                m_ErrorLine = Line.TrimEnd((char)13, (char)10);
                                m_ErrorLineNumber = CurrentLineNumber;
                                throw new FormatException(); // MalformedLineException(GetResourceString[ResID.MyID.TextFieldParser_MalFormedDelimitedLine, CurrentLineNumber.ToString(CultureInfo.InvariantCulture)], CurrentLineNumber);
                            }
                        }
                        while (!EndHelper.FieldFinished);

                        Field = EndHelper.Field;
                        Index = EndHelper.Index + EndHelper.DelimiterLength;
                    }

                    if (m_TrimWhiteSpace)
                        Field = Field.Trim();

                    Fields.Add(Field);
                }
                else
                {
                    // Find the next delimiter
                    Match DelimiterMatch = m_DelimiterRegex.Match(Line, Index);
                    if (DelimiterMatch.Success)
                    {
                        Field = Line.Substring(Index, DelimiterMatch.Index - Index);

                        if (m_TrimWhiteSpace)
                            Field = Field.Trim();

                        Fields.Add(Field);

                        // Move the index
                        Index = DelimiterMatch.Index + DelimiterMatch.Length;
                    }
                    else
                    {
                        // We're at the end of the line so the field consists of all that's left of the line
                        // minus the end of line chars
                        Field = Line.Substring(Index).TrimEnd((char)13, (char)10);

                        if (m_TrimWhiteSpace)
                            Field = Field.Trim();
                        Fields.Add(Field);
                        break;
                    }
                }
            }

            return Fields.ToArray();
        }


        /// ****************************************************************
        /// ;ParseFixedWidthLine
        /// <summary>
        /// Gets the next data line and parses into fixed width fields
        /// </summary>
        /// <returns>An array of the fields in the line</returns>
        /// <remarks></remarks>
        private string[] ParseFixedWidthLine()
        {
            //Debug.Assert(m_FieldWidths != null, "No field widths");

            string Line = ReadNextDataLine();

            if (Line == null)
                return null;

            // Strip off trailing carriage return or line feed
            Line = Line.TrimEnd((char)13, (char)10);

            StringInfo LineInfo = new StringInfo(Line);
            ValidateFixedWidthLine(LineInfo, m_LineNumber - (long)1);

            int Index = 0;
            int Bound = m_FieldWidths.Length - 1;
            string[] Fields = new string[Bound + 1];
            var loopTo = Bound;
            for (int i = 0; i <= loopTo; i++)
            {
                Fields[i] = GetFixedWidthField(LineInfo, Index, m_FieldWidths[i]);
                Index += m_FieldWidths[i];
            }

            return Fields;
        }

        /// *****************************************************************
        /// ;GetFixedWidthField
        /// <summary>
        /// Returns the field at the passed in index
        /// </summary>
        /// <param name="Line">The string containing the fields</param>
        /// <param name="Index">The start of the field</param>
        /// <param name="FieldLength">The length of the field</param>
        /// <returns>The field</returns>
        /// <remarks></remarks>
        private string GetFixedWidthField(StringInfo Line, int Index, int FieldLength)
        {
            string Field;
            if (FieldLength > 0)
                Field = Line.String.Substring(Index, FieldLength); //Line.SubstringByTextElements(Index, FieldLength);
            else
                // Make sure the index isn't past the string
                if (Index >= Line.LengthInTextElements)
                Field = string.Empty;
            else
                Field = Line.String.Substring(Index).TrimEnd((char)13, (char)10); //.SubstringByTextElements(Index).TrimEnd((char)13, (char)10);

            if (m_TrimWhiteSpace)
                return Field.Trim();
            else
                return Field;
        }


        /// ***************************************************************
        /// ;GetEndOfLineIndex
        /// <summary>
        /// Gets the index of the first end of line character
        /// </summary>
        /// <param name="Line"></param>
        /// <returns></returns>
        /// <remarks>When there are no end of line characters, the index is the length (one past the end)</remarks>
        private int GetEndOfLineIndex(string Line)
        {
            //Debug.Assert(Line != null, "We are parsing a Nothing");

            int Length = Line.Length;
            //Debug.Assert(Length > 0, "A blank line shouldn't be parsed");

            if (Length == 1)
            {
                //Debug.Assert((Conversions.ToString(Line[0]) != Constants.vbCr) & (Conversions.ToString(Line[0]) != Constants.vbLf), "A blank line shouldn't be parsed");
                return Length;
            }

            // Check the next to last and last char for end line characters
            if (Line[Length - 2] == '\r' | Line[Length - 2] == '\n')
                return Length - 2;
            else if (Line[Length - 1] == '\r' | Line[Length - 1] == '\n')
                return Length - 1;
            else
                return Length;
        }

        /// *****************************************************************
        /// ;ValidateFixedWidthLine
        /// <summary>
        /// Indicates whether or not a line is valid
        /// </summary>
        /// <param name="Line">The line to be tested</param>
        /// <param name="LineNumber">The line number, used for exception</param>
        /// <remarks></remarks>
        private void ValidateFixedWidthLine(StringInfo Line, long LineNumber)
        {
            //Debug.Assert(Line != null, "No Line sent");

            // The only mal formed line for fixed length fields is one that's too short            
            if (Line.LengthInTextElements < m_LineLength)
            {
                m_ErrorLine = Line.String;
                m_ErrorLineNumber = m_LineNumber - (long)1;
                throw new FormatException(); // MalformedLineException(GetResourceString[ResID.MyID.TextFieldParser_MalFormedFixedWidthLine, LineNumber.ToString(CultureInfo.InvariantCulture)], LineNumber);
            }
        }

        /// ****************************************************************
        /// ;ValidateFieldWidths
        /// <summary>
        /// Determines whether or not the field widths are valid, and sets the size of a line
        /// </summary>
        /// <remarks></remarks>
        private void ValidateFieldWidths()
        {
            if (m_FieldWidths == null)
                throw new InvalidOperationException(); //GetInvalidOperationException[ResID.MyID.TextFieldParser_FieldWidthsNothing];

            if (m_FieldWidths.Length == 0)
                throw new InvalidOperationException(); //GetInvalidOperationException[ResID.MyID.TextFieldParser_FieldWidthsNothing];

            int WidthBound = m_FieldWidths.Length - 1;
            m_LineLength = 0;
            var loopTo = WidthBound - 1;

            // add all but the last element
            for (int i = 0; i <= loopTo; i++)
            {
                //Debug.Assert(m_FieldWidths[i] > 0, "Bad field width, this should have been caught on input");

                m_LineLength += m_FieldWidths[i];
            }

            // add the last field if it's greater than zero (ie not ragged).
            if (m_FieldWidths[WidthBound] > 0)
                m_LineLength += m_FieldWidths[WidthBound];
        }

        /// *****************************************************************
        /// ;ValidateFieldWidthsOnInput
        /// <summary>
        /// Checks the field widths at input.
        /// </summary>
        /// <param name="Widths"></param>
        /// <remarks>
        /// All field widths, except the last one, must be greater than zero. If the last width is
        /// less than one it indicates the last field is ragged
        /// </remarks>
        private void ValidateFieldWidthsOnInput(int[] Widths)
        {
            //Debug.Assert(Widths != null, "There are no field widths");

            int Bound = Widths.Length - 1;
            var loopTo = Bound - 1;
            for (int i = 0; i <= loopTo; i++)
            {
                if (Widths[i] < 1)
                    throw new ArgumentException("", nameof(Widths)); //GetArgumentExceptionWithArgName["FieldWidths", ResID.MyID.TextFieldParser_FieldWidthsMustPositive, "FieldWidths"];
            }
        }

        /// ***************************************************************
        /// ;ValidateAndEscapeDelimiters
        /// <summary>
        /// Validates the delimiters and creates the Regex objects for finding delimiters or quotes followed
        /// by delimiters
        /// </summary>
        /// <remarks></remarks>
        private void ValidateAndEscapeDelimiters()
        {
            if (m_Delimiters == null)
                throw new ArgumentException("", nameof(Delimiters)); //GetArgumentExceptionWithArgName["Delimiters", ResID.MyID.TextFieldParser_DelimitersNothing, "Delimiters"];

            if (m_Delimiters.Length == 0)
                throw new ArgumentException("", nameof(Delimiters)); //GetArgumentExceptionWithArgName["Delimiters", ResID.MyID.TextFieldParser_DelimitersNothing, "Delimiters"];

            int Length = m_Delimiters.Length;

            StringBuilder Builder = new StringBuilder();
            StringBuilder QuoteBuilder = new StringBuilder();

            // Add ending quote pattern. It will be followed by delimiters resulting in a string like:
            // "[ ]*(d1|d2|d3)
            QuoteBuilder.Append(EndQuotePattern + "(");
            var loopTo = Length - 1;
            for (int i = 0; i <= loopTo; i++)
            {
                if (m_Delimiters[i] != null)
                {

                    // Make sure delimiter is legal
                    if (m_HasFieldsEnclosedInQuotes)
                    {
                        if (m_Delimiters[i].IndexOf('"') > -1)
                            throw new InvalidOperationException(); //GetInvalidOperationException[ResID.MyID.TextFieldParser_IllegalDelimiter];
                    }

                    string EscapedDelimiter = Regex.Escape(m_Delimiters[i]);

                    Builder.Append(EscapedDelimiter + "|");
                    QuoteBuilder.Append(EscapedDelimiter + "|");
                }
                else
                    Console.WriteLine("Delimiter element is empty. This should have been caught on input");
            }

            m_SpaceChars = WhitespaceCharacters;

            // Get rid of trailing | and set regex
            m_DelimiterRegex = new Regex(Builder.ToString(0, Builder.Length - 1), REGEX_OPTIONS);
            Builder.Append("\r|\n");
            m_DelimiterWithEndCharsRegex = new Regex(Builder.ToString(), REGEX_OPTIONS);

            // Add end of line (either cr, ln, or nothing) and set regex
            QuoteBuilder.Append("\r|\n)|\"$");
        }


        /// *************************************************************
        /// ;ValidateReadyToRead
        /// <summary>
        /// Checks property settings to ensure we're able to read fields.
        /// </summary>
        /// <remarks>Throws if we're not able to read fields with current property settings</remarks>
        private void ValidateReadyToRead()
        {
            if (m_NeedPropertyCheck | ArrayHasChanged())
            {
                switch (m_TextFieldType)
                {
                    case FieldType.Delimited:
                        {
                            ValidateAndEscapeDelimiters();
                            break;
                        }

                    case FieldType.FixedWidth:
                        {

                            // Check FieldWidths
                            ValidateFieldWidths();
                            break;
                        }

                    default:
                        {
                            Console.WriteLine("Unknown TextFieldType");
                            break;
                        }
                }

                // Check Comment Tokens
                if (m_CommentTokens != null)
                {
                    foreach (string Token in m_CommentTokens)
                    {
                        if (!string.IsNullOrEmpty(Token))
                        {
                            if (m_HasFieldsEnclosedInQuotes & ((int)m_TextFieldType == (int)FieldType.Delimited))
                            {
                                if (string.Compare(Token.Trim(), "\"", StringComparison.Ordinal) == 0)
                                    throw new InvalidOperationException(); //GetInvalidOperationException[ResID.MyID.TextFieldParser_InvalidComment];
                            }
                        }
                    }
                }

                m_NeedPropertyCheck = false;
            }
        }

        /// *************************************************************
        /// ;ValidateDelimiters
        /// <summary>
        /// Thows if any of the delimiters contain line end characters
        /// </summary>
        /// <param name="delimiterArray">A string array of delimiters</param>
        /// <remarks></remarks>
        private void ValidateDelimiters(string[] delimiterArray)
        {
            if (delimiterArray == null)
                return;
            foreach (string delimiter in delimiterArray)
            {
                if (string.IsNullOrEmpty(delimiter))
                    throw new ArgumentException("", nameof(Delimiters)); //GetArgumentExceptionWithArgName["Delimiters", ResID.MyID.TextFieldParser_DelimiterNothing, "Delimiters"];
                if (delimiter.IndexOfAny(new char[] { (char)13, (char)10 }) > -1)
                    throw new ArgumentException("", nameof(Delimiters)); //GetArgumentExceptionWithArgName["Delimiters", ResID.MyID.TextFieldParser_EndCharsInDelimiter];
            }
        }

        /// *************************************************************
        /// ;ArrayHasChanged
        /// <summary>
        /// Determines if the FieldWidths or Delimiters arrays have changed.
        /// </summary>
        /// <remarks>If the array has changed, we need to re initialize before reading.</remarks>
        private bool ArrayHasChanged()
        {
            int lowerBound = 0;
            int upperBound = 0;

            switch (m_TextFieldType)
            {
                case FieldType.Delimited:
                    {
                        //Debug.Assert((m_DelimitersCopy == null & m_Delimiters == null) | (m_DelimitersCopy != null & m_Delimiters != null), "Delimiters and copy are not both Nothing or both not Nothing");

                        // Check null cases
                        if (m_Delimiters == null)
                            return false;

                        lowerBound = m_DelimitersCopy.GetLowerBound(0);
                        upperBound = m_DelimitersCopy.GetUpperBound(0);
                        var loopTo = upperBound;
                        for (int i = lowerBound; i <= loopTo; i++)
                        {
                            if ((m_Delimiters[i] ?? "") != (m_DelimitersCopy[i] ?? ""))
                                return true;
                        }

                        break;
                    }

                case FieldType.FixedWidth:
                    {
                        //Debug.Assert((m_FieldWidthsCopy == null & m_FieldWidths == null) | (m_FieldWidthsCopy != null & m_FieldWidths != null), "FieldWidths and copy are not both Nothing or both not Nothing");

                        // Check null cases
                        if (m_FieldWidths == null)
                            return false;

                        lowerBound = m_FieldWidthsCopy.GetLowerBound(0);
                        upperBound = m_FieldWidthsCopy.GetUpperBound(0);
                        var loopTo1 = upperBound;
                        for (int i = lowerBound; i <= loopTo1; i++)
                        {
                            if (m_FieldWidths[i] != m_FieldWidthsCopy[i])
                                return true;
                        }

                        break;
                    }

                default:
                    {
                        Console.WriteLine("Unknown TextFieldType");
                        break;
                    }
            }

            return false;
        }


        /// *************************************************************
        /// ;CheckCommentTokensForWhitespace
        /// <summary>
        /// Thows if any of the comment tokens contain whitespace
        /// </summary>
        /// <param name="tokens">A string array of comment tokens</param>
        /// <remarks></remarks>
        private void CheckCommentTokensForWhitespace(string[] tokens)
        {
            if (tokens == null)
                return;
            foreach (string token in tokens)
            {
                if (m_WhiteSpaceRegEx.IsMatch(token))
                    throw new ArgumentException("", nameof(tokens)); //GetArgumentExceptionWithArgName["CommentTokens", ResID.MyID.TextFieldParser_WhitespaceInToken];
            }
        }

        /// *************************************************************
        /// ;BeginQuotesRegex
        /// <summary>
        /// Gets the appropriate regex for finding a field beginning with quotes
        /// </summary>
        /// <value>The right regex</value>
        /// <remarks></remarks>
        private Regex BeginQuotesRegex
        {
            get
            {
                if (m_BeginQuotesRegex == null)
                {
                    // Get the pattern
                    string pattern = string.Format(CultureInfo.InvariantCulture, BEGINS_WITH_QUOTE, WhitespacePattern);
                    m_BeginQuotesRegex = new Regex(pattern, REGEX_OPTIONS);
                }

                return m_BeginQuotesRegex;
            }
        }

        /// *************************************************************
        /// ;EndQuotePattern
        /// <summary>
        /// Gets the appropriate expression for finding ending quote of a field
        /// </summary>
        /// <value>The expression</value>
        /// <remarks></remarks>
        private string EndQuotePattern
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, ENDING_QUOTE, WhitespacePattern);
            }
        }

        /// **************************************************************
        /// ;WhitepaceCharacters
        /// <summary>
        /// Returns a string containing all the characters which are whitespace for parsing purposes
        /// </summary>
        /// <value></value>
        /// <remarks></remarks>
        private string WhitespaceCharacters
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (int code in m_WhitespaceCodes)
                {
                    char spaceChar = (char)code;
                    if (!CharacterIsInDelimiter(spaceChar))
                        builder.Append(spaceChar);
                }

                return builder.ToString();
            }
        }

        /// **************************************************************
        /// ;WhitespacePattern
        /// <summary>
        /// Gets the character set of whitespaces to be used in a regex pattern
        /// </summary>
        /// <value></value>
        /// <remarks></remarks>
        private string WhitespacePattern
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (int code in m_WhitespaceCodes)
                {
                    char spaceChar = (char)code;
                    if (!CharacterIsInDelimiter(spaceChar))
                        // Gives us something like \u00A0
                        builder.Append(@"\u" + code.ToString("X4", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        /// ************************************************************************************
        /// ;CharacterIsInDelimiter
        /// <summary>
        /// Checks to see if the passed in character is in any of the delimiters
        /// </summary>
        /// <param name="testCharacter">The character to look for</param>
        /// <returns>True if the character is found in a delimiter, otherwise false</returns>
        /// <remarks></remarks>
        private bool CharacterIsInDelimiter(char testCharacter)
        {
            //Debug.Assert(m_Delimiters != null, "No delimiters set!");

            foreach (string delimiter in m_Delimiters)
            {
                if (delimiter.IndexOf(testCharacter) > -1)
                    return true;
            }

            return false;
        }

        // Indicates reader has been disposed
        private bool m_Disposed;

        // The internal StreamReader that reads the file
        private TextReader m_Reader;

        // An array holding the strings that indicate a line is a comment
        private string[] m_CommentTokens = new string[] { };

        // The line last read by either ReadLine or ReadFields
        private long m_LineNumber = (long)1;

        // Flags whether or not there is data left to read. Assume there is at creation
        private bool m_EndOfData = false;

        // Holds the last mal formed line
        private string m_ErrorLine = "";

        // Holds the line number of the last malformed line
        private long m_ErrorLineNumber = (long)-1;

        // Indicates what type of fields are in the file (fixed width or delimited)
        private FieldType m_TextFieldType = FieldType.Delimited;

        // An array of the widths of the fields in a fixed width file
        private int[] m_FieldWidths;

        // An array of the delimiters used for the fields in the file
        private string[] m_Delimiters;

        // Holds a copy of the field widths last set so we can respond to changes in the array
        private int[] m_FieldWidthsCopy;

        // Holds a copy of the field widths last set so we can respond to changes in the array
        private string[] m_DelimitersCopy;

        // Regular expression used to find delimiters
        private Regex m_DelimiterRegex;

        // Regex used with BuildField
        private Regex m_DelimiterWithEndCharsRegex;

        // Options used for regular expressions
        private const RegexOptions REGEX_OPTIONS = RegexOptions.CultureInvariant;

        // Codes for whitespace as used by String.Trim excluding line end chars as those are handled separately
        private int[] m_WhitespaceCodes = new int[] { 0x9, 0xB, 0xC, 0x20, 0x85, 0xA0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x200B, 0x2028, 0x2029, 0x3000, 0xFEFF };

        // Regualr expression used to find beginning quotes ignore spaces and tabs
        private Regex m_BeginQuotesRegex;

        // Regular expression for whitespace
        private Regex m_WhiteSpaceRegEx = new Regex(@"\s", REGEX_OPTIONS);

        // Indicates whether or not white space should be removed from a returned field
        private bool m_TrimWhiteSpace = true;

        // The position of the cursor in the buffer
        private int m_Position = 0;

        // The position of the peek cursor
        private int m_PeekPosition = 0;

        // The number of chars in the buffer
        private int m_CharsRead = 0;

        // Indicates that the user has changed properties so that we need to validate before a read
        private bool m_NeedPropertyCheck = true;

        // The default size for the buffer
        private const int DEFAULT_BUFFER_LENGTH = 4096;

        // This is a guess as to how much larger the string builder should be beyond the size of what
        // we've already read
        private const int DEFAULT_BUILDER_INCREASE = 10;

        // Buffer used to hold data read from the file. It holds data that must be read
        // ahead of the cursor (for PeekChars and EndOfData)
        private char[] m_Buffer = new char[4096];

        // The minimum length for a valid fixed width line
        private int m_LineLength;

        // Indicates whether or not we handle quotes in a csv appropriate way
        private bool m_HasFieldsEnclosedInQuotes = true;

        // A string of the chars that count as spaces (used for csv format). The norm is spaces and tabs.
        private string m_SpaceChars;

        // The largest size a line can be.
        private int m_MaxLineSize = 10000000;

        // The largest size the buffer can be
        private int m_MaxBufferSize = 10000000;

        // Regex pattern to determine if field begins with quotes
        private const string BEGINS_WITH_QUOTE = @"\G[{0}]*""";

        // Regex pattern to find a quote before a delimiter
        private const string ENDING_QUOTE = "\"[{0}]*";

        // Indicates passed in stream should be not be closed
        private bool m_LeaveOpen = false;
    }

    /// ************************************************************************
    /// ;FieldType
    /// <summary>
    /// Enum used to indicate the kind of file being read, either delimited or fixed length
    /// </summary>
    /// <remarks></remarks>
    public enum FieldType : int
    {
        // !!!!!!!!!! Changes to this enum must be reflected in ValidateFieldTypeEnumValue()
        Delimited,
        FixedWidth
    }


    /// ***********************************************************************
    /// ;QuoteDelimitedFieldBuilder
    /// <summary>
    /// Helper class that when passed a line and an index to a quote delimited field
    /// will build the field and handle escaped quotes
    /// </summary>
    /// <remarks></remarks>
    internal class QuoteDelimitedFieldBuilder
    {

        // ==PUBLIC************************************************************

        /// <summary>
        /// Creates an intance of the class and sets some properties
        /// </summary>
        /// <param name="DelimiterRegex">The regex used to find any of the delimiters</param>
        /// <param name="SpaceChars">Characters treated as space (usually space and tab)</param>
        /// <remarks></remarks>
        public QuoteDelimitedFieldBuilder(Regex DelimiterRegex, string SpaceChars)
        {
            m_DelimiterRegex = DelimiterRegex;
            m_SpaceChars = SpaceChars;
        }

        /// *******************************************************************
        /// ;FieldFinished
        /// <summary>
        /// Indicates whether or not the field has been built.
        /// </summary>
        /// <value>True if the field has been built, otherwise False</value>
        /// <remarks>If the Field has been built, the Field property will return the entire field</remarks>
        public bool FieldFinished
        {
            get
            {
                return m_FieldFinished;
            }
        }

        /// *******************************************************************
        /// ;Field
        /// <summary>
        /// The field being built
        /// </summary>
        /// <value>The field</value>
        /// <remarks></remarks>
        public string Field
        {
            get
            {
                return m_Field.ToString();
            }
        }

        /// *******************************************************************
        /// ;Index
        /// <summary>
        /// The current index on the line. Used to indicate how much of the line was used to build the field
        /// </summary>
        /// <value>The current position on the line</value>
        /// <remarks></remarks>
        public int Index
        {
            get
            {
                return m_Index;
            }
        }

        /// *******************************************************************
        /// ;DelimiterLength
        /// <summary>
        /// The length of the closing delimiter if one was found
        /// </summary>
        /// <value>The length of the delimiter</value>
        /// <remarks></remarks>
        public int DelimiterLength
        {
            get
            {
                return m_DelimiterLength;
            }
        }

        /// *******************************************************************
        /// ;MalformedLine
        /// <summary>
        /// Indicates that the current field breaks the subset of csv rules we enforce
        /// </summary>
        /// <value>True if the line is malformed, otherwise False</value>
        /// <remarks>
        /// The rules we enforce are:
        /// Embedded quotes must be escaped
        /// Only space characters can occur between a delimiter and a quote
        /// </remarks>
        public bool MalformedLine
        {
            get
            {
                return m_MalformedLine;
            }
        }

        /// *******************************************************************
        /// ;BuildField
        /// <summary>
        /// Builds a field by walking through the passed in line starting at StartAt
        /// </summary>
        /// <param name="Line">The line containing the data</param>
        /// <param name="StartAt">The index at which we start building the field</param>
        /// <remarks></remarks>
        public void BuildField(string Line, int StartAt)
        {
            m_Index = StartAt;
            int Length = Line.Length;

            while (m_Index < Length)
            {
                if (Line[m_Index] == '"')
                {

                    // Are we at the end of the file?
                    if ((m_Index + 1) == Length)
                    {
                        // We've found the end of the field
                        m_FieldFinished = true;
                        m_DelimiterLength = 1;

                        // Move index past end of line
                        m_Index += 1;
                        return;
                    }
                    // Check to see if this is an escaped quote
                    if (((m_Index + 1) < Line.Length) & (Line[m_Index + 1] == '"'))
                    {
                        m_Field.Append('"');
                        m_Index += 2;
                        continue;
                    }

                    // Find the next delimiter and make sure everything between the quote and 
                    // the delimiter is ignorable
                    int Limit = default(int);
                    Match DelimiterMatch = m_DelimiterRegex.Match(Line, m_Index + 1);
                    if (!DelimiterMatch.Success)
                        Limit = Length - 1;
                    else
                        Limit = DelimiterMatch.Index - 1;
                    var loopTo = Limit;
                    for (int i = m_Index + 1; i <= loopTo; i++)
                    {
                        if (m_SpaceChars.IndexOf(Line[i]) < 0)
                        {
                            m_MalformedLine = true;
                            return;
                        }
                    }

                    // The length of the delimiter is the length of the closing quote (1) + any spaces + the length
                    // of the delimiter we matched if any
                    m_DelimiterLength = (1 + Limit) - m_Index;
                    if (DelimiterMatch.Success)
                        m_DelimiterLength += DelimiterMatch.Length;

                    m_FieldFinished = true;
                    return;
                }
                else
                {
                    m_Field.Append(Line[m_Index]);
                    m_Index += 1;
                }
            }
        }

        // String builder holding the field
        private StringBuilder m_Field = new StringBuilder();

        // Indicates m_Field contains the entire field
        private bool m_FieldFinished;

        // The current index on the field
        private int m_Index;

        // The length of the closing delimiter if one is found
        private int m_DelimiterLength;

        // The regular expression used to find the next delimiter
        private Regex m_DelimiterRegex;

        // Chars that should be counted as space (and hence ignored if occurring before or after a delimiter
        private string m_SpaceChars;

        // Indicates the line breaks the csv rules we enforce
        private bool m_MalformedLine;
    }
}
