namespace MediaOrcestrator.Domain;

public class MySource
{
    public string Id { get; set; }
    public string TypeId { get; set; }
    public Dictionary<string, string> Settings { get; set; }

    public string Title
    {
        get
        {
            if (!Settings.ContainsKey("_system_name"))
            {
                return "<noname>";
            }

            var title = Settings["_system_name"];
            return title;
        }
    }
}
