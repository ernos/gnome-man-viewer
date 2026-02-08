using Gtk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GMan;

public class MainWindow
{
    private readonly Window mainWindow;
    private readonly Entry searchEntry;
    private readonly TextView manPageView;
    private readonly Label statusLabel;
    private readonly TreeView programListView;
    private readonly ListStore programStore;
    private List<string> allPrograms = new();

    public MainWindow()
    {
        var builder = new Builder();
        builder.AddFromFile(GetUiPath());

        mainWindow = (Window)builder.GetObject("mainWindow");
        searchEntry = (Entry)builder.GetObject("searchEntry");
        programListView = (TreeView)builder.GetObject("programListView");
        manPageView = (TextView)builder.GetObject("manPageView");
        statusLabel = (Label)builder.GetObject("statusLabel");

        if (mainWindow == null || searchEntry == null || programListView == null || manPageView == null || statusLabel == null)
        {
            throw new InvalidOperationException("Failed to load UI from main_window.ui");
        }

        mainWindow.DeleteEvent += OnDeleteEvent;
        searchEntry.Changed += OnSearchTextChanged;

        programStore = new ListStore(typeof(string));
        programListView.Model = programStore;
        programListView.HeadersVisible = false;

        var column = new TreeViewColumn();
        var cellRenderer = new CellRendererText();
        column.PackStart(cellRenderer, true);
        column.AddAttribute(cellRenderer, "text", 0);
        programListView.AppendColumn(column);
        programListView.RowActivated += OnProgramSelected;

        manPageView.Editable = false;
        manPageView.WrapMode = WrapMode.Word;

        LoadPrograms();
    }

    public void ShowAll()
    {
        mainWindow.ShowAll();
    }

    private void LoadPrograms()
    {
        var paths = new[] { "/bin", "/usr/bin", "/usr/local/bin", "/sbin", "/usr/sbin" };
        var programs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        programs.Add(System.IO.Path.GetFileName(file));
                    }
                }
                catch { }
            }
        }

        allPrograms = programs.ToList();
        RefreshProgramList("");
        statusLabel.Text = $"Ready - {allPrograms.Count} programs available";
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        string query = searchEntry.Text.Trim().ToLower();
        RefreshProgramList(query);
    }

    private void RefreshProgramList(string filter)
    {
        programStore.Clear();

        var filtered = string.IsNullOrEmpty(filter)
            ? allPrograms
            : allPrograms.Where(p => p.ToLower().Contains(filter)).ToList();

        foreach (var program in filtered)
        {
            programStore.AppendValues(program);
        }

        statusLabel.Text = $"Found {filtered.Count} program(s)";
    }

    private void OnProgramSelected(object? sender, RowActivatedArgs args)
    {
        string pathStr = args.Path.ToString();
        if (programStore.GetIterFromString(out var iter, pathStr))
        {
            var program = programStore.GetValue(iter, 0)?.ToString();
            if (!string.IsNullOrEmpty(program))
            {
                LoadManPage(program);
            }
        }
    }

    private void LoadManPage(string pageName)
    {
        try
        {
            statusLabel.Text = $"Loading man page for '{pageName}'...";
            string manContent = GetManPageContent(pageName);

            if (string.IsNullOrEmpty(manContent))
            {
                statusLabel.Text = $"No man page found for '{pageName}'";
                manPageView.Buffer.Text = $"No manual entry for {pageName}";
            }
            else
            {
                manPageView.Buffer.Text = manContent;
                statusLabel.Text = $"Displaying: {pageName}";
            }
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error: {ex.Message}";
            manPageView.Buffer.Text = $"Error loading man page: {ex.Message}";
        }
    }

    private string GetManPageContent(string pageName)
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "man";
            process.StartInfo.Arguments = pageName;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private void OnDeleteEvent(object? sender, DeleteEventArgs args)
    {
        Application.Quit();
        args.RetVal = true;
    }

    private static string GetUiPath()
    {
        string baseDir = AppContext.BaseDirectory;
        string uiPath = System.IO.Path.Combine(baseDir, "ui", "main_window.ui");
        if (File.Exists(uiPath))
        {
            return uiPath;
        }

        string devPath = System.IO.Path.Combine(baseDir, "..", "..", "..", "ui", "main_window.ui");
        return System.IO.Path.GetFullPath(devPath);
    }
}
