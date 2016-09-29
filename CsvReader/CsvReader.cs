using System;
using System.Collections.Generic;
using System.IO;

namespace CsvReader
{
    /// <summary>
    /// A CSV reader.  There are so many CSV readers in the world and none of them seem to work right.
    /// Many try to do too much making them too specialized and therefore not useful in general 
    /// contexts.  Some are overly complicated to use.  Some run slow.  As an example, Microsoft's 
    /// TextFieldParser is pretty good, but has some fatal flaws.  It ignores blank lines in a quoted
    /// value.  And, it strips whitespace from quoted values.
    /// 
    /// This implementation is intended to be fast and correct.  Of course, since there is no 
    /// standard for CSV, correct is somewhat subjective.  This is written to conform to the 
    /// obvious aspects of CSV and supports switches to control some of the more contentious 
    /// behaviors ... such as:
    /// 
    /// Custom Delimiters [TODO]
    /// Why would someone want to use a delimiter other than comma?  Well, tab is somewhat common.
    /// And, the implementation is easy and the performance impact is none.  So why not?
    /// 
    /// Comment Lines [TODO]
    /// There is no mention of comment lines in the psuedo-official documents about CSV. But there's
    /// plenty of talk about people using comment lines.  So, it seems the people want
    /// this feature.  So, why not?
    /// 
    /// Trim Whitespace [TODO]
    /// RFC4180 says that whitespace should not be trimmed.  But, consider the quoted value.  Does
    /// that mean the whitespace before and after the quotes should be included?  That's nonesense!
    /// So, RFC4180 is clearly wrong/incomplete WRT quoted values.  But, should the whitespace be
    /// trimmed for unquoted values?  Hmm.  Who knows?  Let's make it optional.  I'm picking trimmed
    /// as default since it makes sense to me. Sorry RFC4180.
    /// 
    /// Ideas:
    ///     line and column info in exception ... because it's useful
    /// </summary>
    /// <remarks>
    /// Some documents that describe the CSV format:
    /// https://en.wikipedia.org/wiki/Comma-separated_values
    /// https://tools.ietf.org/html/rfc4180
    /// http://www.computerhope.com/issues/ch001356.htm
    /// </remarks>
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

            public bool EndOfLine => LinePos >= _line.Length;

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

            public string ContextMessage(string message, int? linePos=null)
            {
                linePos = linePos ?? LinePos;
                return $"{message} at line {LineNumber} column {linePos}.";
            }
        }

        private readonly Buffer _buffer;
        private readonly HashSet<char> _delimiters = new HashSet<char> { ',' };

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
            buffer.ConsumeWhile(c => !_delimiters.Contains(c));
            string value = buffer.SubstringConsumed(startPos);
            value = value.TrimEnd();
            int quotePos = value.IndexOf('"');
            if (quotePos != -1)
                throw new QuoteInUnquotedValueException(
                    buffer.ContextMessage("Unquoted value contains quote", quotePos));
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
                    {
                        buffer.ConsumeChar(); // escaped '"'
                    }
                    else
                    {
                        ConsumeWhitespace(buffer);
                        if (!buffer.EndOfLine && !_delimiters.Contains(buffer.Char))
                            throw new TextAfterQuotedValueException(
                                buffer.ContextMessage("Text after quoted value"));
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
