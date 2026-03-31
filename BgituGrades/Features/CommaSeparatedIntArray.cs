using System.Diagnostics.CodeAnalysis;

namespace BgituGrades.Features
{
    public record CommaSeparatedIntArray(int[] Values) : IParsable<CommaSeparatedIntArray>
    {
        public static CommaSeparatedIntArray Parse(string s, IFormatProvider? provider)
        {
            if (!TryParse(s, provider, out var result))
                throw new FormatException("Invalid format");
            return result;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CommaSeparatedIntArray result)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                result = new CommaSeparatedIntArray([]);
                return true;
            }

            var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var ints = new List<int>();

            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out var val))
                    ints.Add(val);
            }

            result = new CommaSeparatedIntArray([.. ints]);
            return true;
        }

        public static implicit operator int[](CommaSeparatedIntArray wrapper) => wrapper.Values;
    }
}
