using System;

public class RoomCodeService
{
    public string CurrentRoomCode { get; private set; }
    public Action<string> OnRoomCodeChanged;
    public int codeLength = 4;

    public string Generate(string region)
    {
        var regionPart = region.ToLowerInvariant();
        var digitPart = UnityEngine.Random.Range(0, (int)Math.Round(Math.Pow(10, codeLength))).ToString("0000");
        CurrentRoomCode = $"{regionPart}-{digitPart}";
        OnRoomCodeChanged?.Invoke(CurrentRoomCode);
        return CurrentRoomCode;
    }
    
    public void OnClientReceiveCode(string code)
    {
        CurrentRoomCode = code;
        OnRoomCodeChanged?.Invoke(code);
    }
}
