using System;
using Cysharp.Threading.Tasks;
using Photon.Realtime;
using UnityEngine;

public class DevelopAuthenticationProvider : IAuthenticationProvider
{
    private const string UserIdKey = "FriendSlop.DevelopAuthentication.UserId";

    public UniTask<AuthenticationValues> GetAuth()
    {
        var id = PlayerPrefs.GetString(UserIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(UserIdKey, id);
            PlayerPrefs.Save();
        }

        return UniTask.FromResult(new AuthenticationValues(id));
    }
}
