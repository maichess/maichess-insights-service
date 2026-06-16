using System.Text;

namespace MaichessInsightsService.Domain;

// Lowercase, RFC1123-friendly slug used for corpus ids and SparkApplication names:
// keep [a-z0-9], collapse every other run to a single '-', trim leading/trailing '-'.
internal static class Slug
{
    internal static string Make(string value)
    {
        StringBuilder sb = new(value.Length);
        bool pendingDash = false;
        foreach (char c in value.ToLowerInvariant())
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                if (pendingDash && sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append(c);
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }
        }

        return sb.ToString();
    }
}
</content>
