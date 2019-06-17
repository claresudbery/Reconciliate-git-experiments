using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleCatchall.Console.Reconciliation.Utils
{
    internal static class StringHelper
    {
        public static System.Globalization.CultureInfo Culture()
        {
            return new System.Globalization.CultureInfo("en-GB");
        }

        public static string[] Make_sure_there_are_at_least_enough_string_values(int minValuesRequired, string[] sourceArray)
        {
            var list = new List<string>(sourceArray);

            if (sourceArray.Length < minValuesRequired)
            {
                list.AddRange(Enumerable.Repeat(String.Empty, minValuesRequired - sourceArray.Length));
            }

            return list.ToArray();
        }
    }
}