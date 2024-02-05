using FEx.Basics.Abstractions;
using FEx.Extensions;
using FEx.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FEx.Encryption;

public class SecureNotifyPropertyChanged : NotifyPropertyChanged
{
    protected virtual bool IsValid(string propertyName, string decryptedValue) =>
        decryptedValue.All(MatchesUnicodeCategory);

    protected string DecryptFromSource(ref string source, [CallerMemberName] string propertyName = null)
    {
        try
        {
            if (source is not null)
                return Sanitize(ref source, StringHasher.DecryptString(FExEncryption.PassPhrase, source), propertyName);
        }
        catch (Exception ex)
        {
            EncryptSource(ref source, null);
            ex.HandleException(false);
        }

        return null;
    }

    protected T DecryptFromJsonSource<T>(ref string source, [CallerMemberName] string propertyName = null)
    {
        string json = DecryptFromSource(ref source, propertyName);

        try
        {
            return json.FromJson<T>();
        }
        catch
        {
            return default;
        }
    }

    protected bool EncryptJsonSource<T>(ref string backingField,
                                        T newValue,
                                        Action<string> onPropertyChanged = null,
                                        [CallerMemberName] string propertyName = null) =>
        EncryptSource(ref backingField, newValue?.ToJson(), onPropertyChanged, propertyName);

    protected bool EncryptSource(ref string backingField,
                                 string newValue,
                                 Action<string> onPropertyChanged = null,
                                 [CallerMemberName] string propertyName = null)
    {
        string encrypted = null;

        if (newValue is not null)
        {
            encrypted = StringHasher.EncryptString(FExEncryption.PassPhrase, newValue);

            if (newValue != StringHasher.DecryptString(FExEncryption.PassPhrase, encrypted))
                throw new InvalidDataException("Inconsistent data detected");
        }

        return SetProperty(ref backingField, encrypted, onPropertyChanged, propertyName);
    }

    private static bool MatchesUnicodeCategory(char c)
    {
        return char.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.ClosePunctuation => true,
            UnicodeCategory.ConnectorPunctuation => true,
            UnicodeCategory.CurrencySymbol => true,
            UnicodeCategory.DashPunctuation => true,
            UnicodeCategory.DecimalDigitNumber => true,
            UnicodeCategory.EnclosingMark => true,
            UnicodeCategory.FinalQuotePunctuation => true,
            UnicodeCategory.Format => true,
            UnicodeCategory.InitialQuotePunctuation => true,
            UnicodeCategory.LetterNumber => true,
            UnicodeCategory.LineSeparator => true,
            UnicodeCategory.LowercaseLetter => true,
            UnicodeCategory.MathSymbol => true,
            UnicodeCategory.ModifierLetter => true,
            UnicodeCategory.ModifierSymbol => true,
            UnicodeCategory.NonSpacingMark => true,
            UnicodeCategory.OpenPunctuation => true,
            UnicodeCategory.OtherLetter => true,
            UnicodeCategory.OtherNotAssigned => true,
            UnicodeCategory.OtherNumber => true,
            UnicodeCategory.OtherPunctuation => true,
            UnicodeCategory.ParagraphSeparator => true,
            UnicodeCategory.SpaceSeparator => true,
            UnicodeCategory.SpacingCombiningMark => true,
            UnicodeCategory.Surrogate => true,
            UnicodeCategory.TitlecaseLetter => true,
            UnicodeCategory.UppercaseLetter => true,
            _ => false
        };
    }

    private string Sanitize(ref string source, string decryptedValue, string propertyName)
    {
        if (IsValid(propertyName, decryptedValue))
            return decryptedValue;

        EncryptSource(ref source, null);

        return null;
    }
}