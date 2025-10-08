using System;

namespace FEx.WPFx.SVG.SvgConverter;

public static class CmdLineHandler
{
    public static int HandleCommandLine(string arg)
    {
        string[] args = arg is not null
            ? arg.Split(' ')
            : [];

        return HandleCommandLine(args);
    }

    public static int HandleCommandLine(string[] args) =>
        //}
        //    return -1;
        //    Console.WriteLine("Error while handling Commandline.");
        //    //nothing to do, the errors are hopefully already reported via CommandLineParser
        //{
        //catch (Exception)
        //}
        //    return clp.ParseArgs(args, true);
        //{
        //try
        //clp.LogErrorsToConsole = true;
        //clp.Header = "SvgToXaml - Tool to convert SVGs to a Dictionary\r\n(c) 2015 Bernd Klaiber";
        //clp.Target = new CmdLineTarget();
        //CommandLineParser.BKLib.CommandLineParser.CommandLineParser clp = new CommandLineParser.BKLib.CommandLineParser.CommandLineParser { SkipCommandsWhenHelpRequested = true };
        //todo use better command line parser
        throw new NotImplementedException();
}