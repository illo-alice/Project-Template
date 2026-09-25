# Installation

## Dependencies

Use **Unity 6000.5.8f1** with Windows Build Support. Git must be available for UPM dependencies. Import the SDKs below before expecting a clean compile; enter Unity Safe Mode if needed during the initial import.

| Dependency | Installation |
| --- | --- |
| [Photon Fusion 2.1](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download) | Import **2.1.2 Stable, build 2279**, the SDK used for this project. Keep its original `.meta` files. SDK code belongs in `Assets/Photon` and is gitignored. |
| [Photon Voice for Fusion](https://github.com/Photon-Server/Photon-UPM) | Restored by the Git URL in `Packages/manifest.json`; the lock file pins the revision used here (package version **2.63.0**). Fusion 2.1 requires the Realtime 5 compatible edition. |
| [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks/releases) | Import the release's **Unity** distribution into `Assets/Plugins/Facepunch.Steamworks`. Include the Windows x64 managed assembly and matching `steam_api64.dll`; avoid importing duplicate Win32/Posix assemblies into the same target. See [official setup](https://wiki.facepunch.com/steamworks/Setting_Up). |
| [UniTask 2.5.11](https://github.com/Cysharp/UniTask/releases/tag/2.5.11) | Import the Unity package into `Assets/Plugins/UniTask`, including the Addressables/DOTween integration. Do not also install a second UPM copy. |
| [DOTween](https://dotween.demigiant.com/download.php) | Import the free Unity package into `Assets/Plugins/Demigiant`. Run **Tools → Demigiant → DOTween Utility Panel → Setup DOTween**; enable UI modules and create assembly definitions. |
| VContainer 1.19.0, URP, Input System, Localization, Addressables, Newtonsoft JSON | Restored by Package Manager. Keep both `manifest.json` and `packages-lock.json`. |
| TextMesh Pro resources | Import **TMP Essential Resources** when Unity prompts, or use **Window → TextMeshPro → Import TMP Essential Resources**. Original resource GUIDs are referenced by the UI. |

SDK files are not bundled. Preserve vendor `.meta` files when importing so scene/prefab script references resolve. The font and generated UI font materials are included with their OFL license. UI click/hover SoundDefinitions intentionally have empty clip lists: assign your own licensed clips if wanted; the mixer and audio service remain configured.

## Photon and scenes

1. Create your own **Fusion** and **Voice** App IDs in the [Photon Dashboard](https://dashboard.photonengine.com/). Enter them in Photon App Settings after installing the SDKs. These local settings are excluded from Git.
2. Open Fusion's Network Project Config. Ensure `Assembly-CSharp`, `Assembly-CSharp-firstpass` and `PhotonVoice.Fusion` are in **Assemblies To Weave**. Rebuild the Fusion prefab table if the editor requests it; `Assets/Game/Prefabs/Player.prefab` must be registered.
3. The build scene order is **Bootstrap → Menu → Lobby → Game**. Keep that order: the session code uses scene indexes. The removed test scene is not part of the build.
4. Open `Assets/Game/Scenes/Bootstrap.unity`. Start Steam and enter Play Mode. Player identity is a local development GUID; implement verified authentication before relying on it for a shipped game.
5. For a player build, use the Windows Steam build profile and build Addressables player content if requested. Only Windows Steam behavior has been exercised locally.

## Steam testing with App ID 480

`SteamService.AppId` is **480**, Steam's Spacewar test app. No personal App ID is required for local integration experiments. The name shown by Steam will remain Spacewar.

For an overlay-enabled Windows build, configure **Spacewar → Properties → General → Launch Options** on each tester's Steam installation:

```text
"C:\Path\To\Your\Game.exe" %command%
```

Use the actual absolute path on that computer. Steam launches that executable under App ID 480; `%command%` expands to Spacewar's original command and is passed to your executable as arguments. Clear Launch Options to restore ordinary Spacewar. If Spacewar is not installed, install Valve's test application through Steam first.

Launch Spacewar from Steam, or open your exe and let `RestartAppIfNecessary(480)` request the Steam launch. Without the Launch Options override, Steam opens its installed Spacewar executable instead of this project.

**Do not put `steam_appid.txt` beside the build or in its working directory for this mode.** It suppresses `RestartAppIfNecessary`, so a closed Steam client will not be started. A direct Unity launch can initialize Steam too late for the overlay. A non-Steam shortcut uses a different launch identity and produced an incorrect invite menu during testing.

For Editor testing keep Steam open. For multiplayer testing start both builds under separate Steam accounts, create a room, then invite or join by code. Verify host exit, disconnect, repeated joins and invitations from another session. Joining from a closed build remains an unverified scenario.

When using your own Steam App ID, change `SteamService.AppId`, configure the real application's launch executable in Steamworks and remove the Spacewar override. [Valve's initialization and restart documentation](https://partner.steamgames.com/doc/sdk/api#SteamAPI_RestartAppIfNecessary) explains `steam_appid.txt` and relaunch behavior.
