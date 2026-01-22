using System.Reflection;

namespace MediaOrcestrator.Core;

public class InterfaceScanner
{
    public List<TypeInfo> FindImplementations(string directoryPath, Type interfaceType)
    {
        var implementations = new List<TypeInfo>();

        var assemblies = LoadAllAssemblies(directoryPath);

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .ToList();

                foreach (var type in types)
                {
                    if (interfaceType.IsAssignableFrom(type))
                    {
                        implementations.Add(new()
                        {
                            Type = type,
                            Assembly = assembly,
                            AssemblyPath = assembly.Location,
                        });
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine($"Ошибка загрузки типов из {assembly.FullName}: {ex.Message}");
            }
        }

        return implementations;
    }

    private List<Assembly> LoadAllAssemblies(string directoryPath)
    {
        var assemblies = new List<Assembly>();
        var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                assemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить {dllFile}: {ex.Message}");
            }
        }

        return assemblies;
    }
}

public class TypeInfo
{
    public Type Type { get; set; }
    public Assembly Assembly { get; set; }
    public string AssemblyPath { get; set; }

    public override string ToString()
    {
        return $"{Type.FullName} | {Path.GetFileName(AssemblyPath)}";
    }
}
