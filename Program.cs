using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

class ProxyManager
{
    private const string ProxyRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    // Windows API to notify system
    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;

    static void Main(string[] args)
    {
        // Argument mode
        if (args.Length > 0)
        {
            HandleArguments(args);
            return;
        }

        // Interactive mode
        InteractiveMode();
    }

    static void HandleArguments(string[] args)
    {
        string command = args[0].ToLower();

        switch (command)
        {
            case "check":
            case "status":
                CheckProxyStatus();
                break;

            case "get":
            case "show":
                GetProxySettings();
                break;

            case "set":
                if (args.Length < 2)
                {
                    PrintError("❌ Format: dotnet run set <proxy_server> [bypass_list]");
                    PrintInfo("💡 Example: dotnet run set 127.0.0.1:8080");
                    PrintInfo("💡 Example: dotnet run set 127.0.0.1:8080 \"<local>;*.example.com\"");
                    return;
                }
                string proxyServer = args[1];
                string? bypassList = args.Length > 2 ? args[2] : null;
                SetProxyWithArgs(proxyServer, bypassList);
                break;

            case "disable":
            case "off":
                DisableProxy();
                break;

            case "bypass":
                if (args.Length < 2)
                {
                    PrintError("❌ Format: dotnet run bypass <action> [value]");
                    PrintInfo("💡 Actions: set, add, remove, clear, show");
                    PrintInfo("💡 Example: dotnet run bypass set \"<local>;*.internal.com\"");
                    PrintInfo("💡 Example: dotnet run bypass add \"*.example.com\"");
                    PrintInfo("💡 Example: dotnet run bypass remove \"*.example.com\"");
                    PrintInfo("💡 Example: dotnet run bypass clear");
                    PrintInfo("💡 Example: dotnet run bypass show");
                    return;
                }
                string bypassAction = args[1].ToLower();
                string? bypassValue = args.Length > 2 ? args[2] : null;
                HandleBypassCommand(bypassAction, bypassValue);
                break;

            case "help":
            case "-h":
            case "--help":
                ShowHelp();
                break;

            default:
                PrintError($"❌ Unknown command: {command}");
                ShowHelp();
                break;
        }
    }

    static void ShowHelp()
    {
        PrintHeader("🔧 Windows Proxy Manager - Help");
        Console.WriteLine();
        PrintSuccess("Interactive Mode (no arguments):");
        Console.WriteLine("  dotnet run");
        Console.WriteLine();
        PrintSuccess("Command Line Mode:");
        Console.WriteLine("  dotnet run check              🔍 Check proxy status");
        Console.WriteLine("  dotnet run get                📋 Show proxy settings");
        Console.WriteLine("  dotnet run set <server>       ⚙️  Set proxy server");
        Console.WriteLine("  dotnet run set <server> <bypass>  Set proxy with bypass list");
        Console.WriteLine("  dotnet run disable            ⛔ Disable proxy");
        Console.WriteLine("  dotnet run bypass <action>    🚫 Manage bypass list");
        Console.WriteLine("  dotnet run help               📖 Show help");
        Console.WriteLine();
        PrintSuccess("Bypass List Actions:");
        Console.WriteLine("  dotnet run bypass show        📋 Show current bypass list");
        Console.WriteLine("  dotnet run bypass set <list>  ⚙️  Set/replace bypass list");
        Console.WriteLine("  dotnet run bypass add <item>  ➕ Add item to bypass list");
        Console.WriteLine("  dotnet run bypass remove <item> ➖ Remove item from bypass list");
        Console.WriteLine("  dotnet run bypass clear       🗑️  Clear all bypass list");
        Console.WriteLine();
        PrintInfo("💡 Examples:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  dotnet run set 127.0.0.1:8080");
        Console.WriteLine("  dotnet run set 192.168.1.100:3128 \"<local>;*.internal.com\"");
        Console.WriteLine("  dotnet run check");
        Console.WriteLine("  dotnet run disable");
        Console.WriteLine("  dotnet run bypass add \"*.example.com\"");
        Console.WriteLine("  dotnet run bypass remove \"<local>\"");
        Console.ResetColor();
    }

    static void InteractiveMode()
    {
        PrintHeader("🌐 Windows Proxy Manager");
        
        while (true)
        {
            Console.WriteLine();
            PrintSuccess("╔══════════════════════════════╗");
            PrintSuccess("║     Choose Operation:        ║");
            PrintSuccess("╠══════════════════════════════╣");
            Console.WriteLine("║  1⃣  🦜 Check Proxy Status      ║");
            Console.WriteLine("║  2⃣  🐧 Get Proxy Settings      ║");
            Console.WriteLine("║  3⃣  🐞 Set Proxy               ║");
            Console.WriteLine("║  4⃣  🐨 Disable Proxy           ║");
            Console.WriteLine("║  5⃣  👘 Manage Bypass List      ║");
            Console.WriteLine("║  6⃣  ❌ Exit                    ║");
            PrintSuccess("╚══════════════════════════════╝");
            Console.Write("\n👉 Your choice (1-6): ");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CheckProxyStatus();
                    break;
                case "2":
                    GetProxySettings();
                    break;
                case "3":
                    SetProxyInteractive();
                    break;
                case "4":
                    DisableProxy();
                    break;
                case "5":
                    ManageBypassListInteractive();
                    break;
                case "6":
                    PrintSuccess("\n👋 Thank you for using Proxy Manager!");
                    return;
                default:
                    PrintError("❌ Invalid choice!");
                    break;
            }
        }
    }

    static void CheckProxyStatus()
    {
        try
        {
            Console.WriteLine();
            PrintHeader("🔍 Checking Proxy Status...");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            object? proxyEnable = key.GetValue("ProxyEnable");
            object? proxyServer = key.GetValue("ProxyServer");
            object? proxyOverride = key.GetValue("ProxyOverride");

            bool isEnabled = proxyEnable != null && (int)proxyEnable == 1;

            Console.WriteLine();
            if (isEnabled)
            {
                PrintSuccess("╔════════════════════════════════════════════════╗");
                PrintSuccess("║   ✅ PROXY STATUS: ACTIVE                      ║");
                PrintSuccess("╚════════════════════════════════════════════════╝");
                
                // Show active proxy configuration
                if (proxyServer != null && !string.IsNullOrWhiteSpace(proxyServer.ToString()))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("📝 Active Proxy Configuration:");
                    Console.WriteLine($"   🌐 Server      : {proxyServer}");
                    Console.WriteLine($"   🚫 Bypass List : {proxyOverride ?? "(not set)"}");
                    Console.ResetColor();
                }
            }
            else
            {
                PrintWarning("╔════════════════════════════════════════════════╗");
                PrintWarning("║   ⛔ PROXY STATUS: INACTIVE                    ║");
                PrintWarning("╚════════════════════════════════════════════════╝");
                
                // Show configured proxy even if disabled
                if (proxyServer != null && !string.IsNullOrWhiteSpace(proxyServer.ToString()))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("📝 Configured Proxy (currently disabled):");
                    Console.WriteLine($"   🌐 Server      : {proxyServer}");
                    Console.WriteLine($"   🚫 Bypass List : {proxyOverride ?? "(not set)"}");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void GetProxySettings()
    {
        try
        {
            Console.WriteLine();
            PrintHeader("📋 Proxy Settings");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            object? proxyEnable = key.GetValue("ProxyEnable");
            object? proxyServer = key.GetValue("ProxyServer");
            object? proxyOverride = key.GetValue("ProxyOverride");

            bool isEnabled = proxyEnable != null && (int)proxyEnable == 1;

            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            
            if (isEnabled)
            {
                PrintSuccess($"║  📊 Status      : ✅ Active");
            }
            else
            {
                PrintWarning($"║  📊 Status      : ⛔ Inactive");
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"║  🌐 Server      : {proxyServer ?? "(not set)"}");
            Console.WriteLine($"║  🚫 Bypass List : {proxyOverride ?? "(not set)"}");
            Console.ResetColor();
            Console.WriteLine("╚════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void SetProxyInteractive()
    {
        Console.WriteLine();
        PrintHeader("⚙️  Set Proxy Configuration");
        
        Console.Write("\n🌐 Enter proxy server (example: 127.0.0.1:8080): ");
        string? proxyServer = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(proxyServer))
        {
            PrintError("❌ Proxy server cannot be empty!");
            return;
        }

        Console.Write("🚫 Bypass list (separate with ; or Enter for default): ");
        string? bypassList = Console.ReadLine();

        SetProxyCore(proxyServer, bypassList);
    }

    static void SetProxyWithArgs(string proxyServer, string? bypassList)
    {
        Console.WriteLine();
        PrintHeader("⚙️  Setting Proxy...");
        SetProxyCore(proxyServer, bypassList);
    }

    static void SetProxyCore(string proxyServer, string? bypassList)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            // Set proxy server
            key.SetValue("ProxyServer", proxyServer);
            
            // Set bypass list
            if (!string.IsNullOrWhiteSpace(bypassList))
            {
                key.SetValue("ProxyOverride", bypassList);
            }
            else
            {
                // Set default bypass for localhost
                key.SetValue("ProxyOverride", "<local>");
            }
            
            // Enable proxy
            key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);

            Console.WriteLine();
            PrintSuccess("╔════════════════════════════════════════════════╗");
            PrintSuccess("║  ✅ Proxy Successfully Activated!              ║");
            PrintSuccess("╚════════════════════════════════════════════════╝");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   🌐 Server : {proxyServer}");
            if (!string.IsNullOrWhiteSpace(bypassList))
            {
                Console.WriteLine($"   🚫 Bypass : {bypassList}");
            }
            else
            {
                Console.WriteLine($"   🚫 Bypass : <local> (default)");
            }
            Console.ResetColor();

            // Notify system
            Console.WriteLine();
            PrintInfo("⏳ Notifying system...");
            if (NotifyProxyChange())
            {
                PrintSuccess("✅ System successfully notified - Proxy is now active! 🚀");
            }
            else
            {
                PrintWarning("⚠️  Failed to notify system - May need to restart applications");
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void DisableProxy()
    {
        try
        {
            Console.WriteLine();
            PrintHeader("⛔ Disabling Proxy...");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            // Get current proxy before disabling
            object? proxyServer = key.GetValue("ProxyServer");
            object? proxyOverride = key.GetValue("ProxyOverride");

            // Disable proxy
            key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);

            Console.WriteLine();
            PrintSuccess("╔════════════════════════════════════════════════╗");
            PrintSuccess("║  ✅ Proxy Successfully Disabled!               ║");
            PrintSuccess("╚════════════════════════════════════════════════╝");

            // Show what was disabled
            if (proxyServer != null && !string.IsNullOrWhiteSpace(proxyServer.ToString()))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("📝 Disabled proxy configuration:");
                Console.WriteLine($"   🌐 Server : {proxyServer}");
                Console.WriteLine($"   🚫 Bypass : {proxyOverride ?? "(not set)"}");
                Console.ResetColor();
            }

            // Notify system
            Console.WriteLine();
            PrintInfo("⏳ Notifying system...");
            if (NotifyProxyChange())
            {
                PrintSuccess("✅ System successfully notified - Changes are now active! 🚀");
            }
            else
            {
                PrintWarning("⚠️  Failed to notify system - May need to restart applications");
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static bool NotifyProxyChange()
    {
        try
        {
            // INTERNET_OPTION_SETTINGS_CHANGED: notify that settings changed
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            
            // INTERNET_OPTION_REFRESH: refresh settings
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Notify error: {ex.Message}");
            return false;
        }
    }

    static void HandleBypassCommand(string action, string? value)
    {
        switch (action)
        {
            case "show":
                ShowBypassList();
                break;
            case "set":
                if (string.IsNullOrWhiteSpace(value))
                {
                    PrintError("❌ Value required for 'set' action");
                    PrintInfo("💡 Example: dotnet run bypass set \"<local>;*.internal.com\"");
                    return;
                }
                SetBypassList(value);
                break;
            case "add":
                if (string.IsNullOrWhiteSpace(value))
                {
                    PrintError("❌ Value required for 'add' action");
                    PrintInfo("💡 Example: dotnet run bypass add \"*.example.com\"");
                    return;
                }
                AddToBypassList(value);
                break;
            case "remove":
            case "delete":
                if (string.IsNullOrWhiteSpace(value))
                {
                    PrintError("❌ Value required for 'remove' action");
                    PrintInfo("💡 Example: dotnet run bypass remove \"*.example.com\"");
                    return;
                }
                RemoveFromBypassList(value);
                break;
            case "clear":
                ClearBypassList();
                break;
            default:
                PrintError($"❌ Unknown action: {action}");
                PrintInfo("💡 Available actions: show, set, add, remove, clear");
                break;
        }
    }

    static void ManageBypassListInteractive()
    {
        Console.WriteLine();
        PrintHeader("🚫 Manage Bypass List");
        
        while (true)
        {
            Console.WriteLine();
            PrintSuccess("╔══════════════════════════════╗");
            PrintSuccess("║   Bypass List Operations:   ║");
            PrintSuccess("╠══════════════════════════════╣");
            Console.WriteLine("║  1️⃣  Show Bypass List        ║");
            Console.WriteLine("║  2️⃣  Set/Replace Bypass List ║");
            Console.WriteLine("║  3️⃣  Add Item to List        ║");
            Console.WriteLine("║  4️⃣  Remove Item from List   ║");
            Console.WriteLine("║  5️⃣  Clear All List          ║");
            Console.WriteLine("║  6️⃣  Back to Main Menu       ║");
            PrintSuccess("╚══════════════════════════════╝");
            Console.Write("\n👉 Your choice (1-6): ");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ShowBypassList();
                    break;
                case "2":
                    Console.Write("\n📝 Enter bypass list (separate with ;): ");
                    string? newList = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newList))
                    {
                        SetBypassList(newList);
                    }
                    else
                    {
                        PrintError("❌ Bypass list cannot be empty!");
                    }
                    break;
                case "3":
                    Console.Write("\n➕ Enter item to add: ");
                    string? addItem = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(addItem))
                    {
                        AddToBypassList(addItem);
                    }
                    else
                    {
                        PrintError("❌ Item cannot be empty!");
                    }
                    break;
                case "4":
                    Console.Write("\n➖ Enter item to remove: ");
                    string? removeItem = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(removeItem))
                    {
                        RemoveFromBypassList(removeItem);
                    }
                    else
                    {
                        PrintError("❌ Item cannot be empty!");
                    }
                    break;
                case "5":
                    Console.Write("\n⚠️  Are you sure you want to clear all bypass list? (y/n): ");
                    string? confirm = Console.ReadLine();
                    if (confirm?.ToLower() == "y" || confirm?.ToLower() == "yes")
                    {
                        ClearBypassList();
                    }
                    else
                    {
                        PrintInfo("ℹ️  Operation cancelled");
                    }
                    break;
                case "6":
                    return;
                default:
                    PrintError("❌ Invalid choice!");
                    break;
            }
        }
    }

    static void ShowBypassList()
    {
        try
        {
            Console.WriteLine();
            PrintHeader("📋 Current Bypass List");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            object? proxyOverride = key.GetValue("ProxyOverride");

            Console.WriteLine();
            if (proxyOverride == null || string.IsNullOrWhiteSpace(proxyOverride.ToString()))
            {
                PrintWarning("⚠️  No bypass list configured");
                return;
            }

            string bypassList = proxyOverride.ToString()!;
            string[] items = bypassList.Split(';');

            // Console.WriteLine("╔════════════════════════════════════════════════╗");
            // PrintSuccess($"║  Total Items: {items.Length}");
            PrintSuccess($"Total Items: {items.Length}");
            // Console.WriteLine("╠════════════════════════════════════════════════╣");
            
            for (int i = 0; i < items.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"║  {i + 1}. {items[i]}");
                Console.ResetColor();
            }
            
            // Console.WriteLine("╚════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void SetBypassList(string bypassList)
    {
        try
        {
            Console.WriteLine();
            PrintHeader("⚙️  Setting Bypass List...");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            key.SetValue("ProxyOverride", bypassList);

            Console.WriteLine();
            PrintSuccess("╔════════════════════════════════════════════════╗");
            PrintSuccess("║  ✅ Bypass List Successfully Set!              ║");
            PrintSuccess("╚════════════════════════════════════════════════╝");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   🚫 New Bypass List: {bypassList}");
            Console.ResetColor();

            // Notify system
            Console.WriteLine();
            PrintInfo("⏳ Notifying system...");
            if (NotifyProxyChange())
            {
                PrintSuccess("✅ System successfully notified - Changes are now active! 🚀");
            }
            else
            {
                PrintWarning("⚠️  Failed to notify system - May need to restart applications");
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void AddToBypassList(string item)
    {
        try
        {
            Console.WriteLine();
            PrintHeader("➕ Adding to Bypass List...");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            object? proxyOverride = key.GetValue("ProxyOverride");
            string currentList = proxyOverride?.ToString() ?? "";

            // Check if item already exists
            string[] items = currentList.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (items.Any(i => i.Trim().Equals(item.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                PrintWarning($"⚠️  Item '{item}' already exists in bypass list");
                return;
            }

            // Add new item
            string newList = string.IsNullOrWhiteSpace(currentList) 
                ? item 
                : $"{currentList};{item}";

            key.SetValue("ProxyOverride", newList);

            Console.WriteLine();
            PrintSuccess("╔════════════════════════════════════════════════╗");
            PrintSuccess("║  ✅ Item Successfully Added!                   ║");
            PrintSuccess("╚════════════════════════════════════════════════╝");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   ➕ Added: {item}");
            Console.WriteLine($"   🚫 New Bypass List: {newList}");
            Console.ResetColor();

            // Notify system
            Console.WriteLine();
            PrintInfo("⏳ Notifying system...");
            if (NotifyProxyChange())
            {
                PrintSuccess("✅ System successfully notified - Changes are now active! 🚀");
            }
            else
            {
                PrintWarning("⚠️  Failed to notify system - May need to restart applications");
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void RemoveFromBypassList(string item)
    {
        try
        {
            Console.WriteLine();
            PrintHeader("➖ Removing from Bypass List...");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            object? proxyOverride = key.GetValue("ProxyOverride");
            
            if (proxyOverride == null || string.IsNullOrWhiteSpace(proxyOverride.ToString()))
            {
                PrintWarning("⚠️  Bypass list is empty, nothing to remove");
                return;
            }

            string currentList = proxyOverride.ToString()!;
            string[] items = currentList.Split(';', StringSplitOptions.RemoveEmptyEntries);

            // Find and remove item (case insensitive)
            var remainingItems = items.Where(i => !i.Trim().Equals(item.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();

            if (remainingItems.Length == items.Length)
            {
                PrintWarning($"⚠️  Item '{item}' not found in bypass list");
                return;
            }

            string newList = string.Join(";", remainingItems);
            
            if (string.IsNullOrWhiteSpace(newList))
            {
                key.DeleteValue("ProxyOverride", false);
            }
            else
            {
                key.SetValue("ProxyOverride", newList);
            }

            Console.WriteLine();
            PrintSuccess("╔════════════════════════════════════════════════╗");
            PrintSuccess("║  ✅ Item Successfully Removed!                 ║");
            PrintSuccess("╚════════════════════════════════════════════════╝");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   ➖ Removed: {item}");
            if (string.IsNullOrWhiteSpace(newList))
            {
                Console.WriteLine($"   🚫 Bypass List: (empty)");
            }
            else
            {
                Console.WriteLine($"   🚫 New Bypass List: {newList}");
            }
            Console.ResetColor();

            // Notify system
            Console.WriteLine();
            PrintInfo("⏳ Notifying system...");
            if (NotifyProxyChange())
            {
                PrintSuccess("✅ System successfully notified - Changes are now active! 🚀");
            }
            else
            {
                PrintWarning("⚠️  Failed to notify system - May need to restart applications");
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    static void ClearBypassList()
    {
        try
        {
            Console.WriteLine();
            PrintHeader("🗑️  Clearing Bypass List...");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true);
            
            if (key == null)
            {
                PrintError("❌ Cannot access proxy registry");
                return;
            }

            key.DeleteValue("ProxyOverride", false);

            Console.WriteLine();
            PrintSuccess("╔════════════════════════════════════════════════╗");
            PrintSuccess("║  ✅ Bypass List Successfully Cleared!          ║");
            PrintSuccess("╚════════════════════════════════════════════════╝");

            // Notify system
            Console.WriteLine();
            PrintInfo("⏳ Notifying system...");
            if (NotifyProxyChange())
            {
                PrintSuccess("✅ System successfully notified - Changes are now active! 🚀");
            }
            else
            {
                PrintWarning("⚠️  Failed to notify system - May need to restart applications");
            }
        }
        catch (Exception ex)
        {
            PrintError($"❌ Error: {ex.Message}");
        }
    }

    // Helper methods for colored output
    static void PrintHeader(string message)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n═══════════════════════════════════════════════");
        Console.WriteLine($"  {message}");
        Console.WriteLine($"═══════════════════════════════════════════════");
        Console.ResetColor();
    }

    static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}