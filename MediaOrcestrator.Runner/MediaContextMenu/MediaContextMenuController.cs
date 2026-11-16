using MediaOrcestrator.Runner.MediaContextMenu.Actions;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner.MediaContextMenu;

public sealed class MediaContextMenuController : IDisposable
{
    private readonly MediaActionContext _ctx;
    private readonly IReadOnlyList<IMediaMenuAction> _actions;

    private ContextMenuStrip? _currentMenu;
    private CancellationTokenSource? _menuCts;

    public MediaContextMenuController(MediaActionContext ctx)
    {
        _ctx = ctx;
        _actions =
        [
            new MetadataAction(),
            new SyncAction(),
            new SkipAction(),
            new SkipPlanAction(),
            new EditAction(),
            new CopyDetailsAction(),
            new OpenExternalAction(),
            new DeleteAction(),
            new RemoveFromDatabaseAction(),
            new ConvertAction(),
        ];

        _actions = _actions.OrderBy(a => a.Order).ToList();
    }

    public async void Show(MediaSelection selection, Point screenLocation)
    {
        _menuCts?.Cancel();
        _menuCts?.Dispose();
        _menuCts = new();
        var ct = _menuCts.Token;

        _currentMenu?.Dispose();
        var menu = new ContextMenuStrip();
        _currentMenu = menu;

        if (selection.IsBatch)
        {
            menu.Items.Add(new ToolStripMenuItem($"Выбрано: {selection.Count} медиа") { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());
        }

        var addedAnything = false;
        var asyncSlots = new List<AsyncSlot>();

        foreach (var action in _actions)
        {
            List<MenuItemSpec> items;
            try
            {
                items = action.Build(selection, _ctx).ToList();
            }
            catch (Exception ex)
            {
                _ctx.Logger.LogError(ex, "Ошибка построения пунктов меню для {Action}", action.GetType().Name);
                continue;
            }

            ToolStripSeparator? separator = null;

            if (addedAnything && (items.Count > 0 || action is IAsyncMediaMenuAction))
            {
                separator = new();
                menu.Items.Add(separator);
            }

            foreach (var spec in items)
            {
                menu.Items.Add(BuildMenuItem(spec));
                addedAnything = true;
            }

            if (action is IAsyncMediaMenuAction asyncAction)
            {
                var placeholder = new ToolStripMenuItem(asyncAction.LoadingPlaceholder, asyncAction.LoadingIcon)
                {
                    Enabled = false,
                };

                menu.Items.Add(placeholder);
                asyncSlots.Add(new(asyncAction, placeholder, separator));
                addedAnything = true;
            }
        }

        if (menu.Items.Count == 0)
        {
            menu.Dispose();
            _currentMenu = null;
            return;
        }

        menu.Show(screenLocation);

        foreach (var slot in asyncSlots)
        {
            if (!ReferenceEquals(menu, _currentMenu) || menu.IsDisposed)
            {
                return;
            }

            try
            {
                var asyncItems = (await slot.Action.BuildAsync(selection, _ctx, ct)).ToList();

                if (!ReferenceEquals(menu, _currentMenu) || menu.IsDisposed)
                {
                    return;
                }

                ReplacePlaceholder(menu, slot, asyncItems);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _ctx.Logger.LogWarning(ex, "Ошибка загрузки async-пунктов меню для {Action}", slot.Action.GetType().Name);

                if (menu.IsDisposed)
                {
                    return;
                }

                slot.Placeholder.Text = "Ошибка загрузки";
                slot.Placeholder.Enabled = false;
            }
        }
    }

    public void Dispose()
    {
        _menuCts?.Cancel();
        _menuCts?.Dispose();
        _menuCts = null;

        _currentMenu?.Dispose();
        _currentMenu = null;
    }

    private void ReplacePlaceholder(ContextMenuStrip menu, AsyncSlot slot, List<MenuItemSpec> items)
    {
        var idx = menu.Items.IndexOf(slot.Placeholder);
        menu.Items.Remove(slot.Placeholder);
        slot.Placeholder.Dispose();

        if (items.Count == 0)
        {
            if (slot.Separator != null && menu.Items.Contains(slot.Separator))
            {
                menu.Items.Remove(slot.Separator);
                slot.Separator.Dispose();
            }

            return;
        }

        if (idx < 0)
        {
            idx = menu.Items.Count;
        }

        foreach (var spec in items)
        {
            menu.Items.Insert(idx, BuildMenuItem(spec));
            idx++;
        }
    }

    private ToolStripMenuItem BuildMenuItem(MenuItemSpec spec)
    {
        var item = new ToolStripMenuItem(spec.Text, spec.Icon)
        {
            Enabled = spec.Enabled,
            ToolTipText = spec.Tooltip,
        };

        if (spec.Execute is { } execute && spec.Enabled)
        {
            item.Click += async (_, _) =>
            {
                try
                {
                    await execute();
                }
                catch (OperationCanceledException)
                {
                    // тихая отмена - пользователь сам инициировал, диалог не нужен
                }
                catch (Exception ex)
                {
                    _ctx.Logger.LogError(ex, "Ошибка выполнения пункта меню '{Text}'", spec.Text);

                    MessageBox.Show(_ctx.Ui.Owner,
                        $"Ошибка: {ex.Message}",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };
        }

        return item;
    }

    private sealed record AsyncSlot(IAsyncMediaMenuAction Action, ToolStripMenuItem Placeholder, ToolStripSeparator? Separator);
}
