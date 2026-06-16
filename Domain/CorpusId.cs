using System.Globalization;

namespace MaichessInsightsService.Domain;

// Builds the human-readable, reproducible corpus id from a source + filter. Lichess
// corpora encode the month and any filter dimensions so different slices never blur
// (e.g. "lichess-2024-12-blitz-1600-1999-s15"); uploads get a unique "upload-{id}".
internal static class CorpusId
{
    internal static string ForLichess(string yearMonth, CorpusFilterSpec filter)
    {
        List<string> tokens = ["lichess", yearMonth];
        if (!string.IsNullOrWhiteSpace(filter.TimeControl))
        {
            tokens.Add(Slug.Make(filter.TimeControl));
        }

        if (!string.IsNullOrWhiteSpace(filter.RatingBand))
        {
            tokens.Add(Slug.Make(filter.RatingBand));
        }

        if (filter.SampleRate > 0 && filter.SampleRate < 1)
        {
            int percent = (int)Math.Round(filter.SampleRate * 100, MidpointRounding.AwayFromZero);
            tokens.Add("s" + percent.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join('-', tokens);
    }

    internal static string ForUpload(string id) => "upload-" + Slug.Make(id);
}
