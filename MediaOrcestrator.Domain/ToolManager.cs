using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace MediaOrcestrator.Domain;

public class ToolManager(
    string toolsRoot,
    IReleaseProvider releaseProvider,
    ToolVersionDetector versionDetector,
    ToolInstaller installer,
    ILogger<ToolManager> logger) : IToolPathProvider
{
    private readonly ConcurrentDictionary<string, ToolDescriptor> _registry = new();
    private readonly ConcurrentDictionary<string, List<IToolConsumer>> _consumers = new();
    private readonly ConcurrentDictionary<string, string?> _resolvedPaths = new();
    private readonly ConcurrentDictionary<string, string?> _companionPaths = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastChecked = new();
    private readonly ConcurrentDictionary<string, ToolStatus> _cachedStatuses = new();

    public string? GetToolPath(string toolName)
    {
        return _resolvedPaths.GetValueOrDefault(toolName);
    }

    public string? GetCompanionPath(string toolName, string companionName)
    {
        return _companionPaths.GetValueOrDefault($"{toolName}:{companionName}");
    }

    public void RegisterTools(IEnumerable<IToolConsumer> consumers)
    {
        foreach (var consumer in consumers)
        {
            foreach (var tool in consumer.RequiredTools)
            {
                ValidateDescriptor(tool);

                if (_registry.TryGetValue(tool.Name, out var existing))
                {
                    if (existing.GitHubRepo != tool.GitHubRepo)
                    {
                        throw new InvalidOperationException($"Конфликт регистрации инструмента '{tool.Name}': '{existing.GitHubRepo}' vs '{tool.GitHubRepo}'");
                    }

                    if (tool.CompanionExecutables is not null)
                    {
                        var merged = existing.CompanionExecutables?.ToList() ?? [];

                        foreach (var companion in tool.CompanionExecutables)
                        {
                            if (!merged.Contains(companion))
                            {
                                merged.Add(companion);
                            }
                        }

                        _registry[tool.Name] = existing with { CompanionExecutables = merged };
                    }
                }
                else
                {
                    _registry[tool.Name] = tool;
                    _consumers[tool.Name] = [];
                }

                _consumers[tool.Name].Add(consumer);
            }
        }

        logger.LogInformation("Зарегистрировано {Count} инструментов: {Names}",
            _registry.Count, string.Join(", ", _registry.Keys));
    }

    public void ResolveAll()
    {
        Directory.CreateDirectory(toolsRoot);

        foreach (var (name, descriptor) in _registry)
        {
            var toolDir = Path.Combine(toolsRoot, name);
            var resolvedPath = ToolInstaller.FindExecutableInToolDir(toolDir, descriptor);

            _resolvedPaths[name] = resolvedPath;
            ResolveCompanions(name, descriptor, resolvedPath);

            if (resolvedPath is not null)
            {
                logger.LogInformation("Инструмент '{Name}' найден: {Path}", name, resolvedPath);
            }
            else
            {
                logger.LogWarning("Инструмент '{Name}' не найден. Установите через панель инструментов", name);
            }
        }
    }

    public async Task<List<ToolStatus>> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _registry.Select(async kv =>
        {
            var (name, descriptor) = kv;
            var path = _resolvedPaths.GetValueOrDefault(name);
            var installedVersion = await versionDetector.GetInstalledVersionAsync(path, descriptor, cancellationToken);

            if (_lastChecked.TryGetValue(name, out var lastCheck)
                && DateTimeOffset.Now - lastCheck < TimeSpan.FromHours(1)
                && _cachedStatuses.TryGetValue(name, out var cached))
            {
                return cached with { InstalledVersion = installedVersion };
            }

            var release = await releaseProvider.GetLatestReleaseAsync(descriptor.GitHubRepo, descriptor.AssetPattern, cancellationToken);

            var latestVersion = release is not null
                ? ToolVersionDetector.NormalizeTagVersion(release.TagName, descriptor.VersionTagPattern)
                : null;

            var updateAvailable = ToolVersionDetector.IsUpdateAvailable(installedVersion, latestVersion);

            _lastChecked[name] = DateTimeOffset.Now;

            var status = new ToolStatus
            {
                Name = name,
                InstalledVersion = installedVersion,
                LatestVersion = latestVersion,
                UpdateAvailable = updateAvailable,
                ResolvedPath = _resolvedPaths.GetValueOrDefault(name),
                LastChecked = _lastChecked[name],
            };

            _cachedStatuses[name] = status;
            return status;
        });

        var results = await Task.WhenAll(tasks);
        return [.. results];
    }

    public async Task UpdateToolAsync(string toolName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!_registry.TryGetValue(toolName, out var descriptor))
        {
            throw new ArgumentException($"Неизвестный инструмент: {toolName}");
        }

        var currentPath = _resolvedPaths.GetValueOrDefault(toolName);

        var resolvedPath = await installer.InstallAsync(toolName, descriptor, toolsRoot, currentPath, progress, cancellationToken);

        _resolvedPaths[toolName] = resolvedPath;
        ResolveCompanions(toolName, descriptor, resolvedPath);

        _cachedStatuses.TryRemove(toolName, out _);
        _lastChecked.TryRemove(toolName, out _);

        logger.LogInformation("Инструмент '{Name}' успешно обновлён. Путь: {Path}", toolName, resolvedPath);
    }

    public IReadOnlyDictionary<string, ToolDescriptor> GetRegistry()
    {
        return _registry;
    }

    private static void ValidateDescriptor(ToolDescriptor tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new ArgumentException("ToolDescriptor.Name обязателен");
        }

        if (string.IsNullOrWhiteSpace(tool.GitHubRepo))
        {
            throw new ArgumentException($"ToolDescriptor.GitHubRepo обязателен для '{tool.Name}'");
        }

        if (string.IsNullOrWhiteSpace(tool.AssetPattern))
        {
            throw new ArgumentException($"ToolDescriptor.AssetPattern обязателен для '{tool.Name}'");
        }

        if (string.IsNullOrWhiteSpace(tool.VersionCommand))
        {
            throw new ArgumentException($"ToolDescriptor.VersionCommand обязателен для '{tool.Name}'");
        }
    }

    private void ResolveCompanions(string toolName, ToolDescriptor descriptor, string? mainPath)
    {
        if (descriptor.CompanionExecutables is null || mainPath is null)
        {
            return;
        }

        var dir = Path.GetDirectoryName(mainPath)!;

        foreach (var companion in descriptor.CompanionExecutables)
        {
            var companionFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"{companion}.exe"
                : companion;

            var companionPath = Path.Combine(dir, companionFileName);
            var key = $"{toolName}:{companion}";
            _companionPaths[key] = File.Exists(companionPath) ? companionPath : null;

            if (File.Exists(companionPath))
            {
                logger.LogInformation("Companion '{Companion}' для '{Tool}' найден: {Path}",
                    companion, toolName, companionPath);
            }
            else
            {
                logger.LogWarning("Companion '{Companion}' для '{Tool}' не найден в {Dir}",
                    companion, toolName, dir);
            }
        }
    }
}
