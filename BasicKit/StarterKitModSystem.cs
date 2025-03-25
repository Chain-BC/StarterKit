using StarterKit.Commands;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StarterKit
{
    public class StarterKitModSystem : ModSystem
    {
        public static StarterKitConfig config;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide.IsServer();
        }

        public override void StartServerSide(ICoreServerAPI serverAPI)
        {
            StarterKitConfig.TryToLoadConfig(serverAPI); // Load the initial config
            base.StartServerSide(serverAPI);
            StarterKitCommands.RegisterServerCommands(serverAPI);

        }

    }
}
