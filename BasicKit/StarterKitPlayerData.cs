using ProtoBuf;

namespace StarterKit;

[ProtoContract]
public class PlayerData
{
    public PlayerData() {}

    public PlayerData(string playerUID, int currentUsesLeft)
    {
        UID = playerUID;
        UsesLeft = currentUsesLeft;
    }
    
    [ProtoMember(1)]
    public string? UID { get; private set; }

    [ProtoMember(2)]
    public int UsesLeft { get; set; }
}