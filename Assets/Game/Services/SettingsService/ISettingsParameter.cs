using Cysharp.Threading.Tasks;

public interface ISettingsParameter
{
    public string CategoryKey { get; }
    public string SaveKey { get; }
    public UniTask Reset();
    public UniTask Load(string data);
    public UniTask<(bool shouldSave, string data)> TrySave();
}
