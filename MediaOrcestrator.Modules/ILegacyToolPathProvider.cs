namespace MediaOrcestrator.Modules;

public interface ILegacyToolPathProvider
{
    string? GetLegacyToolPath(string toolName);
}
