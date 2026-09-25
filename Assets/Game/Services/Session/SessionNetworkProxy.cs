using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionNetworkProxy : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_LoadScene(
        int index,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        LoadSceneAsync(index, mode).Forget(Debug.LogException);
    }

    private async UniTask LoadSceneAsync(int index, LoadSceneMode mode)
    {
        await Runner.LoadScene(
            SceneRef.FromIndex(index),
            mode,
            setActiveOnLoad: true);
    }
}