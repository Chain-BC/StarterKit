using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace StarterKit.Commands;
public class StarterKitCommands(ICoreServerAPI serverAPI, ModData data)
{
    public void RegisterServerCommands()
    {
        // Main command for giving the starting kit
        serverAPI.ChatCommands.Create("starterkit")
            .WithDescription("Provides the servers's starting kit")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(HandleStarterKitCommand)
            // Gets a player's current uses from the PlayerData stored in the server
            .BeginSubCommand("uses")
            .RequiresPrivilege(Privilege.chat)
            .WithDescription("Shows yours or a different player's kit uses")
            .WithArgs(new WordArgParser("player", false))
            .HandleWith(HandleStarterKitUsesCommand)
            .EndSubCommand()
            // Sets a player's current uses
            .BeginSubCommand("set-uses")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Sets a player's kit uses")
            .WithArgs(new WordArgParser("player", true), new IntArgParser("amount", 1, true))
            .HandleWith(HandleStarterKitUsesSetCommand)
            .EndSubCommand()
            // Resets all the players kit uses
            .BeginSubCommand("reset-uses")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Resets all the players kit uses to the max set in config")
            .HandleWith(HandleStarterKitUsesResetCommand)
            .EndSubCommand()
            // Adds an item to the kit, or modifies an existing one
            .BeginSubCommand("add")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Adds an item to the kit or modifies an existing one")
            .WithArgs(new WordArgParser("item", true), new IntArgParser("amount", 1, false))
            .HandleWith(HandleStarterKitAddCommand)
            .EndSubCommand()
            // Removes an item from the kit
            .BeginSubCommand("remove")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Removes an item from the kit")
            .WithArgs(new WordArgParser("item", true))
            .HandleWith(HandleStarterKitRemoveCommand)
            .EndSubCommand()
            // Removes all items from the kit
            .BeginSubCommand("clear")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Clears all items from the kit")
            .HandleWith(HandleStarterKitClearCommand)
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
            .WithDescription("Reload the config file without restarting the server (only necessary if manually editing config, which is not recommended)")
            .HandleWith(HandleStarterKitReloadCommand)
            .EndSubCommand()
            // Change Config
            .BeginSubCommand("configure")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Modify a value in config")
            .WithArgs(new WordArgParser("key", true), new WordArgParser("value", true))
            .HandleWith(HandleStarterKitModifyConfigCommand)
            .EndSubCommand();
     
    }

    /*
     *  "/starterkit" command, no arguments
     */
    private TextCommandResult HandleStarterKitCommand(TextCommandCallingArgs args)
    {
        if (data.Config.requiresPrivilege && !args.Caller.Player.HasPrivilege(data.Config.privilege))
        {
            return TextCommandResult.Error("Not enough permissions.");
        }

        int playerIndex = data.Players.FindIndex(currPlayer => currPlayer.UID == args.Caller.Player.PlayerUID);

        if (data.Players[playerIndex].UsesLeft <= 0 || data.Config.maxKitUses <= 0)
        {
            return TextCommandResult.Error("No kit uses left.");
        }
        
        foreach (var itemStack in data.Config.kitItems)
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
                return TextCommandResult.Error("One of the items has a wrong ID, please notify an administrator.");
            }

            bool slotsFree = args.Caller.Player.InventoryManager.TryGiveItemstack(currentItemStack, true);
            if (!slotsFree)
            {
                serverAPI.World.SpawnItemEntity(currentItemStack, args.Caller.Pos);
            }
        }
        
        data.Players[playerIndex].UsesLeft -= 1;
        
        return TextCommandResult.Success("Kit has been provided!");
    }
    /*
     * "/starterkit uses [player]" command, 1 argument, an optional word
     */
    private TextCommandResult HandleStarterKitUsesCommand(TextCommandCallingArgs args)
    {
        string successMessage;
        string? playerName = args[0] as string;
        int playerIndex;
        if (playerName == null)
        {
            playerIndex = data.Players.FindIndex(currPlayer => currPlayer.UID == args.Caller.Player.PlayerUID);
            successMessage = "You have " + data.Players[playerIndex].UsesLeft + " uses left!";
        }
        else
        {
            if (!args.Caller.HasPrivilege(Privilege.controlserver))
            {
                return TextCommandResult.Error("Not enough permissions!");
            }
            
            IPlayer? vsPlayer = Array.Find(serverAPI.World.AllPlayers,currPlayer => currPlayer.PlayerName == playerName);
            if (vsPlayer?.PlayerName != playerName)
            {
                return TextCommandResult.Error("Player not found.");
            }
            
            playerIndex = data.Players.FindIndex(currPlayer => currPlayer.UID == vsPlayer.PlayerUID);
            successMessage = playerName + " has " + data.Players[playerIndex].UsesLeft + " uses left!";
        }

        return TextCommandResult.Success(successMessage);
    }
    
    /*
     * "/starterkit uses-set <player> <amount>" command, 2 arguments, a word and an integer
     */
    private TextCommandResult HandleStarterKitUsesSetCommand(TextCommandCallingArgs args)
    {
        string? playerName = args[0] as string;
        int numUses = (int)args[1];
        
        IPlayer? vsPlayer = Array.Find(serverAPI.World.AllPlayers, currPlayer => currPlayer.PlayerName == playerName);
        if (vsPlayer?.PlayerName != playerName)
        {
            return TextCommandResult.Error("Player not found.");
        }
            
        int playerIndex = data.Players.FindIndex(currPlayer => currPlayer.UID == vsPlayer?.PlayerUID);
        data.Players[playerIndex].UsesLeft = numUses;

        return TextCommandResult.Success("Successfully changed " + vsPlayer?.PlayerName + "'s uses!");
    }
    
    /*
     * "/starterkit uses-reset" command, no arguments
     */
    private TextCommandResult HandleStarterKitUsesResetCommand(TextCommandCallingArgs _)
    {
        foreach (var player in data.Players)
        {
            player.UsesLeft = data.Config.maxKitUses;
        }
        return TextCommandResult.Success("Reset uses for every player!");
    }

    /*
     *  "/starterkit add <item> [amount]" command, 2 arguments, a word and an integer
     */
    private TextCommandResult HandleStarterKitAddCommand(TextCommandCallingArgs args)
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
            return TextCommandResult.Error("Invalid item/block name.");
        }
        if (itemAmount < 1)
        {
            return TextCommandResult.Error("Invalid amount.");
        }

        // The kit has unique items, but you can change the amount of each unique item that a person gets, this checks that
        int updateItemIndex = data.Config.kitItems.FindIndex(currItem => (currItem[0] == itemString && currItem[1] != itemAmount.ToString()));
        if (data.Config.kitItems.FindIndex(currItem => (currItem[0] == itemString && currItem[1] == itemAmount.ToString())) != -1) // Same item and item amount are in the list
        {
            return TextCommandResult.Error("Item already in the kit.");
        }
        else if ( updateItemIndex != -1) // Same item but not the same item amount are in the list
        {
            data.Config.kitItems[updateItemIndex] = itemArray;
            successMessage = "Item amount updated!";
        }
        else // Item is not in the list
        {
            data.Config.kitItems.Add(itemArray);
            successMessage = "Item successfully added!";
        }

        StarterKitConfig.StoreToFileConfig(serverAPI, ref data.Config);
        return TextCommandResult.Success(successMessage);
    }

    /*
     *  "/starterkit remove <item>" command, 1 argument, a word
     */
    private TextCommandResult HandleStarterKitRemoveCommand(TextCommandCallingArgs args)
    {
        string itemString = (args[0] as string)!.ToLower();
        Item? item = serverAPI.World.GetItem(itemString);
        Block? block = serverAPI.World.GetBlock(itemString);

        // Validates given arguments
        if (item == null && block == null)
        {
            return TextCommandResult.Error("Invalid item/block name.");
        }

        int updateItemIndex = data.Config.kitItems.FindIndex(currItem => (currItem[0] == itemString));
        if (updateItemIndex != -1)
        {
            data.Config.kitItems.RemoveAt(updateItemIndex);
        }
        else
        {
            return TextCommandResult.Error("Item not in the kit!");
        }

        StarterKitConfig.StoreToFileConfig(serverAPI, ref data.Config);
        return TextCommandResult.Success("Item successfully removed!");
    }
    /*
     * "/starterkit clear" command, no arguments
     */
    private TextCommandResult HandleStarterKitClearCommand(TextCommandCallingArgs args)
    {
        if (data.Config.kitItems.Count == 0)
        {
            return TextCommandResult.Error("Kit already empty.");
        }
        data.Config.kitItems.Clear();
        StarterKitConfig.StoreToFileConfig(serverAPI, ref data.Config);
        return TextCommandResult.Success("Kit successfully cleared!");
    }

    /*
     *  "/starterkit list" command, no arguments
     */
    private TextCommandResult HandleStarterKitListCommand(TextCommandCallingArgs args)
    {
        var successMessage = "Items currently in the kit:\n";
        foreach (var item in data.Config.kitItems)
        {
            successMessage += "  " + item[0] + " (" + item[1] + ")" + "\n";
        }
        return TextCommandResult.Success(successMessage);
    }

    /*
     *  "/starterkit reload" command, no arguments
     */
    private TextCommandResult HandleStarterKitReloadCommand(TextCommandCallingArgs args)
    {
        bool success = StarterKitConfig.LoadFromFileConfig(serverAPI, ref data.Config);
        if (!success)
        {
            return TextCommandResult.Error("Something went wrong when trying to reload the configuration.");
        }
        return TextCommandResult.Success("Configuration successfully reloaded!");
    }

    /*
     *  "/starterkit modify_config <key> <value>" command, 2 arguments, both words
     */
    private TextCommandResult HandleStarterKitModifyConfigCommand(TextCommandCallingArgs args)
    {
        var successMessage = "Configuration value successfully changed!";
        string key = (args[0] as string)!;
        string value = (args[1] as string)!;

        switch (key)
        {
            case "requiresPrivilege":
                bool validValue = bool.TryParse(value, out bool processedValue);
                if (!validValue)
                {
                    return TextCommandResult.Error("Invalid boolean value.");
                }
                data.Config.requiresPrivilege = processedValue;
                StarterKitConfig.StoreToFileConfig(serverAPI, ref data.Config);
                break;

            case "privilege":
                data.Config.privilege = value;
                StarterKitConfig.StoreToFileConfig(serverAPI, ref data.Config);
                break;
            
            case "maxKitUses":
                data.Config.maxKitUses = int.Parse(value);
                StarterKitConfig.StoreToFileConfig(serverAPI, ref data.Config);
                break;

            default:
                successMessage = "Valid keys are: requiresPrivilege, privilege, maxKitUses.";
                break;
        }
        return TextCommandResult.Success(successMessage);
    }
}
