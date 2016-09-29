using CsvReader;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace CsvReaderUnitTest
{
    [TestClass]
    public class CsvReaderUnitTest
    {
        [TestMethod]
        public void ReadRecord_ReturnsValuesDelimitedByComma()
        {
            var reader = CsvReader.CsvReader.Parse("a,b,c");
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, reader.ReadRecord().ToArray());
        }

        [TestMethod]
        public void ReadRecord_ReturnsNullAfterLastLine()
        {
            var reader = CsvReader.CsvReader.Parse("a,b,c");
            reader.ReadRecord();
            Assert.IsNull(reader.ReadRecord());
        }
    }
}
