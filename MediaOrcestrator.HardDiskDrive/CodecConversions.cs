namespace MediaOrcestrator.HardDiskDrive;

internal static class CodecConversions
{
    public static readonly IReadOnlyList<CodecConversion> All =
    [
        new(1, "vp9 to h264", "VP9→H264", "vp9", ".mp4",
            (t, src, dst, dur, p, ct) => t.TranscodeVp9ToH264Async(src, dst, dur, p, ct)),
        new(2, "h264 to vp9", "H264→VP9", "h264", ".webm",
            (t, src, dst, dur, p, ct) => t.TranscodeH264ToVp9Async(src, dst, dur, p, ct)),
        new(3, "av1 to h264", "AV1→H264", "av1", ".mp4",
            (t, src, dst, dur, p, ct) => t.TranscodeAv1ToH264Async(src, dst, dur, p, ct)),
    ];

    public static CodecConversion? Find(int id)
    {
        return All.FirstOrDefault(c => c.Id == id);
    }
}
