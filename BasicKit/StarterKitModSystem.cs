using System.Collections.Generic;
using StarterKit.Commands;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace StarterKit;
public class StarterKitModSystem : ModSystem
{
    private readonly ModData _data = new();
    public required ICoreServerAPI serverAPI;
    
    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        serverAPI = api;
        StarterKitCommands commands = new(api, _data);
        
        api.Event.SaveGameLoaded += OnSaveGameLoading;
        api.Event.GameWorldSave += OnSaveGameSaving;
        api.Event.PlayerJoin += OnPlayerJoin;
        
        StarterKitConfig.TryToLoadConfig(serverAPI, ref _data.Config); // Load the initial config
        base.StartServerSide(serverAPI);
        commands.RegisterServerCommands();

    }

    private void OnSaveGameLoading()
    {
        byte[] byteData = serverAPI.WorldManager.SaveGame.GetData("StarterKitPlayerData");
        _data.Players = byteData == null ? new List<PlayerData>() : SerializerUtil.Deserialize<List<PlayerData>>(byteData);
    }

    private void OnSaveGameSaving()
    {
        serverAPI.WorldManager.SaveGame.StoreData("StarterKitPlayerData", SerializerUtil.Serialize(_data.Players));
    }

    private void OnPlayerJoin(IServerPlayer player)
    {
        int playerIndex = _data.Players.FindIndex(currPlayer => currPlayer.UID == player.PlayerUID);
        if (playerIndex == -1)
        {
            PlayerData tempPlayer = new PlayerData(player.PlayerUID, _data.Config.maxKitUses);
            _data.Players.Add(tempPlayer);
        }
        
    }

}

