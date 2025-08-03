using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;
using WabbajackDownloader.Hashing;
using WabbajackDownloader.ModList;

namespace WabbajackDownloader.Core;

internal static class ModlistExtractor
{
    public static List<NexusDownload> ExtractDownloadLinks(string wabbajackFile, ILogger? logger)
    {
        var downloads = new List<NexusDownload>();
        using var stream = File.Open(wabbajackFile, FileMode.Open, FileAccess.Read);
        using var file = new ZipArchive(stream, ZipArchiveMode.Read);

        // the archive should contain a file named "modlist"
        var entry = file.GetEntry("modlist") ?? throw new InvalidDataException("The wabbajack file does not contain a modlist.");
        logger?.LogInformation("Extracting downloadable mods from {wabbajackFile}.", wabbajackFile);
        var modlist = JsonNode.Parse(entry.Open())?.AsObject();
        if (modlist != null)
        {
            // the element "Archives" should exist within modlist
            var archives = modlist["Archives"]?.AsArray() ?? modlist["archives"]?.AsArray();
            if (archives != null)
            {
                // iterate through each archive entry
                foreach (var archive in archives)
                {
                    var archiveObject = archive?.AsObject();
                    // get the "Name" element that contains the file name
                    var name = archiveObject?["Name"]?.ToString() ?? archiveObject?["name"]?.ToString();
                    // get the "Size" element that contains the file size
                    var size = long.Parse(archiveObject?["Size"]?.ToString() ?? archiveObject?["size"]?.ToString() ?? "0");
                    // get the "Hash" element that contains the computed hash
                    var hash = archiveObject?["Hash"]?.ToString() ?? archiveObject?["hash"]?.ToString();
                    // get the "Meta" element that contains the game name, mod ID, and file ID
                    var meta = archiveObject?["Meta"]?.ToString() ?? archiveObject?["meta"]?.ToString();
                    if (name == null || hash == null || meta == null)
                    {
                        logger?.LogTrace("This entry is not a nexus download. Skipping ahead.\nInvalid entry:\n{entry}", archiveObject);
                        continue;
                    }

                    // extract game name, mod id, and file id
                    if (ExtractValues(meta, out var gameName, out var modID, out var fileID))
                    {
                        if (GameData.GameLookup.TryGetValue(gameName, out var game))
                        {
                            // add valid entry to list
                            var download = new NexusDownload(game, name, modID, fileID, size, Hash.Interpret(hash));
                            downloads.Add(download);
                            logger?.LogTrace("Mod {name} is added to download.", name);
                        }
                    }
                    else
                    {
                        logger?.LogTrace("Cannot extract game name, mod ID, and file ID from:\n{meta}.", meta);
                    }
                }
            }
            else
            {
                logger?.LogError("Cannot extract downloads from wabbajack file: no Archives entry found.");
            }
        }
        else
            logger?.LogError("Cannot extract downloads from wabbajack file: modlist is not a valid json object.");

        logger?.LogInformation("Extracted {count} downloadable mods from modlist.", downloads.Count);
        return downloads;
    }

    /// <summary>
    /// Attempt to extract the game name, mod id, and file id from a meta string
    /// </summary>
    private static bool ExtractValues(string meta, out string gameName, out string modID, out string fileID)
    {
        gameName = string.Empty;
        modID = string.Empty;
        fileID = string.Empty;

        using var reader = new StringReader(meta);
        reader.ReadLine();
        gameName = ExtractValue(reader.ReadLine());
        modID = ExtractValue(reader.ReadLine());
        fileID = ExtractValue(reader.ReadLine());

        return gameName != string.Empty && modID != string.Empty && fileID != string.Empty;

        static string ExtractValue(ReadOnlySpan<char> line)
        {
            var separatorIndex = line.IndexOf('=');
            return separatorIndex == -1 ? string.Empty : line[(separatorIndex + 1)..].ToString();
        }
    }
}
