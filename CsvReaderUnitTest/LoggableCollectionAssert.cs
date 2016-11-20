using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Scb
{
    /// <summary>
    /// Extends the features of CollectionAssert by logging the expected and actual values if the 
    /// assertion fails.  Note that you may _not_ want to use this if collections are large since 
    /// the output would be voluminous.
    /// </summary>
    public static class LoggableCollectionAssert
    {
        private static string AsText(IEnumerable collection)
        {
            var items = new List<string>();
            foreach (var i in collection)
                items.Add("<" + i + ">");
            return string.Join(", ", items);
        }

        public static void Contains(ICollection collection, object element)
        {
            CollectionAssert.Contains(collection, element,
                $"Expected element:<{element}>. Actual collection:<{AsText(collection)}>.");
        }

        /// <summary>
        /// Fails if the two collection do not have the same items in the same order.
        /// </summary>
        public static void AreEqual(ICollection expected, ICollection actual)
        {
            CollectionAssert.AreEqual(expected, actual,
                $"Expected:<{AsText(expected)}>. Actual:<{AsText(actual)}>.");
        }

        /// <summary>
        /// Fails if the two collection do not have the same items ignoring order.
        /// </summary>
        public static void AreEquivalent(ICollection expected, ICollection actual)
        {
            CollectionAssert.AreEquivalent(expected, actual,
                $"Expected:<{AsText(expected)}>. Actual:<{AsText(actual)}>.");
        }

        public static void IsSubsetOf(ICollection subset, ICollection superset)
        {
            CollectionAssert.IsSubsetOf(subset, superset,
                $"Expected subset:<{AsText(subset)}>. Actual:<{AsText(superset)}>.");
        }
    }
}
