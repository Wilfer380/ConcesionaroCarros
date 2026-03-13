using System;
using System.Collections.Generic;

namespace SistemaDeInstalacion.Tests
{
    internal static class AssertEx
    {
        public static void True(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        public static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        public static void NotNull(object value, string message)
        {
            if (value == null)
                throw new InvalidOperationException(message);
        }

        public static void Null(object value, string message)
        {
            if (value != null)
                throw new InvalidOperationException(message);
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + $" Esperado: '{expected}'. Actual: '{actual}'.");
        }

        public static void Contains(string expectedSubstring, string actualValue, string message)
        {
            if (actualValue == null || actualValue.IndexOf(expectedSubstring, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    message + $" Subcadena esperada: '{expectedSubstring}'. Actual: '{actualValue}'.");
        }
    }
}
