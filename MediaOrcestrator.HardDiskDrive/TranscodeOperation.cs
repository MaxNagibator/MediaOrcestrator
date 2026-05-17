using MediaOrcestrator.Modules;

namespace MediaOrcestrator.HardDiskDrive;

internal delegate Task<bool> TranscodeOperation(
    VideoTranscoder transcoder,
    string inputPath,
    string outputPath,
    TimeSpan totalDuration,
    IProgress<double>? progress,
    CancellationToken cancellationToken);