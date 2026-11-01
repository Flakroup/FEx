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

    /// <summary>The skeleton's sentence; <paramref name="source"/> is where the data comes from, in the genitive ("Płatnika").</summary>
    public static string Loading(string source) => $"Wczytywanie danych z {source}...";

    /// <summary>The tooltip of the refresh button; <paramref name="source"/> as in <see cref="Loading"/>.</summary>
    public static string Refresh(string source) => $"Odśwież dane z {source}";

    /// <summary>The bold line of the unreachable state; <paramref name="connectionTarget"/> in the form that follows "połączyć z" ("bazą Płatnika").</summary>
    public static string ErrorTitle(string connectionTarget) => $"Nie udało się połączyć z {connectionTarget}";

    public static string Fresh(DateTime at) => $"Wczytano o {at:HH:mm}";
}
