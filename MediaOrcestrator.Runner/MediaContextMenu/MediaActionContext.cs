using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Domain.Merging;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner.MediaContextMenu;

public sealed record MediaActionContext(
    Orcestrator Orcestrator,
    SyncRetryRunner RetryRunner,
    BatchRenameService BatchRenameService,
    BatchPreviewService BatchPreviewService,
    CoverGenerator CoverGenerator,
    CoverTemplateStore CoverTemplateStore,
    MediaMergeService MergeService,
    ActionHolder ActionHolder,
    CommentsService CommentsService,
    SettingsManager SettingsManager,
    ILoggerFactory LoggerFactory,
    ILogger Logger,
    IMediaActionUi Ui);
