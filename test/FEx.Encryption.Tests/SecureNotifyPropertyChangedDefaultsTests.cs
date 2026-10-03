using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Encryption.Exceptions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// What a consumer gets without overriding anything. A failed read returns null, so the default report is
/// the only trace it leaves - and a silent one lets the user type a new value over ciphertext that was
/// perfectly recoverable. The report must not cost the secret either: whatever reaches a log is plaintext's
/// way off the machine.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class SecureNotifyPropertyChangedDefaultsTests : IDisposable
{
    private const int FastIterations = FExStringCipher.MinIterations;
    private const string Plaintext = "hunter2-SECRET";

    private static readonly FExStringCipher _cipher = new("the owning passphrase", FastIterations);
    private static readonly FExStringCipher _stranger = new("an unrelated passphrase", FastIterations);

    private static readonly string[] _logMethods =
        [nameof(IFExLogger.Trace), nameof(IFExLogger.Debug), nameof(IFExLogger.Information),
         nameof(IFExLogger.Warning), nameof(IFExLogger.Error), nameof(IFExLogger.Critical)];

    private readonly IFExLogger _previous = FExStaticLogger.Instance;
    private readonly IFExLogger _logger = Substitute.For<IFExLogger>();

    public SecureNotifyPropertyChangedDefaultsTests()
    {
        FExStaticLogger.Configure(() => _logger);
    }

    public void Dispose() => FExStaticLogger.Configure(() => _previous);

    [Fact]
    public void DefaultOnDecryptionFailed_LogsThePropertyAndTheExceptionType()
    {
        var secrets = new DefaultSecrets(_cipher) { RawSecret = _stranger.Encrypt("written elsewhere") };

        secrets.Secret.ShouldBeNull();

        _logger.ReceivedWithAnyArgs(1).Warning(default(string)!);
        var logged = LoggedText().ShouldHaveSingleItem();
        logged.ShouldContain(nameof(DefaultSecrets.Secret));
        logged.ShouldContain(nameof(FExDecryptionException));
        logged.ShouldNotContain("integrity check", Case.Insensitive, "the log carries the exception type, not its message");
    }

    [Fact]
    public void JsonShapeMismatch_KeepsThePlaintextOutOfTheLogAndTheReportedException()
    {
        // Authentic, decryptable JSON whose shape does not fit: a string where an int belongs. The
        // deserializer's own message quotes the offending value - the decrypted secret.
        var secrets = new RecordingSecrets(_cipher) { RawPayload = _cipher.Encrypt($"{{\"{nameof(Payload.Count)}\":\"{Plaintext}\"}}") };

        secrets.Data.ShouldBeNull();

        var reported = secrets.Failures.ShouldHaveSingleItem();
        reported.ShouldBeOfType<FExDecryptionException>();
        reported.InnerException.ShouldBeNull();
        reported.ToString().ShouldNotContain(Plaintext);
        reported.Message.ShouldContain(nameof(Payload.Count), Case.Sensitive, "the JSON path is still reported");

        var logged = LoggedText().ToList();
        logged.ShouldNotBeEmpty();
        logged.ShouldAllBe(text => !text.Contains(Plaintext));
    }

    /// <summary>Every string and exception handed to a logging method, flattened to text.</summary>
    private IEnumerable<string> LoggedText() =>
        _logger.ReceivedCalls()
            .Where(call => _logMethods.Contains(call.GetMethodInfo().Name))
            .SelectMany(call => call.GetArguments())
            .Select(argument => argument switch
            {
                Exception exception => exception.ToString(),
                _ => argument?.ToString()
            })
            .OfType<string>();

    private sealed class Payload
    {
        public int Count { get; set; }
    }

    /// <summary>Overrides only the cipher; <see cref="SecureNotifyPropertyChanged.OnDecryptionFailed" /> keeps its default body.</summary>
    private class DefaultSecrets : SecureNotifyPropertyChanged
    {
        private readonly FExStringCipher _instanceCipher;
        private string? _secret;
        private string? _payload;

        internal DefaultSecrets(FExStringCipher cipher)
        {
            _instanceCipher = cipher;
        }

        internal string? RawSecret
        {
            set => _secret = value;
        }

        internal string? RawPayload
        {
            set => _payload = value;
        }

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }

        public Payload? Data => DecryptFromJsonSource<Payload>(_payload);

        protected override FExStringCipher Cipher => _instanceCipher;
    }

    /// <summary>Also keeps what was reported, then hands it to the default body so the log is exercised too.</summary>
    private sealed class RecordingSecrets : DefaultSecrets
    {
        internal RecordingSecrets(FExStringCipher cipher)
            : base(cipher)
        {
        }

        internal List<Exception> Failures { get; } = [];

        protected override void OnDecryptionFailed(string? propertyName, Exception exception)
        {
            Failures.Add(exception);
            base.OnDecryptionFailed(propertyName, exception);
        }
    }
}
