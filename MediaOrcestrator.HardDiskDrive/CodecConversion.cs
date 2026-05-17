namespace MediaOrcestrator.HardDiskDrive;

internal sealed record CodecConversion(
    int Id,
    string Name,
    string Label,
    string SourceCodec,
    string OutputExtension,
    TranscodeOperation Transcode);