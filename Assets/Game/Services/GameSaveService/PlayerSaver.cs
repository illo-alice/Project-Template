using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class PlayerSaver : NetworkBehaviour
{
    private GameSaveService _saveService;
    private string _playerId;

    [Inject]
    public void Construct(GameSaveService saveService)
    {
        _saveService = saveService;
    }

    public override void Spawned()
    {
        // The host registers every player's state, not just its own character.
        if (!Runner.IsServer) return;
        if (_saveService == null)
            throw new InvalidOperationException("PlayerSaver must be instantiated through SessionObjectProvider.");
        if (Object.InputAuthority == PlayerRef.None)
            throw new InvalidOperationException("PlayerSaver requires a player with InputAuthority.");

        var playerId = Runner.GetPlayerUserId(Object.InputAuthority);
        _saveService.BindPlayer(playerId, this);
        _playerId = playerId;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_playerId == null) return;
        _saveService.UnbindPlayer(_playerId, this);
        _playerId = null;
    }

    public async UniTask<PlayerSaveData> Save()
    {
        var result = new PlayerSaveData();
        var dataManipulators = GetComponentsInChildren<IDataManipulator>();

        foreach (var dataManipulator in dataManipulators)
        {
            var data = await dataManipulator.Save();
            var key = dataManipulator.Key;
            
            result.Values.Add(key, data);
        }
        
        return result;
    }

    public async UniTask Load(PlayerSaveData data)
    {
        var dataManipulators = GetComponentsInChildren<IDataManipulator>().ToDictionary(k => k.Key, v => v);
        
        foreach (var (key, value) in data.Values)
        {
            await dataManipulators[key].Load(value);
        }
    }
}
