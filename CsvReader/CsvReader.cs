using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvReader
{
    public sealed class CsvReader
    {
        private readonly TextReader _textReader;

        public CsvReader(TextReader textReader)
        {
            _textReader = textReader;
        }

        public static CsvReader Parse(string text)
        {
            return new CsvReader(new StringReader(text));
        }

        /// <summary>
        /// Returns the next record of values or null if the end of the stream has been reached.
        /// </summary>
        public IEnumerable<string> ReadRecord()
        {
            string text = _textReader.ReadLine();
            return text?.Split(',');
        }
    }
}
