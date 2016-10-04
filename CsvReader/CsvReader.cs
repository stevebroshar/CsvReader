using System;
using System.Collections.Generic;
using System.IO;

namespace Scb
{
    /// <summary>
    /// A CSV reader.  There are so many CSV readers in the world and none of them seem to work right.
    /// Many try to do too much making them too specialized and therefore not useful in general 
    /// contexts.  Some are overly complicated to use.  Some run slow.  As an example, Microsoft's 
    /// TextFieldParser is pretty good, but has some fatal flaws.  It ignores blank lines in a quoted
    /// value.  And, it strips whitespace from quoted values.  And, it's relatively slow.  Other than
    /// that it's great ;)
    /// 
    /// This implementation is intended to be fast and correct.  Of course, since there is no 
    /// standard for CSV, correct is somewhat subjective.  This is written to conform to the 
    /// obvious aspects of CSV and supports switches to control some of the more contentious 
    /// behaviors ... such as:
    /// 
    /// Custom Delimiters
    /// Why would someone want to use a delimiter other than comma ... for something called "COMMA
    /// separated values"?  Well, tab is somewhat common.  And, the implementation is easy and the 
    /// performance impact is none.  So why not?  See SetDelimiters().
    /// 
    /// Comment Lines
    /// There is no mention of comment lines in the psuedo-official documents about CSV. But there's
    /// plenty of talk about people using comment lines.  So, it seems the people want this feature.
    /// So, why not?  I was able to add it with very minimal performance impact.  See 
    /// SetCommentChars().
    /// 
    /// Trim Whitespace [TODO: implemented this]
    /// RFC4180 says that whitespace should not be trimmed.  But, consider the quoted value.  Does
    /// that mean the whitespace before and after the quotes should be included?  That's nonesense!
    /// I'd say RFC4180 is incomplete if not wrong WRT quoted values.  But, should the whitespace be
    /// trimmed for unquoted values?  Hmm.  Who knows?  Let's make it optional.  I'm picking trimmed
    /// as default since it makes sense to me. Sorry RFC4180.
    /// 
    /// Quote in Unquoted Value
    /// RFC4180 says that an unquoted value should NOT contain a quote so I added checking -- 
    /// propagating an exception if found.  But, I wonder whether consumers might sometimes like to
    /// relax that rule.  It's easy to not treat a quote as special when the value is not 
    /// enclosed in a quote -- starts with a quote -- just don't check for a quote char.  But, I 
    /// think the issue is robustness.  If the consumer is expecting quotes to not be special for 
    /// an unquoted value, but has a value with a quote at the beginning, then there is ambiguity.
    /// The consumer might think the value should be read with the quote, but since quoted value 
    /// is so core to CSV, the value would be parsed as quoted.  hmm.  Maybe there should be an 
    /// option to disable quoted value parsing.  Hey maybe that's what 
    /// TextFieldParser.HasFieldsEnclosedInQuotes is about.  Maybe this should have a similar 
    /// switch.  I'd call it something better like SupportQuotedValues -- defaulting to true ... 
    /// since it's normal CSV behavior.
    /// </summary>
    /// <remarks>
    /// Some documents that describe the CSV format:
    /// https://en.wikipedia.org/wiki/Comma-separated_values
    /// https://tools.ietf.org/html/rfc4180
    /// http://www.computerhope.com/issues/ch001356.htm
    /// </remarks>
    public sealed class CsvReader
    {
        #region Helper Classes

        internal sealed class QuoteInUnquotedValueException : CsvException
        {
            public QuoteInUnquotedValueException(Context context) : 
                base("Unquoted value contains quote", context) { }
        }

        internal sealed class TextAfterQuotedValueException : CsvException
        {
            public TextAfterQuotedValueException(Context context) : 
                base("Text after quoted value", context) { }
        }

        internal sealed class QuoteStartWithoutEndException : CsvException
        {
            public QuoteStartWithoutEndException(Context context) :
                base("Unmatched quote at start of value", context) { }
        }

        internal sealed class Buffer
        {
            private readonly TextReader _textReader;
            private string _line;

            public Buffer(TextReader textReader)
            {
                _textReader = textReader;
            }

            /// <summary>
            /// Read the next line.
            /// </summary>
            public void NextLine()
            {
                LinePos = 0;
                _line = _textReader.ReadLine();
                if (_line == null)
                    EndOfData = true;
                else
                    ++LineNumber;
            }

            /// <summary>
            /// Current line index.
            /// </summary>
            public int LineNumber { get; private set; } = -1;

            /// <summary>
            /// Current column of the current line.
            /// </summary>
            public int LinePos { get; private set; }

            /// <summary>
            /// Whether LinePos is past the end of the current line.
            /// </summary>
            public bool EndOfLine => _line == null || LinePos >= _line.Length;

            /// <summary>
            /// Whether the last line has been read.
            /// </summary>
            public bool EndOfData { get; private set; }

            /// <summary>
            /// Returns the char at LinePos.
            /// </summary>
            public char Char => _line[LinePos];

            /// <summary>
            /// Increments LinePos by one if not at end-of-line.
            /// </summary>
            public void ConsumeChar() { if (!EndOfLine) ++LinePos; }

            /// <summary>
            /// Increments LinePos until until the condition (which is passed the current char) 
            /// is false or end-of-line.
            /// </summary>
            /// <remarks>
            /// This could be simplified by using ConsumeChar(), but that would add extra checking.
            /// </remarks>
            public void ConsumeWhile(Func<char, bool> condition)
            {
                if (_line != null)
                    while (LinePos < _line.Length && condition(Char))
                        ++LinePos;
            }

            /// <summary>
            /// Returns the substring of the current line from pos up to but not including the char
            /// as LinePos.
            /// </summary>
            public string SubstringConsumed(int pos)
            {
                return _line.Substring(pos, LinePos - pos);
            }

            public CsvException.Context Context(int? column = null)
            {
                return new CsvException.Context(LineNumber, column ?? LinePos);
            }
        }

        #endregion

        private const char Quote = '"';
        private readonly Buffer _buffer;
        private HashSet<char> _delimiters = new HashSet<char> { ',' };
        private HashSet<char> _commentChars;

        private static void ConsumeWhitespace(Buffer buffer)
        {
            buffer.ConsumeWhile(char.IsWhiteSpace);
        }

        private string ConsumeUnquotedValue(Buffer buffer)
        {
            var startPos = buffer.LinePos;
            buffer.ConsumeWhile(c => !_delimiters.Contains(c));
            string value = buffer.SubstringConsumed(startPos);
            value = value.TrimEnd();
            int quotePos = value.IndexOf(Quote);
            if (quotePos != -1)
                throw new QuoteInUnquotedValueException(buffer.Context(quotePos));
            return value;
        }

        private string ConsumeQuotedValue(Buffer buffer)
        {
            var startContext = new CsvException.Context(buffer.LineNumber, buffer.LinePos);
            var startPos = buffer.LinePos;
            buffer.ConsumeChar(); // start quote
            bool done = false;
            string value = "";
            while (!done)
            {
                buffer.ConsumeWhile(c => c != Quote);
                if (buffer.EndOfLine)
                {
                    // value spans lines
                    value += buffer.SubstringConsumed(startPos) + Environment.NewLine;
                    buffer.NextLine();
                    if (buffer.EndOfData)
                        throw new QuoteStartWithoutEndException(startContext);
                    startPos = 0;
                }
                else
                {
                    buffer.ConsumeChar(); // end or escape quote
                    if (!buffer.EndOfLine && buffer.Char == Quote)
                    {
                        buffer.ConsumeChar(); // escaped quote
                    }
                    else
                    {
                        value += buffer.SubstringConsumed(startPos); // last part thru end quote

                        // check for text between end quote and delimiter
                        ConsumeWhitespace(buffer);
                        if (!buffer.EndOfLine && !_delimiters.Contains(buffer.Char))
                            throw new TextAfterQuotedValueException(buffer.Context());

                        done = true;
                    }
                }
            }
            value = value.Substring(1, value.Length - 2); // remove enclosing quotes
            value = value.Replace(@"""""", @""""); // replace doubled quotes with single
            return value;
        }

        private string ConsumeValue(Buffer buffer)
        {
            ConsumeWhitespace(buffer);
            if (buffer.EndOfLine)
                return "";
            if (buffer.Char == Quote)
                return ConsumeQuotedValue(buffer);
            return ConsumeUnquotedValue(buffer);
        }

        /// <summary>
        /// Sets the chars that delimit values.  Default is [','].
        /// </summary>
        public void SetDelimiters(params char[] delimiters)
        {
            _delimiters = new HashSet<char>(delimiters);
        }

        /// <summary>
        /// Sets the chars that specify a comment line -- when in the first column.
        /// Default is none -- no comment lines.
        /// </summary>
        public void SetCommentChars(params char[] chars)
        {
            _commentChars = new HashSet<char>(chars);
        }

        /// <summary>
        /// Initialize for a TextReader.
        /// </summary>
        public CsvReader(TextReader textReader)
        {
            _buffer = new Buffer(textReader);
        }

        /// <summary>
        /// Initialize for a string.
        /// </summary>
        public static CsvReader Parse(string text)
        {
            return new CsvReader(new StringReader(text));
        }

        /// <summary>
        /// Returns the next record of values or null if the end of the data has been reached.
        /// </summary>
        public IEnumerable<string> ReadRecord()
        {
            _buffer.NextLine();
            if (_buffer.EndOfData)
                return null;

            // consume whitespace lines
            if (_commentChars != null)
                if (!_buffer.EndOfLine & _commentChars.Contains(_buffer.Char))
                    _buffer.ConsumeWhile(c=> true);
            ConsumeWhitespace(_buffer);
            while (!_buffer.EndOfData && _buffer.EndOfLine)
            {
                _buffer.NextLine();
                if (_commentChars != null)
                    if (!_buffer.EndOfLine && _commentChars.Contains(_buffer.Char))
                        _buffer.ConsumeWhile(c => true);
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

    public abstract class CsvException : Exception
    {
        internal sealed class Context
        {
            public Context(int line, int column)
            {
                Line = line;
                Column = column;
            }

            public int Line { get; }
            public int Column { get; }
            public string Message(string message)
            {
                return $"{message} at line {Line + 1} column {Column + 1}.";
            }
        }

        internal CsvException(string message, Context context) :
            base(context.Message(message))
        {
            Line = context.Line;
            Column = context.Column;
        }
        public int Line { get; }
        public int Column { get; }
    }
}
