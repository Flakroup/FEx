using System;

namespace FEx.WPFx.Fluent.Views;

/// <summary>The sentences of the load pattern, in one place so every tab says the same thing.</summary>
public static class LoadText
{
    public const string Blocked = "Najpierw muszą się wczytać dane";
    public const string ErrorHint = "Sprawdź połączenie w Ustawieniach i odśwież.";
    public const string Retry = "Odśwież";
    public const string OpenSettings = "Otwórz Ustawienia";
    public const string MissingTitle = "Brak konfiguracji bazy";
    public const string MissingHint = "Uzupełnij połączenie w Ustawieniach.";

    /// <summary>The hint under an empty list of a tab that has a year or period to pick.</summary>
    public const string OtherYearHint = "Wybierz inny rok.";

    /// <summary>The same hint for a tab whose parameter is a month.</summary>
    public const string OtherMonthHint = "Wybierz inny miesiąc.";

    /// <summary>The bold line when the data was read but could not be shown.</summary>
    public const string DisplayFailedTitle = "Nie udało się wyświetlić danych";

    /// <summary>The line under it.</summary>
    public const string DisplayFailedHint = "Dane zostały odczytane, ale nie dało się ich pokazać. Spróbuj odświeżyć, a jeśli to nie pomoże, zgłoś błąd.";

    /// <summary>The skeleton's sentence; <paramref name="source"/> is where the data comes from, in the genitive ("Płatnika"). Blank source drops the clause.</summary>
    public static string Loading(string source) =>
        string.IsNullOrWhiteSpace(source) ? "Wczytywanie danych..." : $"Wczytywanie danych z {source}...";

    /// <summary>The tooltip of the refresh button; <paramref name="source"/> as in <see cref="Loading"/>.</summary>
    public static string Refresh(string source) =>
        string.IsNullOrWhiteSpace(source) ? "Odśwież dane" : $"Odśwież dane z {source}";

    /// <summary>The bold line of the unreachable state; <paramref name="connectionTarget"/> in the form that follows "połączyć z" ("bazą Płatnika"). Blank names the database generically.</summary>
    public static string ErrorTitle(string connectionTarget) =>
        string.IsNullOrWhiteSpace(connectionTarget) ? "Nie udało się połączyć z bazą danych" : $"Nie udało się połączyć z {connectionTarget}";

    public static string Fresh(DateTime at) => $"Wczytano o {at:HH:mm}";
}
