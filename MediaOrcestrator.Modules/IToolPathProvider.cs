namespace MediaOrcestrator.Modules;

public interface IToolPathProvider
{
    string? GetToolPath(string toolName);
    string? GetCompanionPath(string toolName, string companionName);
}
