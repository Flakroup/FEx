using FEx.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FEx.WPFx.SVG.SvgConverter;

public class CmdLineTarget //: SimpleBaseTarget
{
#pragma warning disable S2360 // 10+ parameters - overloads impractical
    //[ArgumentCommand(LongDesc = "Creates a ResourceDictionary with the svg-Images of a folder")]
    public int BuildDict(
        //[ArgumentParam(Aliases = "i", Desc = "dir to the SVGs", LongDesc = "specify folder of the graphic files to process")]
        string inputdir,
        //[ArgumentParam(Aliases = "o", LongDesc = "Name for the xaml outputfile")]
        string outputname,
        //[ArgumentParam(DefaultValue = null, ExplicitNeeded = false, LongDesc = "folder for the xaml-Output, optional, default: folder of svgs")]
        string? outputdir = null,
        //[ArgumentParam(LongDesc = "Builds a htmlfile to browse the svgs, optional, default true")]
        bool buildhtmlfile = true,
        //[ArgumentParam(DefaultValue = null, ExplicitNeeded = false, LongDesc = "Prefix to name alll items of this file, optional, default: no prefix")]
        string? nameprefix = null,
        //[ArgumentParam(DefaultValue = false, ExplicitNeeded = false, LongDesc = "If true, es explicit ResourceKey File is created, default: false", ExplicitWantedArguments = "resKeyNS,resKeyNSName")]
        bool useComponentResKeys = false,
        //[ArgumentParam(DefaultValue = null, ExplicitNeeded = false, LongDesc = "Namespace to use with UseResKey")]
        string? compResKeyNS = null,
        //[ArgumentParam(DefaultValue = null, ExplicitNeeded = false, LongDesc = "name of Namespace to use with UseResKey" )]
        string? compResKeyNSName = null,
        //[ArgumentParam(DefaultValue = false, ExplicitNeeded = false, LongDesc = "If true, PixelsPerDip is filtered to ensure compatibility for < 4.6.2, default: false")]
        bool filterPixelsPerDip = false)
#pragma warning restore S2360
    {
        FExLoggingModule.Log("Building resource dictionary...", GetType());
        var outFileName = Path.Combine(outputdir ?? inputdir, outputname);

        if (!Path.HasExtension(outFileName))
            outFileName = Path.ChangeExtension(outFileName, ".xaml");

        var resKeyInfo = new ResKeyInfo
        {
            Name = null,
            XamlName = Path.GetFileNameWithoutExtension(outputname),
            Prefix = nameprefix,
            UseComponentResKeys = useComponentResKeys,
            NameSpace = compResKeyNS,
            NameSpaceName = compResKeyNSName
        };

        File.WriteAllText(outFileName, ConverterLogic.SvgDirToXaml(inputdir, resKeyInfo, null, filterPixelsPerDip));
        FExLoggingModule.Log($"xaml written to: {outFileName}", GetType());

        if (buildhtmlfile)
        {
            var htmlFilePath = Path.Combine(inputdir, Path.GetFileNameWithoutExtension(outputname));
            var files = ConverterLogic.SvgFilesFromFolder(inputdir);
            BuildHtmlBrowseFile(files, htmlFilePath);
        }

        return 0; //no Error
    }

    private static void BuildHtmlBrowseFile(IEnumerable<string> files, string outputFilename) =>
        BuildHtmlBrowseFile(files, outputFilename, 128);

    private static void BuildHtmlBrowseFile(IEnumerable<string> files, string outputFilename, int size)
    {
        //<html>
        //    <head>
        //        <title>Browse Images</title>
        //    </head>
        //    <body>
        //        Images in file xyz<br>
        //        <img src="cloud-17-icon.svg" title="Title" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //        <img src="cloud-17-icon.svg" height="128" width="128">
        //    </body>
        //</html>
        var doc = new XDocument(new XElement("html",
            new XElement("head", new XElement("title", "Browse svg images")),
            new XElement("body",
                $"Images in file: {outputFilename}",
                new XElement("br"),
                files.Select(f => new XElement("img",
                    new XAttribute("src", Path.GetFileName(f) ?? ""),
                    new XAttribute("title", Path.GetFileNameWithoutExtension(f) ?? ""),
                    new XAttribute("height", size),
                    new XAttribute("width", size))))));

        var filename = Path.ChangeExtension(outputFilename, ".html");
        doc.Save(filename);
        FExLoggingModule.Log($"Html overview written to {filename}", typeof(CmdLineTarget));
    }
}