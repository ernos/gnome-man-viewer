using Gtk;
using System;
using System.IO;

namespace GMan;

public class SettingsDialog
{
    public static (ResponseType response, bool enableHelpFallback, bool useSingleClick, bool favoritesAtTop) ShowDialog(Window parent)
    {
        // Load current settings
        var settings = Settings.Load();
        bool enableHelpFallback = settings.EnableHelpFallback;
        bool useSingleClick = settings.UseSingleClick;
        bool favoritesAtTop = settings.FavoritesAtTop;

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

            // Save settings
            var saveSettings = new Settings
            {
                EnableHelpFallback = enableHelpFallback,
                UseSingleClick = useSingleClick,
                Favorites = settings.Favorites, // Preserve existing favorites
                FavoritesAtTop = favoritesAtTop
            };
            saveSettings.Save();
        }

        dialog.Hide();
        return (response, enableHelpFallback, useSingleClick, favoritesAtTop);
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
                         $"ShowNotes={ShowNotes}\n";

            File.WriteAllText(SettingsPath, content);
        }
        catch { }
    }
}
