using Fusion;
using VContainer;
using VContainer.Unity;

public class SessionObjectProvider : NetworkObjectProviderDefault
{
    private IObjectResolver _objectResolver;

    public void SetObjectResolver(IObjectResolver objectResolver)
    {
        _objectResolver = objectResolver;
    }
    
    protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
    {
        return _objectResolver.Instantiate(prefab);
    }
}
