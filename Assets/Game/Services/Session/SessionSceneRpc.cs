using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionSceneRpc : SimulationBehaviour
{
    [Rpc]
    public static void RPC_LoadGame(NetworkRunner runner)
    {
        if (!runner.IsSceneAuthority || runner.IsSceneManagerBusy)
            return;

        LoadGameAsync(runner).Forget(Debug.LogException);
    }

    private static async UniTask LoadGameAsync(NetworkRunner runner)
    {
        await runner.LoadScene(
            SceneRef.FromIndex(SceneIndices.Game),
            LoadSceneMode.Additive,
            setActiveOnLoad: true);

        await runner.UnloadScene(SceneRef.FromIndex(SceneIndices.Lobby));
    }
}
