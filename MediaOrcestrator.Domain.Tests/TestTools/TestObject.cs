namespace MediaOrcestrator.Domain.Tests.TestTools;

public abstract class TestObject
{
    public SyncEnvironment Environment { get; private set; } = null!;
    protected bool IsNew { get; set; } = true;

    public virtual void Attach(SyncEnvironment env)
    {
        Bind(env);
        env.AddObject(this);
    }

    public void Bind(SyncEnvironment env)
    {
        Environment = env;
    }

    public abstract void LocalSave();

    public TestObject SaveObject()
    {
        LocalSave();
        return this;
    }
}
