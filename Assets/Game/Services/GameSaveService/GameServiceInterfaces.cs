using Cysharp.Threading.Tasks;

public interface ISaveable
{
    public UniTask<string> Save();
}

public interface ILoadable
{
    public UniTask Load(string data);
}

public interface IDataId
{
    public string Key { get; }
}

public interface IDataManipulator : ISaveable, ILoadable, IDataId
{
    
}