using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace YtDlpDownloader.Services.Parser;

internal static class YtDlpFormatMetadata
{
    private static readonly HashSet<string> NonMediaProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "mhtml",
        "storyboard"
    };

    private static readonly HashSet<string> NonMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "mhtml",
        "json",
        "sb"
    };

    public static List<string> ExtractVideoResolutions(JsonElement formatsProp)
    {
        var resolutions = new SortedSet<int>(Comparer<int>.Create((left, right) => right.CompareTo(left)));

        foreach (var format in formatsProp.EnumerateArray())
        {
            if (TryGetDownloadableVideoHeight(format, out var height))
            {
                resolutions.Add(height);
            }
        }

        var result = resolutions
            .Select(height => $"{height}p")
            .ToList();

        result.Insert(0, "Максимальное");
        return result;
    }

    public static bool IsDownloadableVideoFormat(JsonElement format)
    {
        return TryGetDownloadableVideoHeight(format, out _);
    }

    private static bool TryGetDownloadableVideoHeight(JsonElement format, out int height)
    {
        height = 0;

        var vcodec = GetString(format, "vcodec");
        if (string.IsNullOrWhiteSpace(vcodec) || string.Equals(vcodec, "none", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var protocol = GetString(format, "protocol");
        if (!string.IsNullOrWhiteSpace(protocol) && NonMediaProtocols.Contains(protocol))
        {
            return false;
        }

        var ext = GetString(format, "ext");
        if (string.IsNullOrWhiteSpace(ext) || NonMediaExtensions.Contains(ext))
        {
            return false;
        }

        if (!format.TryGetProperty("height", out var heightProp) || heightProp.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        return heightProp.TryGetInt32(out height) && height > 0;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
    }
}
