using System.Security.Cryptography;
using System.Text;

namespace MediaOrcestrator.Runner;

internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex? _mutex;

    private SingleInstanceGuard(Mutex? mutex, bool isPrimaryInstance)
    {
        _mutex = mutex;
        IsPrimaryInstance = isPrimaryInstance;
    }

    public bool IsPrimaryInstance { get; }

    public static SingleInstanceGuard Acquire()
    {
        Mutex? mutex = null;
        try
        {
            bool createdNew;
            try
            {
                mutex = new(true, $@"Global\MediaOrcestrator.{ComputeInstanceKey()}", out createdNew);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or WaitHandleCannotBeOpenedException)
            {
                return new(null, true);
            }

            if (!createdNew)
            {
                return new(null, false);
            }

            var guard = new SingleInstanceGuard(mutex, true);

            mutex = null;
            return guard;
        }
        finally
        {
            mutex?.Dispose();
        }
    }

    public void Dispose()
    {
        _mutex?.Dispose();
    }

    private static string ComputeInstanceKey()
    {
        var path = AppContext.BaseDirectory
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToLowerInvariant();

        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(path)));
    }
}
