using System;
using System.Text;
using WabbajackDownloader.Hashing;
using WabbajackDownloader.ModList;

namespace WabbajackDownloader.Core;

internal record NexusDownload(GameData Game, string FileName, string ModID, string FileID, long FileSize, Hash Hash)
{
    public string GameID => Game.NexusGameId.ToString();

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
