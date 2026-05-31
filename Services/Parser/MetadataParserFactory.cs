using System;
using System.Text.Json;

namespace YtDlpDownloader.Services.Parser;

public class MetadataParserFactory
{
    public IMetadataParser GetParser(JsonElement root)
    {
        bool isPlaylist = root.TryGetProperty("_type", out var typeProp) && 
            typeProp.GetString() == "playlist";

        if (isPlaylist)
            return new PlaylistMetadataParser();
        
        return new VideoMetadataParser();
    }
}