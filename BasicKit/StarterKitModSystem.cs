using StarterKit.Commands;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StarterKit
{
    public class StarterKitModSystem : ModSystem
    {
        public static Config config;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide.IsServer();
        }

        public override void StartServerSide(ICoreServerAPI serverAPI)
        {
            TryToLoadConfig(serverAPI); // Load the initial config
            base.StartServerSide(serverAPI);
            StarterKitCommands.RegisterServerCommands(serverAPI);
            
        }

        private void TryToLoadConfig(ICoreAPI serverAPI)
        {
            try
            {
                config = serverAPI.LoadModConfig<Config>("StarterKitConfig.json");
                config ??= new Config();
                serverAPI.StoreModConfig<Config>(config, "StarterKitConfig.json");
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Could not load config! Loading default settings instead.");
                Mod.Logger.Error(e);
                config = new Config();
            }
        }

    }
}
