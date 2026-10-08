using CommandLine;
using System;
using System.Collections.Generic;

namespace FEx.CLI.Tests;

/// <summary>Option classes and a parse helper that surfaces the real CommandLineParser errors.</summary>
internal static class CliFixtures
{
    /// <summary>Parses with a quiet parser (no help text on the console) and returns the errors it reports.</summary>
    public static IReadOnlyList<Error> ErrorsOf(Type[] types, params string[] args)
    {
        using Parser parser = new(settings => settings.HelpWriter = null);
        List<Error> errors = [];

        parser.ParseArguments(args, types).WithNotParsed(errors.AddRange);

        return errors;
    }

    /// <summary>Single-type (verb-less) parse of an options class given only as a <see cref="Type" />.</summary>
    public static IReadOnlyList<Error> ErrorsOfOptions(Type optionsType, params string[] args)
    {
        using Parser parser = new(settings => settings.HelpWriter = null);
        List<Error> errors = [];

        parser.ParseArguments(() => Activator.CreateInstance(optionsType)!, args)
            .WithNotParsed(errors.AddRange);

        return errors;
    }

    public sealed class Simple
    {
        [Option('n', "name", Required = true)]
        public string Name { get; set; } = string.Empty;

        [Option('p', "port", Default = 80)]
        public int Port { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }
    }

    public sealed class Optional
    {
        [Option('n', "name")]
        public string? Name { get; set; }

        [Option('p', "port")]
        public int Port { get; set; }

        [Option("pair", Min = 2, Max = 2)]
        public IEnumerable<string>? Pair { get; set; }

        [Option("tag")]
        public IEnumerable<string>? Tags { get; set; }
    }

    public sealed class Exclusive
    {
        [Option("aa", SetName = "set-a")]
        public string? A { get; set; }

        [Option("bb", SetName = "set-b")]
        public string? B { get; set; }
    }

    public sealed class Grouped
    {
        [Option("xx", Group = "g")]
        public string? X { get; set; }

        [Option("yy", Group = "g")]
        public string? Y { get; set; }
    }


    public sealed class Throwing
    {
        [Option("boom")]
        public string? Boom
        {
            get => null;
            set => throw new InvalidOperationException("setter exploded");
        }
    }

    [Verb("run", HelpText = "Runs")]
    public sealed class RunVerb
    {
        [Option('f', "fast")]
        public bool Fast { get; set; }
    }

    [Verb("build", HelpText = "Builds")]
    public sealed class BuildVerb
    {
        [Option('c', "config")]
        public string? Config { get; set; }
    }

    [Verb("a", true)]
    public sealed class DefaultVerbA
    {
    }

    [Verb("b", true)]
    public sealed class DefaultVerbB
    {
    }

    public sealed class GroupAndSet
    {
        [Option("gg", Group = "g", SetName = "s")]
        public string? One { get; set; }
    }
}
