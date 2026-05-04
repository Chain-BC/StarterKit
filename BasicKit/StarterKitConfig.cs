using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace StarterKit;
public class StarterKitConfig
{
    public bool requiresPrivilege = true;
    public string privilege = "starterkit";
    public readonly List<string[]> kitItems = new(); // Each string in the list has the item name at index 0 and the item amount at index 1
    public int maxKitUses = 1;

    public static void TryToLoadConfig(ICoreAPI serverAPI, ref StarterKitConfig config)
    {
        try
        {
            config = serverAPI.LoadModConfig<StarterKitConfig>("StarterKitConfig.json");
            config ??= new StarterKitConfig();
            serverAPI.StoreModConfig(config, "StarterKitConfig.json");
        }
        catch (Exception e)
        {
            serverAPI.Logger.Error("Could not load config! Loading default settings instead.");
            serverAPI.Logger.Error(e);
            config ??= new StarterKitConfig();
        }
    }

    public static bool LoadFromFileConfig(ICoreAPI serverAPI, ref StarterKitConfig config)
    {
        bool success = true;
        try
        {
            config = serverAPI.LoadModConfig<StarterKitConfig>("StarterKitConfig.json");
        }
        catch (Exception e)
        {
            serverAPI.Logger.Error("Could not load config from file!");
            serverAPI.Logger.Error(e);
            success = false;
        }
        return success;
    }

    public static void StoreToFileConfig(ICoreAPI serverAPI, ref StarterKitConfig config)
    {
        try
        {
            serverAPI.StoreModConfig(config, "StarterKitConfig.json");
        }
        catch (Exception e)
        {
            serverAPI.Logger.Error("Could not store config to file!");
            serverAPI.Logger.Error(e);
        }
    }

}
