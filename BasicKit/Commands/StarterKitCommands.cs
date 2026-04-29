using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StarterKit.Commands
{
    internal static class StarterKitCommands
    {

        //private static readonly string configFileName = "StarterKitConfig.json";

        public static void RegisterServerCommands(ICoreServerAPI serverAPI)
        {

            // Main command for giving the starting kit
            serverAPI.ChatCommands.Create("starterkit")
                .WithDescription("Provides the servers\'s starting kit")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithAlias("sk")
                .HandleWith(args => HandleStarterKitCommand(args, serverAPI))
                // Adds an item to the kit, or modifies an existing one
                .BeginSubCommand("add")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Adds an item to the kit or modifies an existing one")
                .WithArgs(new WordArgParser("item", true), new IntArgParser("amount", 1, false))
                .HandleWith(args => HandleStarterKitAddCommand(args, serverAPI))
                .EndSubCommand()
                // Removes an item from the kit
                .BeginSubCommand("remove")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Removes an item from the kit")
                .WithArgs(new WordArgParser("item", true))
                .HandleWith(args => HandleStarterKitRemoveCommand(args, serverAPI))
                .EndSubCommand()
                // List all items on the kit
                .BeginSubCommand("list")
                .RequiresPrivilege(Privilege.chat)
                .WithDescription("List items currently in the kit")
                .HandleWith(HandleStarterKitListCommand)
                .EndSubCommand()
                // Reload Config
                .BeginSubCommand("reload")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Reload the config file without restarting the server")
                .HandleWith(args => HandleStarterKitReloadCommand(args, serverAPI))
                .EndSubCommand()
                // Change Config
                .BeginSubCommand("modify_config")
                .WithAlias("mc")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Modify a value in config")
                .WithArgs(new WordArgParser("key", true), new WordArgParser("value", true))
                .HandleWith(args => HandleStarterKitModifyConfigCommand(args, serverAPI))
                .EndSubCommand();
         
        }

        /*
         *  "/starterkit" command, no arguments
         */
        private static TextCommandResult HandleStarterKitCommand(TextCommandCallingArgs args, ICoreServerAPI serverAPI)
        {
            var successMessage = "Kit has been provided";
            if (StarterKitModSystem.Config.requiresPrivilege && !args.Caller.Player.HasPrivilege(StarterKitModSystem.Config.privilege))
            {
                return TextCommandResult.Error("Not enough permissions!");
            }
            
            foreach (var itemStack in StarterKitModSystem.Config.kitItems)
            {
                Item? currentItem = serverAPI.World.GetItem(itemStack[0]);
                Block? currentBlock = serverAPI.World.GetBlock(itemStack[0]);
                ItemStack currentItemStack;
                if (currentItem != null) // If it's an item do this block of code
                {
                    currentItemStack = new(currentItem, int.Parse(itemStack[1]));
                }
                else if (currentBlock != null) // If it's a block, do this instead
                {
                    currentItemStack = new(currentBlock, int.Parse(itemStack[1]));
                }
                else // If the item was added manually via editing the config, there is a chance for both of those to fail
                {
                    return TextCommandResult.Error("One of the items has a wrong ID, please notify an administator!");
                }

                bool slotsFree = args.Caller.Player.InventoryManager.TryGiveItemstack(currentItemStack, true);
                if (!slotsFree)
                {
                    serverAPI.World.SpawnItemEntity(currentItemStack, args.Caller.Pos);
                }
            }
            
            return TextCommandResult.Success(successMessage);
        }

        /*
         *  "/starterkit add <item> [amount]" command, 2 arguments, a word and an integer
         */
        private static TextCommandResult HandleStarterKitAddCommand(TextCommandCallingArgs args, ICoreServerAPI serverAPI)
        {
            string successMessage;
            var itemString = (args[0] as string)!.ToLower();
            var itemAmount = (int)args[1];
            string[] itemArray = [itemString, itemAmount.ToString()];
            Item? item = serverAPI.World.GetItem(itemString);
            Block? block = serverAPI.World.GetBlock(itemString);

            // Validates given arguments
            if (item == null && block == null)
            {
                return TextCommandResult.Error("Invalid item/block name!");
            }
            if (itemAmount < 1)
            {
                return TextCommandResult.Error("Invalid amount!");
            }

            // The kit has unique items, but you can change the amount of each unique item that a person gets, this checks that
            int updateItemIndex = StarterKitModSystem.Config.kitItems.FindIndex(currItem => (currItem[0] == itemString && currItem[1] != itemAmount.ToString()));
            if (StarterKitModSystem.Config.kitItems.FindIndex(currItem => (currItem[0] == itemString && currItem[1] == itemAmount.ToString())) != -1) // Same item and item amount are in the list
            {
                return TextCommandResult.Error("Item already in the kit!");
            }
            else if ( updateItemIndex != -1) // Same item but not the same item amount are in the list
            {
                StarterKitModSystem.Config.kitItems[updateItemIndex] = itemArray;
                successMessage = "Item amount updated!";
            }
            else // Item is not in the list
            {
                StarterKitModSystem.Config.kitItems.Add(itemArray);
                successMessage = "Item successfully added!";
            }

            StarterKitConfig.StoreToFileConfig(serverAPI);
            return TextCommandResult.Success(successMessage);
        }

        /*
         *  "/starterkit remove <item>" command, 1 argument, a word
         */
        private static TextCommandResult HandleStarterKitRemoveCommand(TextCommandCallingArgs args, ICoreServerAPI serverAPI)
        {
            string successMessage = "Item successfully removed!";
            string itemString = (args[0] as string)!.ToLower();
            Item? item = serverAPI.World.GetItem(itemString);
            Block? block = serverAPI.World.GetBlock(itemString);

            // Validates given arguments
            if (item == null && block == null)
            {
                return TextCommandResult.Error("Invalid item/block name!");
            }

            int updateItemIndex = StarterKitModSystem.Config.kitItems.FindIndex(currItem => (currItem[0] == itemString));
            if (updateItemIndex != -1)
            {
                StarterKitModSystem.Config.kitItems.RemoveAt(updateItemIndex);
            }
            else
            {
                return TextCommandResult.Error("Item not in the kit!");
            }

            StarterKitConfig.StoreToFileConfig(serverAPI);
            return TextCommandResult.Success(successMessage);
        }

        /*
         *  "/starterkit list" command, no arguments
         */
        private static TextCommandResult HandleStarterKitListCommand(TextCommandCallingArgs _)
        {
            string successMessage = "Items currently in the kit:\n";
            foreach (var item in StarterKitModSystem.Config.kitItems)
            {
                successMessage += "  " + item[0] + " - " + item[1] + "\n";
            }
            return TextCommandResult.Success(successMessage);
        }

        /*
         *  "/starterkit reload" command, no arguments
         */
        private static TextCommandResult HandleStarterKitReloadCommand(TextCommandCallingArgs _, ICoreServerAPI serverAPI)
        {
            bool success = StarterKitConfig.LoadFromFileConfig(serverAPI);
            if (!success)
            {
                return TextCommandResult.Error("Something went wrong when trying to reload the configuration!");
            }
            return TextCommandResult.Success("Configuration successfully reloaded");
        }

        /*
         *  "/starterkit modify_config <key> <value>" command, 2 arguments, both words
         */
        private static TextCommandResult HandleStarterKitModifyConfigCommand(TextCommandCallingArgs args, ICoreServerAPI serverAPI)
        {
            string successMessage = "Configuration value successfully changed";
            string key = (args[0] as string)!.ToLower();
            string value = (args[1] as string)!;

            switch (key)
            {
                case "requiresPrivilege":
                    bool validValue = bool.TryParse(value, out bool processedValue);
                    if (!validValue)
                    {
                        return TextCommandResult.Error("Invalid boolean value");
                    }
                    StarterKitModSystem.Config.requiresPrivilege = processedValue;
                    StarterKitConfig.StoreToFileConfig(serverAPI);
                    break;

                case "privilege":
                    StarterKitModSystem.Config.privilege = value;
                    StarterKitConfig.StoreToFileConfig(serverAPI);
                    break;

                default:
                    successMessage = "Valid keys are: requiresPrivilege, privilege, removePrivilegeOnKitUse";
                    break;
            }
            return TextCommandResult.Success(successMessage);
        }
    }
}
