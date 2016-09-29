using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CsvReaderUnitTest
{
    [TestClass]
    public class CsvReaderUnitTest
    {
        #region Unquoted Values

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
        public void ReadRecord_PreservesWhitespaceWithinValue()
        {
            var values = CsvReader.CsvReader.Parse("a b\tc").ReadRecord();
            Assert.AreEqual("a b\tc", values.First());
        }

        /// <summary>
        /// This might be controversial behavior since some CSV descriptions say whitespace 
        /// should not be ignored.  Maybe there should be an option to control this behavior.
        /// </summary>
        [TestMethod]
        public void ReadRecord_IgnoresWhitespaceAroundValue()
        {
            var values = CsvReader.CsvReader.Parse(" a ,\tb\t").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a", "b" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_Propagates_ForQuoteInUnquotedValue()
        {
            ExceptionAssert.Propagates<CsvReader.CsvReader.QuoteInUnquotedValueException>(
                () => CsvReader.CsvReader.Parse(@"a""").ReadRecord());
        }

        // NOTE: This test is wrong if one must obey the rule that an unquoted value cannot contain a quote.
        //[TestMethod]
        //public void ReadRecord_TreatsQuoteAsRegularTextIfValueStartsWithNonQuote()
        //{
        //    var values = CsvReader.CsvReader.Parse(@"x""a,b""""").ReadRecord();
        //    CollectionAssert.AreEqual(new[] { @"x""a", @"b""""" }, values.ToArray());
        //}

        #endregion

        #region Quoted Values

        [TestMethod]
        public void ReadRecord_ReturnsQuotedValuesDelimitedByComma()
        {
            var values = CsvReader.CsvReader.Parse(@"""a"",""b"",""c""").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_ReturnsQuotedAndUnquotedValues()
        {
            var values = CsvReader.CsvReader.Parse(@"a,""b"",c,""d""").ReadRecord();
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, values.ToArray());
        }

        [TestMethod]
        public void ReadRecord_IncludesAllWhitespaceInQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse(@""" a b """).ReadRecord();
            Assert.AreEqual(" a b ", values.First());
        }

        /// <summary>
        /// This might be controversial behavior since some CSV descriptions say whitespace 
        /// should not be ignored.  For an unquoted value I think it could go either way.  
        /// But, for a quoted value it makes no sense to include anything outside of the quotes.
        /// </summary>
        [TestMethod]
        public void ReadRecord_ExcludesWhitespaceOutsideOfQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse(" \"ab\"\t").ReadRecord();
            Assert.AreEqual("ab", values.First());
        }

        [TestMethod]
        public void ReadRecord_IncludesCommaInQuotedValue()
        {
            var values = CsvReader.CsvReader.Parse(@"""a,b""").ReadRecord();
            Assert.AreEqual("a,b", values.First());
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
        public void ReadRecord_Propagates_ForStartQuoteWitNoMatchingEnd_InLastLine()
        {
            ExceptionAssert.Propagates<CsvReader.CsvReader.QuoteStartWithoutEndException>(
                () => CsvReader.CsvReader.Parse(@"""a").ReadRecord());
        }

        [TestMethod]
        public void ReadRecord_Propagates_ForStartQuoteWitNoMatchingEnd_InNonLastLine()
        {
            ExceptionAssert.Propagates<CsvReader.CsvReader.QuoteStartWithoutEndException>(
                () => CsvReader.CsvReader.Parse($@"""a{Environment.NewLine}b").ReadRecord());
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
                () => CsvReader.CsvReader.Parse(@"""a""x,b").ReadRecord());
            ExceptionAssert.Propagates<CsvReader.CsvReader.TextAfterQuotedValueException>(
                () => CsvReader.CsvReader.Parse(@"""a"" x,b").ReadRecord());
            ExceptionAssert.Propagates<CsvReader.CsvReader.TextAfterQuotedValueException>(
                () => CsvReader.CsvReader.Parse(@"""a""x").ReadRecord());
            ExceptionAssert.Propagates<CsvReader.CsvReader.TextAfterQuotedValueException>(
                () => CsvReader.CsvReader.Parse(@"""a"" x").ReadRecord());
        }

        #endregion

        #region Records & Lines

        /// <summary>
        /// Some CSV docs say that each record should have the same number of values.
        /// But they don't say what to do if they don't.  In practice, I've found that
        /// it's best to be forgiving so that the caller has the fexability can deal with it.
        /// </summary>
        [TestMethod]
        public void ReadRecord_ReturnsAllValuesOfRecord_WhenCountDiffersFromRecordToRecord()
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

        #region Custom Delimiters

        [TestMethod]
        public void ReadRecord_ReturnsValuesDelimitedByColonOrSemicolon()
        {
            var reader = CsvReader.CsvReader.Parse("a:b;c");
            reader.SetDelimiters(':', ';');
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, reader.ReadRecord().ToArray());
        }

        #endregion

        #region Comment Lines

        [TestMethod]
        public void ReadRecord_IgnoresLineStartingWithCommentChar()
        {
            var reader = CsvReader.CsvReader.Parse("#comment");
            reader.SetCommentChars('#', '$');
            Assert.IsNull(reader.ReadRecord());
        }

        [TestMethod]
        public void ReadRecord_IgnoresLinesStartingWithCommentChar()
        {
            var reader = CsvReader.CsvReader.Parse($"a{Environment.NewLine}#comment{Environment.NewLine}b{Environment.NewLine}$comment{Environment.NewLine}c");
            reader.SetCommentChars('#', '$');
            Assert.AreEqual("a", reader.ReadRecord().First());
            Assert.AreEqual("b", reader.ReadRecord().First());
            Assert.AreEqual("c", reader.ReadRecord().First());
        }

        #endregion

        [TestClass]
        public class BufferUnitTest
        {
            [TestMethod]
            public void LineNumber_IsMinusOneBeforeFirstRead()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader(""));
                Assert.AreEqual(-1, buffer.LineNumber);
            }

            [TestMethod]
            public void NextLine_IncrementsLineNumber()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader($"a{Environment.NewLine}b{Environment.NewLine}c"));
                buffer.NextLine();
                Assert.AreEqual(0, buffer.LineNumber);
                buffer.NextLine();
                Assert.AreEqual(1, buffer.LineNumber);
            }

            [TestMethod]
            public void NextLine_SetsLinePosToZero()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader($"a{Environment.NewLine}b{Environment.NewLine}c"));
                buffer.NextLine();
                buffer.ConsumeChar();
                Assert.IsTrue(buffer.LinePos != 0);
                buffer.NextLine();
                Assert.AreEqual(0, buffer.LinePos);
            }

            [TestMethod]
            public void Char_ReturnsCharAtLinePos()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("abc"));
                buffer.NextLine();
                Assert.AreEqual('a', buffer.Char);
                buffer.ConsumeChar();
                Assert.AreEqual('b', buffer.Char);
            }

            /// Maybe Char should not propagate when EOL -- return null instead?  Or maybe let
            /// it propagate, but change to a method since a property getter is not supposed to 
            /// propagate.
            [TestMethod]
            public void Char_Propagates_AtEndOfLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader(""));
                buffer.NextLine();
                Assert.IsTrue(buffer.EndOfLine);
                ExceptionAssert.Propagates<NullReferenceException>(() =>
                {
                    var bufferChar = buffer.Char;
                });
            }

            [TestMethod]
            public void ConsumeChar_IncrementsLinePos()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("abc"));
                buffer.NextLine();
                var origPos = buffer.LinePos;
                buffer.ConsumeChar();
                Assert.AreEqual(origPos + 1, buffer.LinePos);
            }

            [TestMethod]
            public void ConsumeChar_DoesNotIncrementLinePosWhenAtEndOfLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader(""));
                buffer.NextLine();
                Assert.IsTrue(buffer.EndOfLine);
                var origPos = buffer.LinePos;
                buffer.ConsumeChar();
                Assert.AreEqual(origPos, buffer.LinePos);
            }

            [TestMethod]
            public void EndOfData_IsFalseBeforeFirstRead()
            {
                Assert.IsFalse(new CsvReader.CsvReader.Buffer(new StringReader("")).EndOfData);
            }

            [TestMethod]
            public void EndOfData_IsTrueAfterReadLastLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader(""));
                buffer.NextLine();
                Assert.IsTrue(buffer.EndOfData);
            }

            [TestMethod]
            public void EndOfLine_IsTrueBeforeFirstRead()
            {
                Assert.IsTrue(new CsvReader.CsvReader.Buffer(new StringReader("")).EndOfLine);
            }

            [TestMethod]
            public void EndOfLine_IsTrueAfterReadEmptyLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader(""));
                buffer.NextLine();
                Assert.IsTrue(buffer.EndOfLine);
            }

            [TestMethod]
            public void EndOfLine_IsFalseAfterReadNonEmptyLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("x"));
                buffer.NextLine();
                Assert.IsFalse(buffer.EndOfLine);
            }

            [TestMethod]
            public void EndOfLine_IsFalseAfterConsumeLastCharOfLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("x"));
                buffer.NextLine();
                buffer.ConsumeChar();
                Assert.IsTrue(buffer.EndOfLine);
            }

            [TestMethod]
            public void ConsumeWhile_DoesNotAdvancePositionIfConditionTrue()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("x"));
                buffer.NextLine();
                buffer.ConsumeWhile(c => true);
                var origPos = buffer.LinePos;
                Assert.AreEqual(origPos, buffer.LinePos);
            }

            [TestMethod]
            public void ConsumeWhile_AdvancesToPositionWhenConditionTrue()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("abc"));
                buffer.NextLine();
                buffer.ConsumeWhile(c => c != 'c');
                Assert.AreEqual(2, buffer.LinePos);
            }

            [TestMethod]
            public void ConsumeWhile_AdvancesToEndOfLineIfConditionNeverTrue()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader("abc"));
                buffer.NextLine();
                buffer.ConsumeWhile(c => c != 'd');
                Assert.AreEqual(3, buffer.LinePos);
            }

            [TestMethod]
            public void ConsumeWhile_DoesNotAdvanceToNextLine()
            {
                var buffer = new CsvReader.CsvReader.Buffer(new StringReader($"abc{Environment.NewLine}d"));
                buffer.NextLine();
                buffer.ConsumeWhile(c => c != 'd');
                Assert.AreEqual(0, buffer.LineNumber);
            }
        }
    }
}
