using System;

public class RoomCodeService
{
    public string CurrentRoomCode { get; private set; }
    public Action<string> OnRoomCodeChanged;
    public string Generate()
    {
        CurrentRoomCode = $"{GetNumber()}{GetNumber()}{GetNumber()}{GetNumber()}{GetNumber()}{GetNumber()}";
        OnRoomCodeChanged?.Invoke(CurrentRoomCode);
        return CurrentRoomCode;
    }
    
    private int GetNumber()
    {
        return UnityEngine.Random.Range(0, 10);
    }
    
    public void OnClientReceiveCode(string code)
    {
        CurrentRoomCode = code;
        OnRoomCodeChanged?.Invoke(code);
    }
}
