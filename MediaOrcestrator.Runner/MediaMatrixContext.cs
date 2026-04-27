using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Domain.Merging;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public sealed record MediaMatrixContext(
    Orcestrator Orcestrator,
    SyncRetryRunner RetryRunner,
    ILogger<MediaMatrixGridControl> Logger,
    SettingsManager SettingsManager,
    BatchRenameService BatchRenameService,
    BatchPreviewService BatchPreviewService,
    CoverGenerator CoverGenerator,
    CoverTemplateStore CoverTemplateStore,
    MediaMergeService MergeService,
    ActionHolder ActionHolder,
    CommentsService CommentsService,
    ILoggerFactory LoggerFactory);
