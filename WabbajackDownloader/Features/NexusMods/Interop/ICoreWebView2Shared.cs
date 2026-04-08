using System.Runtime.InteropServices;

namespace WabbajackDownloader.Features.NexusMods.Interop;

// EventRegistrationToken is a value type used by all add_/remove_ event pairs
[StructLayout(LayoutKind.Sequential)]
public struct EventRegistrationToken
{
    public long Value;
}

public enum CoreWebView2DownloadState
{
    InProgress = 0,
    Interrupted = 1,
    Completed = 2,
}

public enum CoreWebView2DownloadInterruptReason
{
    None = 0,
    FileFailed = 1,
    FileAccessDenied = 2,
    FileNoSpace = 3,
    FileNameTooLong = 4,
    FileTooLarge = 5,
    FileMalicious = 6,
    FileTransientError = 7,
    FileBlockedByPolicy = 8,
    FileSecurityCheckFailed = 9,
    FileTooShort = 10,
    FileHashMismatch = 11,
    NetworkFailed = 12,
    NetworkTimeout = 13,
    NetworkDisconnected = 14,
    NetworkServerDown = 15,
    NetworkInvalidRequest = 16,
    ServerFailed = 17,
    ServerNoRange = 18,
    ServerBadContent = 19,
    ServerUnauthorized = 20,
    ServerCertificateProblem = 21,
    ServerForbidden = 22,
    ServerUnexpectedResponse = 23,
    ServerContentLengthMismatch = 24,
    ServerCrossOriginRedirect = 25,
    UserCanceled = 26,
    UserShutdown = 27,
    UserPaused = 28,
    DownloadProcessCrashed = 29,
}