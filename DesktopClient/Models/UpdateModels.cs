using System;

namespace MicroluxErgConnect.Models;

public record UpdateManifest(
    Version Version,
    string DownloadUrl,
    string? ReleaseNotes,
    DateTime? ReleasedAt);

public record UpdateState(
    bool UpdateAvailable,
    Version? LatestVersion,
    string? DownloadedFile,
    string StatusMessage);
