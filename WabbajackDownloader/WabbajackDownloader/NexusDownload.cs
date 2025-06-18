using System;
using System.Text;

namespace WabbajackDownloader;

internal record NexusDownload(GameData Game, string FileName, string ModID, string FileID, ulong FileSize, string Hash)
{
    public string GameID => Game.NexusGameID.ToString();

    public string Url
    {
        get => url ??= GetDownloadUrl();
    }
    private string? url;

    public Uri Uri
    {
        get => uri ??= GetDownloadUri();
    }
    private Uri? uri;

    private Uri GetDownloadUri() => new(GetDownloadUrl());

    private string GetDownloadUrl()
    {
        StringBuilder sb = new("https://www.nexusmods.com/");
        sb.Append(Game.NexusName);
        sb.Append("/mods/");
        sb.Append(ModID);
        sb.Append("?tab=files&file_id=");
        sb.Append(FileID);
        return sb.ToString();
    }
}
