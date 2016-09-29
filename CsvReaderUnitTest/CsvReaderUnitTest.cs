using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CsvReaderUnitTest
{
    [TestClass]
    public class CsvReaderUnitTest
    {
        #region Values

        [TestMethod]
        public void ReadRecord_ReturnsValuesDelimitedByComma()
        {
            var values = CsvReader.CsvReader.Parse("a,b,c").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_ReturnsEmptyMiddleValue()
        {
            var values = CsvReader.CsvReader.Parse(",a").ReadRecord();
            CollectionAssert.AreEqual(new[] { "", "a" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_ReturnsEmptyEndValue()
        {
            var values = CsvReader.CsvReader.Parse("a,").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a", "" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_PreservesWhitespaceWithinValues()
        {
            var values = CsvReader.CsvReader.Parse("a b\tc").ReadRecord();
            Assert.AreEqual("a b\tc", values.First());
        }

        [TestMethod]
        public void ReadRecord_IgnoresWhitespaceAroundValues()
        {
            var values = CsvReader.CsvReader.Parse(" a ,\tb\t").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a", "b" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_IncludesCommaInQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse(@"""a,b""").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a,b" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_IncludesNewLineInQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse($@"""a{Environment.NewLine}b{Environment.NewLine}c""").ReadRecord();
            Assert.AreEqual($"a{Environment.NewLine}b{Environment.NewLine}c", values.First());
        }

        /// <summary>
        /// Ensure that even though ignore whitespace lines between records, 
        /// do not ignore them within a quoted value.
        /// </summary>
        [TestMethod]
        public void ReadRecord_IncludesEmptyLineInQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse($@"""a{Environment.NewLine}{Environment.NewLine}b""").ReadRecord();
            Assert.AreEqual($"a{Environment.NewLine}{Environment.NewLine}b", values.First());
        }

        [TestMethod]
        public void ReadRecord_TreatsDoubledQuotesAsSingleInsideQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse(@"""""""a""""b""""""").ReadRecord();
            Assert.AreEqual(@"""a""b""", values.First());
        }

        [TestMethod]
        public void ReadRecord_Propagates_ForTextAfterQuotedValue()
        {
            ExceptionAssert.Propagates<CsvReader.CsvReader.TextAfterQuotedValueException>(
                () => CsvReader.CsvReader.Parse(@"""a""x").ReadRecord());
        }

        [TestMethod]
        public void ReadRecord_PropagatesForUnquotedValueWithQuote()
        {
            ExceptionAssert.Propagates<CsvReader.CsvReader.QuoteInUnquotedValueException>(
                () => CsvReader.CsvReader.Parse(@"a""").ReadRecord());
        }

        //[TestMethod]
        //public void ReadRecord_TreatsQuoteAsRegularTextIfValueStartsWithNonQuote()
        //{
        //    var values = CsvReader.CsvReader.Parse(@"x""a,b""""").ReadRecord();
        //    CollectionAssert.AreEqual(new[] { @"x""a", @"b""""" }, values.ToArray());
        //}

        #endregion

        #region Records & Lines

        [TestMethod]
        public void ReadRecord_ReturnsAllValuesOfRecord_WhenDiffersFromRecordToRecord()
        {
            var text = $"a,b,c{Environment.NewLine}d{Environment.NewLine}e,f";
            var reader = CsvReader.CsvReader.Parse(text);
            Assert.AreEqual(3, reader.ReadRecord().Count());
            Assert.AreEqual(1, reader.ReadRecord().Count());
            Assert.AreEqual(2, reader.ReadRecord().Count());
        }

        [TestMethod]
        public void ReadRecord_ReturnsNullAfterLastLine()
        {
            var reader = CsvReader.CsvReader.Parse("a,b,c");
            reader.ReadRecord();
            Assert.IsNull(reader.ReadRecord());
        }

        [TestMethod]
        public void ReadRecord_IgnoresEmptyLastLine()
        {
            var reader = CsvReader.CsvReader.Parse($"a{Environment.NewLine}");
            Assert.AreEqual("a", reader.ReadRecord().First());
            Assert.IsNull(reader.ReadRecord());
        }

        [TestMethod]
        public void ReadRecord_IgnoresEmptyMidLine()
        {
            var reader = CsvReader.CsvReader.Parse($"a{Environment.NewLine}{Environment.NewLine}b");
            Assert.AreEqual("a", reader.ReadRecord().First());
            Assert.AreEqual("b", reader.ReadRecord().First());
        }

        [TestMethod]
        public void ReadRecord_IgnoresWhitespaceLine()
        {
            var reader = CsvReader.CsvReader.Parse($"a{Environment.NewLine} \t{Environment.NewLine}b");
            Assert.AreEqual("a", reader.ReadRecord().First());
            Assert.AreEqual("b", reader.ReadRecord().First());
        }

        [TestMethod]
        public void ReadRecord_IgnoresMultipleWhitespaceLines()
        {
            var reader = CsvReader.CsvReader.Parse($"a{Environment.NewLine} \t{Environment.NewLine} \t{Environment.NewLine}b");
            Assert.AreEqual("a", reader.ReadRecord().First());
            Assert.AreEqual("b", reader.ReadRecord().First());
        }

        #endregion
    }
}
