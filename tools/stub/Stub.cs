using System;
using System.IO;
using System.Text;

internal static class StubJava
{
    private static int Main(string[] args)
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        File.WriteAllText(Path.Combine(dir, "args.txt"), Environment.CommandLine, Encoding.UTF8);
        Console.WriteLine("stub java: arguments captured");
        return 0;
    }
}
