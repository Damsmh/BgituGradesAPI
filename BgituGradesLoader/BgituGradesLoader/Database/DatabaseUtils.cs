using System.Text.RegularExpressions;

namespace BgituGradesLoader.Database
{
    public static class DatabaseUtils
    {
        private const string LECTURE_VALUE = "LECTURE";
        private const string PRACTICE_VALUE = "PRACTICE";
        private const string EXTRA_SYMBOLS = " .,-";

        public static string GetPairType(bool isLecture)
        {
            if (isLecture)
                return LECTURE_VALUE;
            return PRACTICE_VALUE;
        }

        public static string NormalizeDisciplineForDatabase(string? disciplineName)
        {
            if (disciplineName == null)
                return string.Empty;

            disciplineName = disciplineName.Trim();
            disciplineName = Regex.Replace(disciplineName, @"\s+", " ");
            return disciplineName;
        }

        public static string NormalizeDisciplineForFiltering(this string? disciplineName)
        {
            if (disciplineName == null)
                return string.Empty;

            disciplineName = Regex.Replace(disciplineName, @"[\s.,-]+", "");
            disciplineName = disciplineName.ToLower();
            return disciplineName;
        }

        public static int CountExtraSymbols(this string? disciplineName)
        {
            int count = 0;
            if (disciplineName == null)
                return count;

            foreach (char symbol in disciplineName)
                if (EXTRA_SYMBOLS.Contains(symbol))
                    count++;
            return count;
        }
    }
}
