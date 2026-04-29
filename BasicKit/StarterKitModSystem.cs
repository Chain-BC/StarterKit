using StarterKit.Commands;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StarterKit
{
    public class StarterKitModSystem : ModSystem
    {
        #pragma warning disable CS8618
        public static StarterKitConfig Config;
        #pragma warning restore CS8618
        
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide.IsServer();
        }

        public override void StartServerSide(ICoreServerAPI serverAPI)
        {
            StarterKitConfig.TryToLoadConfig(serverAPI, ref Config); // Load the initial config
            base.StartServerSide(serverAPI);
            StarterKitCommands.RegisterServerCommands(serverAPI);

        }

    }
}
