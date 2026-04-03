using System.Text.RegularExpressions;

namespace BgituGrades.Features
{
    public static class GroupCourseParser
    {
        private static readonly Regex CourseRegex =
            new(@"-(\d)\d{2}", RegexOptions.Compiled);

        public static int Parse(string? name)
        {
            var match = CourseRegex.Match(name!);
            return int.Parse(match.Groups[1].Value);
        }
    }
}
