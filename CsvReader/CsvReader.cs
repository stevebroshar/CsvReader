using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// A CSV parser.
/// </summary>
/// <remarks>
/// Some documents that describe the CSV format.  No document is definintive as there is no standard.
/// https://en.wikipedia.org/wiki/Comma-separated_values
/// https://tools.ietf.org/html/rfc4180
/// http://www.computerhope.com/issues/ch001356.htm
/// </remarks>
namespace CsvReader
{
    public sealed class CsvReader
    {
        internal abstract class CsvException : Exception
        {
            public CsvException(string message) : base(message) { }
        }

        internal sealed class QuoteInUnquotedValueException : CsvException
        {
            public QuoteInUnquotedValueException(string message) : base(message) { }
        }

        internal sealed class TextAfterQuotedValueException : CsvException
        {
            public TextAfterQuotedValueException(string message) : base(message) { }
        }

        internal sealed class Buffer
        {
            private readonly TextReader _textReader;
            private string _line;

            public Buffer(TextReader textReader)
            {
                _textReader = textReader;
            }

            public void NextLine()
            {
                LinePos = 0;
                _line = _textReader.ReadLine();
                if (_line == null)
                    EndOfData = true;
                else
                    ++LineNumber;
            }

            public int LineNumber { get; private set; }

            public int LinePos { get; private set; }

            public bool EndOfLine => LinePos >= _line.Length;//_line != null &&

            public bool EndOfData { get; private set; }

            public char Char => _line[LinePos];

            public void ConsumeChar() { ++LinePos; }

            public void ConsumeWhile(Func<char, bool> condition)
            {
                while (!EndOfLine && condition(Char))
                    ConsumeChar();
            }

            public string SubstringConsumed(int pos)
            {
                return _line.Substring(pos, LinePos - pos);
            }

            public string PositionMessage(string message, int? pos=null)
            {
                pos = pos ?? LinePos;
                return $"{message} at position {pos}.";
            }
        }

        private readonly Buffer _buffer;

        public CsvReader(TextReader textReader)
        {
            _buffer = new Buffer(textReader);
        }

        public static CsvReader Parse(string text)
        {
            return new CsvReader(new StringReader(text));
        }

        private void ConsumeWhitespace(Buffer buffer)
        {
            buffer.ConsumeWhile(c => char.IsWhiteSpace(c));
        }

        private string ConsumeUnquotedValue(Buffer buffer)
        {
            var startPos = buffer.LinePos;
            buffer.ConsumeWhile(c => c != ',');
            string value = buffer.SubstringConsumed(startPos);
            //if (!buffer.EndOfLine)
            //    buffer.ConsumeChar(); // ','
            value = value.TrimEnd();
            int quotePos = value.IndexOf('"');
            if (quotePos != -1)
                throw new QuoteInUnquotedValueException(buffer.PositionMessage("Unquoted value contains quote", quotePos));
            return value;
        }

        private string ConsumeQuotedValue(Buffer buffer)
        {
            var startPos = buffer.LinePos;
            buffer.ConsumeChar(); // start '"'
            bool done = false;
            string value = "";
            while (!done)
            {
                buffer.ConsumeWhile(c => c != '"');
                if (buffer.EndOfLine)
                {
                    value += buffer.SubstringConsumed(startPos) + Environment.NewLine;
                    buffer.NextLine();
                    startPos = 0;
                }
                else
                {
                    buffer.ConsumeChar(); // end or escape '"'
                    if (!buffer.EndOfLine && buffer.Char == '"')
                        buffer.ConsumeChar(); // escaped '"'
                    else
                    {
                        while (!buffer.EndOfLine)
                            if (!char.IsWhiteSpace(buffer.Char))
                                throw new TextAfterQuotedValueException(buffer.PositionMessage("Text after quoted value"));
                        done = true;
                    }
                }
            }
            value += buffer.SubstringConsumed(startPos);
            value = value.Substring(1, value.Length - 2);
            value = value.Replace(@"""""", @"""");
            return value;
        }

        private string ConsumeValue(Buffer buffer)
        {
            ConsumeWhitespace(buffer);
            if (buffer.EndOfLine)
                return "";
            if (buffer.Char == '"')
                return ConsumeQuotedValue(buffer);
            else
                return ConsumeUnquotedValue(buffer);
        }

        /// <summary>
        /// Returns the next record of values or null if the end of the stream has been reached.
        /// </summary>
        public IEnumerable<string> ReadRecord()
        {
            _buffer.NextLine();
            if (_buffer.EndOfData)
                return null;

            // consume whitespace lines
            ConsumeWhitespace(_buffer);
            while (!_buffer.EndOfData && _buffer.EndOfLine)
            {
                _buffer.NextLine();
                ConsumeWhitespace(_buffer);
            }
            if (_buffer.EndOfData)
                return null;

            var values = new List<string>();
            while (true)
            {
                values.Add(ConsumeValue(_buffer));
                if (_buffer.EndOfLine)
                    return values;
                _buffer.ConsumeChar();
            }
        }
    }
}
