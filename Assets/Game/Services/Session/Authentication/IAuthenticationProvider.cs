using Cysharp.Threading.Tasks;
using Photon.Realtime;

public interface IAuthenticationProvider
{
    public UniTask<AuthenticationValues> GetAuth();
}
