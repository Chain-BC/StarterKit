using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace StarterKit
{
    public class StarterKitConfig
    {
        public bool requiresPrivilege = true;
        public string privilege = "starterkit";
        public bool removePrivilegeOnKitUse = true;
        public List<string[]> kitItems = new();

        public static void TryToLoadConfig(ICoreAPI serverAPI)
        {
            try
            {
                StarterKitModSystem.config = serverAPI.LoadModConfig<StarterKitConfig>("StarterKitConfig.json");
                StarterKitModSystem.config ??= new StarterKitConfig();
                serverAPI.StoreModConfig<StarterKitConfig>(StarterKitModSystem.config, "StarterKitConfig.json");
            }
            catch (Exception e)
            {
                serverAPI.Logger.Error("Could not load config! Loading default settings instead.");
                serverAPI.Logger.Error(e);
                StarterKitModSystem.config = new StarterKitConfig();
            }
        }

        public static bool LoadFromFileConfig(ICoreAPI serverAPI)
        {
            bool success = true;
            try
            {
                StarterKitModSystem.config = serverAPI.LoadModConfig<StarterKitConfig>("StarterKitConfig.json");
            }
            catch (Exception e)
            {
                serverAPI.Logger.Error("Could not load config from file!");
                serverAPI.Logger.Error(e);
                success = false;
            }
            return success;
        }

        public static void StoreToFileConfig(ICoreAPI serverAPI)
        {
            try
            {
                serverAPI.StoreModConfig<StarterKitConfig>(StarterKitModSystem.config, "StarterKitConfig.json");
            }
            catch (Exception e)
            {
                serverAPI.Logger.Error("Could not store config to file!");
                serverAPI.Logger.Error(e);
            }
        }

    }
}
