using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Features.Dashboard;

/// <summary>
/// Represents the data required to procure a Wabbajack modlist and download files.
/// </summary>
internal abstract record ModListPreparationOptions();
internal sealed record LocalModListPreparationOptions(string LocalFilePath) : ModListPreparationOptions;
internal sealed record RemoteModListPreparationOptions(ModListMetadata Metadata) : ModListPreparationOptions;