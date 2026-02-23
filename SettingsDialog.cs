using Gtk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GMan;

public class SettingsDialog
{
    public static (ResponseType response, bool enableHelpFallback, bool useSingleClick, bool favoritesAtTop, bool autoCopySelection) ShowDialog(Window parent)
    {
        // Load current settings
        var settings = Settings.Load();
        bool enableHelpFallback = settings.EnableHelpFallback;
        bool useSingleClick = settings.UseSingleClick;
        bool favoritesAtTop = settings.FavoritesAtTop;
        bool autoCopySelection = settings.AutoCopySelection;

        using var dialog = new Dialog("Settings", parent, DialogFlags.Modal | DialogFlags.DestroyWithParent);
        dialog.SetDefaultSize(400, 300);

        // Create dialog content
        var contentArea = dialog.ContentArea;
        contentArea.Spacing = 10;
        contentArea.MarginStart = 15;
        contentArea.MarginEnd = 15;
        contentArea.MarginTop = 15;
        contentArea.MarginBottom = 15;

        // Help fallback option
        var helpBox = new Box(Orientation.Vertical, 5);
        var enableHelpFallbackCheck = new CheckButton("Run programs with --help when no man page exists");
        enableHelpFallbackCheck.Active = enableHelpFallback;
        helpBox.PackStart(enableHelpFallbackCheck, false, false, 0);

        var warningLabel = new Label("⚠️  Warning: This executes programs which may access system resources");
        warningLabel.Xalign = 0;
        warningLabel.MarginStart = 25;
        var attrList = new Pango.AttrList();
        attrList.Insert(new Pango.AttrScale(0.9));
        attrList.Insert(new Pango.AttrForeground(40000, 20000, 0));
        warningLabel.Attributes = attrList;
        helpBox.PackStart(warningLabel, false, false, 0);

        contentArea.PackStart(helpBox, false, false, 5);

        // Separator
        contentArea.PackStart(new Separator(Orientation.Horizontal), false, false, 5);

        // Click behavior option
        var clickBox = new Box(Orientation.Vertical, 5);
        var clickLabel = new Label("Program list click behavior:");
        clickLabel.Xalign = 0;
        clickLabel.MarginBottom = 5;
        clickBox.PackStart(clickLabel, false, false, 0);

        var singleClickRadio = new RadioButton("Single-click to load man page");
        singleClickRadio.Active = useSingleClick;
        clickBox.PackStart(singleClickRadio, false, false, 0);

        var doubleClickRadio = new RadioButton(singleClickRadio, "Double-click to load man page (default)");
        doubleClickRadio.Active = !useSingleClick;
        clickBox.PackStart(doubleClickRadio, false, false, 0);

        contentArea.PackStart(clickBox, false, false, 5);

        // Separator
        contentArea.PackStart(new Separator(Orientation.Horizontal), false, false, 5);

        // Favorites position option
        var favoritesBox = new Box(Orientation.Vertical, 5);
        var favoritesLabel = new Label("Favorites list position:");
        favoritesLabel.Xalign = 0;
        favoritesLabel.MarginBottom = 5;
        favoritesBox.PackStart(favoritesLabel, false, false, 0);

        var favoritesTopRadio = new RadioButton("Show favorites at top (default)");
        favoritesTopRadio.Active = favoritesAtTop;
        favoritesBox.PackStart(favoritesTopRadio, false, false, 0);

        var favoritesBottomRadio = new RadioButton(favoritesTopRadio, "Show favorites at bottom");
        favoritesBottomRadio.Active = !favoritesAtTop;
        favoritesBox.PackStart(favoritesBottomRadio, false, false, 0);

        contentArea.PackStart(favoritesBox, false, false, 5);

        // Separator
        contentArea.PackStart(new Separator(Orientation.Horizontal), false, false, 5);

        // Auto-copy selection option
        var autoCopyCheck = new CheckButton("Automatically copy selected text in man page to clipboard");
        autoCopyCheck.Active = autoCopySelection;
        contentArea.PackStart(autoCopyCheck, false, false, 5);

        // Separator
        contentArea.PackStart(new Separator(Orientation.Horizontal), false, false, 5);

        // Install button
        var installButton = new Button("Install GMan System-Wide...");
        installButton.Clicked += (sender, args) =>
        {
            ShowInstallDialog(parent);
        };
        contentArea.PackStart(installButton, false, false, 5);

        // Add buttons
        dialog.AddButton("Cancel", ResponseType.Cancel);
        dialog.AddButton("Save", ResponseType.Ok);

        dialog.ShowAll();

        var response = (ResponseType)dialog.Run();

        if (response == ResponseType.Ok)
        {
            enableHelpFallback = enableHelpFallbackCheck.Active;
            useSingleClick = singleClickRadio.Active;
            favoritesAtTop = favoritesTopRadio.Active;
            autoCopySelection = autoCopyCheck.Active;

            // Save settings
            var saveSettings = new Settings
            {
                EnableHelpFallback = enableHelpFallback,
                UseSingleClick = useSingleClick,
                Favorites = settings.Favorites, // Preserve existing favorites
                FavoritesAtTop = favoritesAtTop,
                AutoCopySelection = autoCopySelection
            };
            saveSettings.Save();
        }

        dialog.Hide();
        return (response, enableHelpFallback, useSingleClick, favoritesAtTop, autoCopySelection);
    }

    public static void ShowInstallDialog(Window parent)
    {
        using var dialog = new Dialog("Install GMan", parent, DialogFlags.Modal | DialogFlags.DestroyWithParent);
        dialog.SetDefaultSize(500, 300);

        var contentArea = dialog.ContentArea;
        contentArea.Spacing = 15;
        contentArea.MarginStart = 20;
        contentArea.MarginEnd = 20;
        contentArea.MarginTop = 15;
        contentArea.MarginBottom = 15;

        // Title and description
        var titleLabel = new Label("Choose Installation Type");
        var titleAttrs = new Pango.AttrList();
        titleAttrs.Insert(new Pango.AttrWeight(Pango.Weight.Bold));
        titleAttrs.Insert(new Pango.AttrScale(1.2));
        titleLabel.Attributes = titleAttrs;
        contentArea.PackStart(titleLabel, false, false, 0);

        // User-local option (recommended)
        var userLocalBox = new Box(Orientation.Vertical, 5);
        userLocalBox.MarginStart = 10;

        var userLocalRadio = new RadioButton("User-Local Installation (Recommended)");
        userLocalBox.PackStart(userLocalRadio, false, false, 0);

        var userLocalDesc = new Label("• Install to ~/.local/bin/gman (no sudo required)\n" +
                                      "• Desktop entry in ~/.local/share/applications\n" +
                                      "• Icon in ~/.local/share/icons\n" +
                                      "• Only available to your user account");
        userLocalDesc.Xalign = 0;
        userLocalDesc.MarginStart = 25;
        userLocalDesc.LineWrap = true;
        userLocalBox.PackStart(userLocalDesc, false, false, 0);

        contentArea.PackStart(userLocalBox, false, false, 5);

        // System-wide option
        var systemWideBox = new Box(Orientation.Vertical, 5);
        systemWideBox.MarginStart = 10;

        var systemWideRadio = new RadioButton(userLocalRadio, "System-Wide Installation");
        systemWideBox.PackStart(systemWideRadio, false, false, 0);

        var systemWideDesc = new Label("• Install to /usr/bin/gman (requires sudo)\n" +
                                       "• Desktop entry in /usr/share/applications\n" +
                                       "• Icon in /usr/share/icons\n" +
                                       "• Available to all users on the system");
        systemWideDesc.Xalign = 0;
        systemWideDesc.MarginStart = 25;
        systemWideDesc.LineWrap = true;
        systemWideBox.PackStart(systemWideDesc, false, false, 0);

        contentArea.PackStart(systemWideBox, false, false, 5);

        // Warning for system-wide
        var warningLabel = new Label("⚠️  System-wide installation will prompt for your password");
        warningLabel.Xalign = 0;
        warningLabel.MarginStart = 35;
        var warningAttrs = new Pango.AttrList();
        warningAttrs.Insert(new Pango.AttrScale(0.9));
        warningAttrs.Insert(new Pango.AttrForeground(40000, 20000, 0));
        warningLabel.Attributes = warningAttrs;
        contentArea.PackStart(warningLabel, false, false, 0);

        // Buttons
        dialog.AddButton("Cancel", ResponseType.Cancel);
        var installButton = (Button)dialog.AddButton("Install", ResponseType.Ok);
        installButton.StyleContext.AddClass("suggested-action");

        dialog.ShowAll();
        var response = (ResponseType)dialog.Run();

        if (response == ResponseType.Ok)
        {
            bool success;
            if (userLocalRadio.Active)
            {
                success = UserLocalInstall(parent);
            }
            else
            {
                success = SystemWideInstall(parent);
            }

            if (success)
            {
                ShowInfoDialog(parent, "Installation Successful",
                    userLocalRadio.Active
                    ? "GMan has been installed for your user account.\n\nYou may need to log out and back in for the application menu entry to appear."
                    : "GMan has been installed system-wide.\n\nAll users can now launch GMan from the application menu.");
            }
        }

        dialog.Hide();
    }

    private static bool UserLocalInstall(Window parent)
    {
        try
        {
            // Get paths
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localBin = Path.Combine(homeDir, ".local", "bin");
            string localApps = Path.Combine(homeDir, ".local", "share", "applications");
            string localIcons = Path.Combine(homeDir, ".local", "share", "icons", "hicolor", "128x128", "apps");

            // Create directories if they don't exist
            Directory.CreateDirectory(localBin);
            Directory.CreateDirectory(localApps);
            Directory.CreateDirectory(localIcons);

            // Get current executable location
            string currentExe = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(currentExe))
            {
                ShowErrorDialog(parent, "Installation Error", "Could not determine executable location.");
                return false;
            }

            string exeDir = Path.GetDirectoryName(currentExe) ?? "";
            string targetExe = Path.Combine(localBin, "gman");

            // Copy executable
            File.Copy(currentExe, targetExe, true);

            // Make executable (chmod +x)
            var chmodProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{targetExe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chmodProcess.Start();
            chmodProcess.WaitForExit();

            // Copy dependencies
            string[] dependencies = { "gman.dll", "gman.runtimeconfig.json", "gman.deps.json" };
            foreach (var dep in dependencies)
            {
                string sourcePath = Path.Combine(exeDir, dep);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, Path.Combine(localBin, dep), true);
                }
            }

            // Copy UI resources
            string sourceUiDir = Path.Combine(exeDir, "ui");
            string targetUiDir = Path.Combine(localBin, "ui");
            if (Directory.Exists(sourceUiDir))
            {
                Directory.CreateDirectory(targetUiDir);
                foreach (var file in Directory.GetFiles(sourceUiDir))
                {
                    string fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(targetUiDir, fileName), true);
                }
            }

            // Copy icon
            string sourceIcon = Path.Combine(exeDir, "ui", "icon_128.png");
            if (File.Exists(sourceIcon))
            {
                File.Copy(sourceIcon, Path.Combine(localIcons, "gman.png"), true);
            }

            // Create desktop file
            string desktopContent = $@"[Desktop Entry]
Version=2.0
Type=Application
Name=GMan
Comment=GTK# Man Page Viewer
Exec={targetExe}
Icon=gman
Terminal=false
Categories=Utility;Development;Documentation;
Keywords=man;manual;documentation;help;
StartupWMClass=gman
";
            File.WriteAllText(Path.Combine(localApps, "gman.desktop"), desktopContent);

            // Update icon cache (optional, may not exist on all systems)
            try
            {
                var updateIconProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gtk-update-icon-cache",
                        Arguments = $"-f -t \"{Path.Combine(homeDir, ".local", "share", "icons", "hicolor")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };
                updateIconProcess.Start();
                updateIconProcess.WaitForExit();
            }
            catch { /* Ignore if gtk-update-icon-cache not available */ }

            // Update desktop database (optional)
            try
            {
                var updateDesktopProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "update-desktop-database",
                        Arguments = $"\"{localApps}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };
                updateDesktopProcess.Start();
                updateDesktopProcess.WaitForExit();
            }
            catch { /* Ignore if update-desktop-database not available */ }

            return true;
        }
        catch (Exception ex)
        {
            ShowErrorDialog(parent, "Installation Error", $"Failed to install: {ex.Message}");
            return false;
        }
    }

    private static bool SystemWideInstall(Window parent)
    {
        try
        {
            // Check if pkexec is available
            if (!IsCommandAvailable("pkexec"))
            {
                ShowErrorDialog(parent, "PolicyKit Not Available",
                    "pkexec (PolicyKit) is not available on this system.\n\n" +
                    "Please install PolicyKit or use the user-local installation option.");
                return false;
            }

            // Get current executable location
            string currentExe = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(currentExe))
            {
                ShowErrorDialog(parent, "Installation Error", "Could not determine executable location.");
                return false;
            }

            string exeDir = Path.GetDirectoryName(currentExe) ?? "";
            string tempDir = Path.GetTempPath();
            string scriptPath = Path.Combine(tempDir, $"gman-install-{Guid.NewGuid()}.sh");

            string gmanDll = Path.Combine(exeDir, "gman.dll");
            string gmanRuntimeConfig = Path.Combine(exeDir, "gman.runtimeconfig.json");
            string gmanDeps = Path.Combine(exeDir, "gman.deps.json");
            string gmanUi = Path.Combine(exeDir, "ui");
            string gmanIcon = Path.Combine(exeDir, "ui", "icon_128.png");

            // Create installation script
            string script = @"#!/bin/bash
set -e

# Install executable and dependencies
cp -f """ + currentExe + @""" /usr/bin/gman
chmod 755 /usr/bin/gman

# Install dependencies
cp -f """ + gmanDll + @""" /usr/bin/gman.dll 2>/dev/null || true
cp -f """ + gmanRuntimeConfig + @""" /usr/bin/gman.runtimeconfig.json 2>/dev/null || true
cp -f """ + gmanDeps + @""" /usr/bin/gman.deps.json 2>/dev/null || true

# Install UI resources
mkdir -p /usr/bin/ui
cp -rf """ + gmanUi + @"""/* /usr/bin/ui/ 2>/dev/null || true

# Install icon
mkdir -p /usr/share/icons/hicolor/128x128/apps
cp -f """ + gmanIcon + @""" /usr/share/icons/hicolor/128x128/apps/gman.png 2>/dev/null || true

# Install desktop file
cat > /usr/share/applications/gman.desktop << 'DESKTOP_END'
[Desktop Entry]
Version=2.0
Type=Application
Name=GMan
Comment=GTK# Man Page Viewer
Exec=/usr/bin/gman
Icon=gman
Terminal=false
Categories=Utility;Development;Documentation;
Keywords=man;manual;documentation;help;
StartupWMClass=gman
DESKTOP_END

chmod 644 /usr/share/applications/gman.desktop

# Update caches
gtk-update-icon-cache -f -t /usr/share/icons/hicolor 2>/dev/null || true
update-desktop-database /usr/share/applications 2>/dev/null || true

echo ""Installation complete""
";
            File.WriteAllText(scriptPath, script);

            // Make script executable
            var chmodProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chmodProcess.Start();
            chmodProcess.WaitForExit();

            // Execute with pkexec
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pkexec",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Clean up script
            try { File.Delete(scriptPath); } catch { }

            if (process.ExitCode == 0)
            {
                return true;
            }
            else if (process.ExitCode == 126 || process.ExitCode == 127)
            {
                // User cancelled or authentication failed
                ShowInfoDialog(parent, "Installation Cancelled", "Installation was cancelled or authentication failed.");
                return false;
            }
            else
            {
                ShowErrorDialog(parent, "Installation Failed",
                    $"Failed to install (exit code {process.ExitCode}).\n\n" +
                    $"Error: {error}\n\nOutput: {output}");
                return false;
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog(parent, "Installation Error", $"Failed to install: {ex.Message}");
            return false;
        }
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void ShowInfoDialog(Window parent, string title, string message)
    {
        using var dialog = new MessageDialog(
            parent,
            DialogFlags.Modal,
            MessageType.Info,
            ButtonsType.Ok,
            message);
        dialog.Title = title;
        dialog.Run();
        dialog.Hide();
    }

    private static void ShowErrorDialog(Window parent, string title, string message)
    {
        using var dialog = new MessageDialog(
            parent,
            DialogFlags.Modal,
            MessageType.Error,
            ButtonsType.Ok,
            message);
        dialog.Title = title;
        dialog.Run();
        dialog.Hide();
    }
}

public class Settings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "gman",
        "settings.conf"
    );

    public bool EnableHelpFallback { get; set; } = false;  // Default: disabled for security
    public bool UseSingleClick { get; set; } = false;       // Default: double-click
    public List<string> Favorites { get; set; } = new();    // Default: empty list
    public bool FavoritesAtTop { get; set; } = true;        // Default: favorites at top
    public bool ShowNotes { get; set; } = false;            // Default: notes hidden
    public bool AutoCopySelection { get; set; } = false;    // Default: auto-copy disabled

    public static Settings Load()
    {
        var settings = new Settings();

        if (File.Exists(SettingsPath))
        {
            try
            {
                var lines = File.ReadAllLines(SettingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        switch (key)
                        {
                            case "EnableHelpFallback":
                                settings.EnableHelpFallback = value.ToLower() == "true";
                                break;
                            case "UseSingleClick":
                                settings.UseSingleClick = value.ToLower() == "true";
                                break;
                            case "Favorites":
                                // Parse comma-separated list
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    settings.Favorites = value.Split(',')
                                        .Select(f => f.Trim())
                                        .Where(f => !string.IsNullOrEmpty(f))
                                        .ToList();
                                }
                                break;
                            case "FavoritesAtTop":
                                settings.FavoritesAtTop = value.ToLower() == "true";
                                break;
                            case "ShowNotes":
                                settings.ShowNotes = value.ToLower() == "true";
                                break;
                            case "AutoCopySelection":
                                settings.AutoCopySelection = value.ToLower() == "true";
                                break;
                        }
                    }
                }
            }
            catch { }
        }

        return settings;
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var favoritesString = Favorites.Count > 0 ? string.Join(",", Favorites) : "";
            var content = $"EnableHelpFallback={EnableHelpFallback}\n" +
                         $"UseSingleClick={UseSingleClick}\n" +
                         $"Favorites={favoritesString}\n" +
                         $"FavoritesAtTop={FavoritesAtTop}\n" +
                         $"ShowNotes={ShowNotes}\n" +
                         $"AutoCopySelection={AutoCopySelection}\n";

            File.WriteAllText(SettingsPath, content);
        }
        catch { }
    }
}
