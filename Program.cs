using Gtk;

namespace GMan;

class Program
{
    static void Main(string[] args)
    {
        Application.Init();
        var window = new MainWindow();
        window.ShowAll();
        Application.Run();
    }
}
