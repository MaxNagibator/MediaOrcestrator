using System.Globalization;

namespace MediaOrcestrator.Domain;

public sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var ix = 0;
        var iy = 0;

        while (ix < x.Length && iy < y.Length)
        {
            var cx = x[ix];
            var cy = y[iy];

            if (char.IsDigit(cx) && char.IsDigit(cy))
            {
                var nx = ParseNumber(x, ref ix);
                var ny = ParseNumber(y, ref iy);

                var cmp = nx.CompareTo(ny);

                if (cmp != 0)
                {
                    return cmp;
                }
            }
            else
            {
                var cmp = char.ToLower(cx, CultureInfo.CurrentCulture)
                    .CompareTo(char.ToLower(cy, CultureInfo.CurrentCulture));

                if (cmp != 0)
                {
                    return cmp;
                }

                ix++;
                iy++;
            }
        }

        return x.Length.CompareTo(y.Length);
    }

    private static long ParseNumber(string s, ref int index)
    {
        var start = index;

        while (index < s.Length && char.IsDigit(s[index]))
        {
            index++;
        }

        return long.Parse(s.AsSpan(start, index - start), CultureInfo.InvariantCulture);
    }
}
