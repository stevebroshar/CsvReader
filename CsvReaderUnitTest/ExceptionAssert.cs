using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvReaderUnitTest
{
    public static class ExceptionAssert
    {
        private static T GetException<T>(Action action, string message = "") where T : Exception
        {
            try
            {
                action();
            }
            catch (T exception)
            {
                return exception;
            }
            throw new AssertFailedException("Expected exception " + typeof(T).FullName + ", but none was propagated.  " + message);
        }

        public static void Propagates<T>(Action action) where T : Exception
        {
            Propagates<T>(action, "");
        }

        public static void Propagates<T>(Action action, string message) where T : Exception
        {
            GetException<T>(action, message);
        }

        public static void Propagates<T>(Action action, Action<T> validation) where T : Exception
        {
            Propagates(action, validation, "");
        }

        public static void Propagates<T>(Action action, Action<T> validation, string message) where T : Exception
        {
            validation(GetException<T>(action, message));
        }
    }
}
