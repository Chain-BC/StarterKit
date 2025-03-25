using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StarterKit.Commands
{
    internal static class StarterKitCommands
    {

        private static readonly string configFileName = "StarterKitConfig.json";

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
                .HandleWith(args => HandleStarterKitListCommand(args))
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
            string successMessage;
            if (StarterKitModSystem.config.requiresPrivilege && !args.Caller.Player.HasPrivilege(StarterKitModSystem.config.privilege))
            {
                return TextCommandResult.Error("Not enough permissions!");
            }

            Item currentItem;
            bool slotsFree;
            for (int i = 0; i < StarterKitModSystem.config.kitItems.Count; i++)
            {
                currentItem = serverAPI.World.GetItem(StarterKitModSystem.config.kitItems[i][0]);
                ItemStack currentItemStack = new(currentItem, int.Parse(StarterKitModSystem.config.kitItems[i][1]));
                slotsFree = args.Caller.Player.InventoryManager.TryGiveItemstack(currentItemStack, true);
                if (!slotsFree)
                {
                    serverAPI.World.SpawnItemEntity(currentItemStack, args.Caller.Pos);
                }
            }

            if (StarterKitModSystem.config.removePrivilegeOnKitUse && StarterKitModSystem.config.requiresPrivilege)
            {
                serverAPI.Permissions.DenyPrivilege(args.Caller.Player.PlayerUID, StarterKitModSystem.config.privilege);
                successMessage = "Kit has been provided, privilege revoked";
            }
            else
            {
                successMessage = "Kit has been provided";
            }
            return TextCommandResult.Success(successMessage);
        }

        /*
         *  "/starterkit add <item> [amount]" command, 2 arguments, a word and an integer
         */
        private static TextCommandResult HandleStarterKitAddCommand(TextCommandCallingArgs args, ICoreServerAPI serverAPI)
        {
            string successMessage = "";
            string itemString = (args[0] as string).ToLower();
            int itemAmount = (int)args[1];
            string[] itemArray = new string[] { itemString, itemAmount.ToString()};
            Item item = serverAPI.World.GetItem(itemString);
            int updateItemIndex = -1;

            // Validates given arguments
            if (item == null)
            {
                return TextCommandResult.Error("Invalid item name!");
            }
            if (itemAmount < 1)
            {
                return TextCommandResult.Error("Invalid amount!");
            }

            // The kit has unique items, but you can change the amount of each unique item that a person gets, this checks that
            updateItemIndex = StarterKitModSystem.config.kitItems.FindIndex(currItem => (currItem[0] == itemString && currItem[1] != itemAmount.ToString()));
            if (StarterKitModSystem.config.kitItems.FindIndex(currItem => (currItem[0] == itemString && currItem[1] == itemAmount.ToString())) != -1)
            {
                return TextCommandResult.Error("Item already in the kit!");
            }
            else if ( updateItemIndex != -1)
            {
                StarterKitModSystem.config.kitItems[updateItemIndex] = itemArray;
                successMessage = "Item amount updated!";
            }
            else
            {
                StarterKitModSystem.config.kitItems.Add(itemArray);
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
            string successMessage = "";
            int updateItemIndex = -1;
            string itemString = (args[0] as string).ToLower();
            Item item = serverAPI.World.GetItem(itemString);

            // Validates given arguments
            if (item == null)
            {
                return TextCommandResult.Error("Invalid item name!");
            }

            updateItemIndex = StarterKitModSystem.config.kitItems.FindIndex(currItem => currItem[0] == itemString);
            if (updateItemIndex != -1)
            {
                StarterKitModSystem.config.kitItems.RemoveAt(updateItemIndex);
                successMessage = "Item successfully removed!";
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
        private static TextCommandResult HandleStarterKitListCommand(TextCommandCallingArgs args)
        {
            string successMessage = "Items currently in the kit:\n";
            for (int i = 0; i < StarterKitModSystem.config.kitItems.Count; i++)
            {
                successMessage += "  " + StarterKitModSystem.config.kitItems[i][0] + " - " + StarterKitModSystem.config.kitItems[i][1].ToString() + "\n";
            }
            return TextCommandResult.Success(successMessage);
        }

        /*
         *  "/starterkit reload" command, no arguments
         */
        private static TextCommandResult HandleStarterKitReloadCommand(TextCommandCallingArgs args, ICoreServerAPI serverAPI)
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
            string key = (args[0] as string).ToLower();
            string value = args[1] as string;
            bool processedValue;
            bool validValue;

            switch (key)
            {
                case "requiresprivilege":
                    validValue = bool.TryParse(value, out processedValue);
                    if (!validValue)
                    {
                        return TextCommandResult.Error("Invalid boolean value");
                    }
                    StarterKitModSystem.config.requiresPrivilege = processedValue;
                    StarterKitConfig.StoreToFileConfig(serverAPI);
                    break;

                case "privilege":
                    StarterKitModSystem.config.privilege = value;
                    StarterKitConfig.StoreToFileConfig(serverAPI);
                    break;

                case "removeprivilegeonkituse":
                    validValue = bool.TryParse(value, out processedValue);
                    if (!validValue)
                    {
                        return TextCommandResult.Error("Invalid boolean value");
                    }
                    StarterKitModSystem.config.removePrivilegeOnKitUse = processedValue;
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
