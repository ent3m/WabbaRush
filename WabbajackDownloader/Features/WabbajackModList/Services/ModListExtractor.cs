using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;
using WabbajackDownloader.Common.Hashing;

namespace WabbajackDownloader.Features.WabbajackModList;

public class ModlistExtractor(ILogger<ModlistExtractor> logger)
{
    private readonly ILogger<ModlistExtractor> _logger = logger;

    // Extract download links from a local .wabbajack file
    public List<NexusDownload> ExtractDownloadLinks(string wabbajackFile)
    {
        var downloads = new List<NexusDownload>();
        using var stream = File.Open(wabbajackFile, FileMode.Open, FileAccess.Read);
        using var file = new ZipArchive(stream, ZipArchiveMode.Read);

        // The archive should contain a file named "modlist"
        var entry = file.GetEntry("modlist") ?? throw new InvalidDataException("The wabbajack file does not contain a modlist.");
        _logger.LogInformation("Extracting downloadable mods from {wabbajackFile}.", wabbajackFile);
        var modlist = JsonNode.Parse(entry.Open())?.AsObject();
        if (modlist != null)
        {
            // The element "Archives" should exist within modlist
            var archives = modlist["Archives"]?.AsArray() ?? modlist["archives"]?.AsArray();
            if (archives != null)
            {
                // Iterate through each archive entry
                foreach (var archive in archives)
                {
                    var archiveObject = archive?.AsObject();
                    // Get the "Name" element that contains the file name
                    var name = archiveObject?["Name"]?.ToString() ?? archiveObject?["name"]?.ToString();
                    // Get the "Size" element that contains the file size
                    var size = long.Parse(archiveObject?["Size"]?.ToString() ?? archiveObject?["size"]?.ToString() ?? "0");
                    // Get the "Hash" element that contains the hash
                    var hash = archiveObject?["Hash"]?.ToString() ?? archiveObject?["hash"]?.ToString();
                    // Get the "Meta" element that contains the game name, mod ID, and file ID
                    var meta = archiveObject?["Meta"]?.ToString() ?? archiveObject?["meta"]?.ToString();

                    if (name == null || hash == null || meta == null)
                    {
                        _logger.LogDebug("This entry is not a nexus download. Skipping ahead.\nInvalid entry:\n{entry}", archiveObject);
                        continue;
                    }

                    // Extract game name, mod id, and file id
                    if (ExtractValues(meta, out var gameName, out var modID, out var fileID))
                    {
                        if (GameData.GameLookup.TryGetValue(gameName, out var game))
                        {
                            // Add valid entry to list
                            var download = new NexusDownload(game, name, modID, fileID, size, Hash.Interpret(hash));
                            downloads.Add(download);
                            _logger.LogDebug("Mod {name} is added to download.", name);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Cannot extract game name, mod ID, and file ID from:\n{meta}.", meta);
                    }
                }
            }
            else
            {
                _logger.LogError("Cannot extract downloads from wabbajack file: no Archives entry found.");
            }
        }
        else
            _logger.LogError("Cannot extract downloads from wabbajack file: modlist is not a valid json object.");

        _logger.LogInformation("Extracted {count} downloadable mods from modlist.", downloads.Count);
        return downloads;
    }

    // Attempt to extract the game name, mod id, and file id from a meta string
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
