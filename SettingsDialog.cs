using Gtk;
using System;
using System.IO;

namespace GMan;

public class SettingsDialog
{
    private readonly Dialog dialog;
    private readonly CheckButton enableHelpFallbackCheck;
    private readonly RadioButton singleClickRadio;
    private readonly RadioButton doubleClickRadio;
    
    public bool EnableHelpFallback { get; private set; }
    public bool UseSingleClick { get; private set; }
    
    public SettingsDialog(Window parent)
    {
        dialog = new Dialog("Settings", parent, DialogFlags.Modal);
        dialog.SetDefaultSize(400, 250);
        
        // Load current settings
        var settings = Settings.Load();
        EnableHelpFallback = settings.EnableHelpFallback;
        UseSingleClick = settings.UseSingleClick;
        
        // Create dialog content
        var contentArea = dialog.ContentArea;
        contentArea.Spacing = 10;
        contentArea.MarginStart = 15;
        contentArea.MarginEnd = 15;
        contentArea.MarginTop = 15;
        contentArea.MarginBottom = 15;
        
        // Help fallback option
        var helpBox = new Box(Orientation.Vertical, 5);
        enableHelpFallbackCheck = new CheckButton("Run programs with --help when no man page exists");
        enableHelpFallbackCheck.Active = EnableHelpFallback;
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
        
        singleClickRadio = new RadioButton("Single-click to load man page");
        singleClickRadio.Active = UseSingleClick;
        clickBox.PackStart(singleClickRadio, false, false, 0);
        
        doubleClickRadio = new RadioButton(singleClickRadio, "Double-click to load man page (default)");
        doubleClickRadio.Active = !UseSingleClick;
        clickBox.PackStart(doubleClickRadio, false, false, 0);
        
        contentArea.PackStart(clickBox, false, false, 5);
        
        // Add buttons
        dialog.AddButton("Cancel", ResponseType.Cancel);
        dialog.AddButton("Save", ResponseType.Ok);
        
        dialog.ShowAll();
    }
    
    public ResponseType Run()
    {
        var response = (ResponseType)dialog.Run();
        
        if (response == ResponseType.Ok)
        {
            EnableHelpFallback = enableHelpFallbackCheck.Active;
            UseSingleClick = singleClickRadio.Active;
            
            // Save settings
            var settings = new Settings
            {
                EnableHelpFallback = EnableHelpFallback,
                UseSingleClick = UseSingleClick
            };
            settings.Save();
        }
        
        dialog.Destroy();
        return response;
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
                        var value = parts[1].Trim().ToLower();
                        
                        switch (key)
                        {
                            case "EnableHelpFallback":
                                settings.EnableHelpFallback = value == "true";
                                break;
                            case "UseSingleClick":
                                settings.UseSingleClick = value == "true";
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
            
            var content = $"EnableHelpFallback={EnableHelpFallback}\n" +
                         $"UseSingleClick={UseSingleClick}\n";
            
            File.WriteAllText(SettingsPath, content);
        }
        catch { }
    }
}
