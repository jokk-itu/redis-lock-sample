namespace WorkerService;
public class ObjectStoreSettings
{
  public ICollection<string> Nodes { get; set; } = new List<string>();
  public string Extra { get; set; } = string.Empty;
}