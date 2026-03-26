namespace MediaOrcestrator.Modules;

public interface IToolConsumer
{
    IReadOnlyList<ToolDescriptor> RequiredTools { get; }
}
