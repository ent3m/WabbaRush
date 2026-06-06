namespace WabbajackDownloader.Features.Dashboard;

partial class DownloadPageViewModel
{
    /// <summary>
    /// The general download state of <see cref="DownloadContext"/>.
    /// </summary>
    private enum DownloadState
    {
        /// <summary>
        /// The item is initialized and waiting to be downloaded.
        /// </summary>
        Idle,
        /// <summary>
        /// Active network stream ingestion is taking place.
        /// </summary>
        Downloading,
        /// <summary>
        /// The stream was interrupted but remains alive and resumable.
        /// </summary>
        Paused,
        /// <summary>
        /// A recoverable error occurred. Can be resetted to the <see cref="Idle"/> state.
        /// </summary>
        Retrying,
        /// <summary>
        /// An unrecoverable failure occurred. This download item should be skipped.
        /// </summary>
        Failed,
        /// <summary>
        /// Download canceled by user or system, signaling cessation of all download activity.
        /// </summary>
        Canceled,
        /// <summary>
        /// A systemic failure or security block occurred. Subsequent downloads should be aborted.
        /// </summary>
        Fatal,
        /// <summary>
        /// The file has been fully downloaded and its hash integrity is verified.
        /// </summary>
        Completed
    }
}
