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

            internal void ConsumeWhile(Func<char, bool> condition)
            {
                while (!EndOfLine && condition(Char))
                    ConsumeChar();
            }

            internal string SubstringConsumed(int pos)
            {
                return _line.Substring(pos, LinePos - pos);
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

        private string PositionContext(string message, int pos)
        {
            return $"{message} at position {pos}.";
        }

        private void ConsumeWhitespace(Buffer buffer)
        {
            while (char.IsWhiteSpace(buffer.Char))
                buffer.ConsumeChar();
        }

        private string ConsumeValue(Buffer buffer)
        {
            ConsumeWhitespace(buffer);
            //if (pos >= text.Length)
            //return "";
            var startPos = buffer.LinePos;

            if (buffer.Char == '"')
            {
                buffer.ConsumeChar();
                bool done = false;
                while (!done)
                {
                    buffer.ConsumeWhile(c => c != '"');
                    if (buffer.EndOfLine)
                    {
                        throw new Exception("need next line!");
                    }
                    else
                    {
                        buffer.ConsumeChar();
                        if (!buffer.EndOfLine && buffer.Char == '"')
                            buffer.ConsumeChar(); // jump over doubled quote; and continue reading string
                        else
                        {
                            while (!buffer.EndOfLine)
                                if (!char.IsWhiteSpace(buffer.Char))
                                    throw new TextAfterQuotedValueException(PositionContext("Text after quoted value", buffer.LinePos));
                            done = true;
                        }
                    }
                }
                var value = buffer.SubstringConsumed(startPos);
                value = value.Substring(1, value.Length - 2);
                value = value.Replace(@"""""", @"""");
                return value;
            }
            else
            {
                buffer.ConsumeWhile(c => c != ',');
                string value = buffer.SubstringConsumed(startPos);
                if (!buffer.EndOfLine)
                    buffer.ConsumeChar(); // ','
                value = value.TrimEnd();
                int quotePos = value.IndexOf('"');
                if (quotePos != -1)
                    throw new QuoteInUnquotedValueException(PositionContext("Unquoted value contains quote", quotePos));
                return value;
            }
        }

        /// <summary>
        /// Returns the next record of values or null if the end of the stream has been reached.
        /// </summary>
        public IEnumerable<string> ReadRecord()
        {
            _buffer.NextLine();
            while (!_buffer.EndOfData && _buffer.EndOfLine)
                _buffer.NextLine();
            if (_buffer.EndOfData)
                return null;
            var values = new List<string>();
            while (!_buffer.EndOfLine)
            {
                values.Add(ConsumeValue(_buffer));
            }
            return values;
        }
    }
}
