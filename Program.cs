using FireSaveRepair.Core;

namespace FireSaveRepair;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if(args.Length>0)
        {
            try {
                if(args[0]=="--ui-test"){ApplicationConfiguration.Initialize();return UI.UiTests.Run(args);}
                return SelfTests.Run(args);
            }
            catch(Exception e) { Console.Error.WriteLine(e);return 1; }
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
