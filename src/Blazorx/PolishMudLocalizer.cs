using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Collections.Generic;

namespace FEx.Blazorx;

/// <summary>
/// Polish translations for MudBlazor's built-in UI strings (data-grid filter dialog, pager labels, the
/// conversion error under a text-editable picker). Unknown keys fall through to MudBlazor's English
/// defaults (resourceNotFound = true).
/// </summary>
public sealed class PolishMudLocalizer : MudLocalizer
{
    private static readonly Dictionary<string, string> Translations = new()
    {
        // Thrown by MudBlazor's value converters when typed text does not parse as a date/time, and
        // resolved through MudLocalizer exactly like the grid keys - MudFormComponent reads
        // ConversionErrorMessage via its Localizer, so this is the label an operator sees under the field.
        ["Converter_InvalidDateTime"] = "Niepoprawna data lub godzina",
        ["MudDataGrid.AddFilter"] = "Dodaj filtr",
        ["MudDataGrid.Apply"] = "Zastosuj",
        ["MudDataGrid.Cancel"] = "Anuluj",
        ["MudDataGrid.Clear"] = "Wyczyść",
        ["MudDataGrid.Column"] = "Kolumna",
        ["MudDataGrid.Columns"] = "Kolumny",
        ["MudDataGrid.Contains"] = "zawiera",
        ["MudDataGrid.EndsWith"] = "kończy się na",
        ["MudDataGrid.Equal"] = "równe",
        ["MudDataGrid.Filter"] = "Filtr",
        ["MudDataGrid.FilterValue"] = "Wartość filtra",
        ["MudDataGrid.Group"] = "Grupuj",
        ["MudDataGrid.Hide"] = "Ukryj",
        ["MudDataGrid.HideAll"] = "Ukryj wszystkie",
        ["MudDataGrid.is empty"] = "jest puste",
        ["MudDataGrid.is not empty"] = "nie jest puste",
        ["MudDataGrid.NotContains"] = "nie zawiera",
        ["MudDataGrid.NotEqual"] = "różne od",
        ["MudDataGrid.Operator"] = "Operator",
        ["MudDataGrid.RefreshData"] = "Odśwież",
        ["MudDataGrid.Save"] = "Zapisz",
        ["MudDataGrid.ShowAll"] = "Pokaż wszystkie",
        ["MudDataGrid.Sort"] = "Sortuj",
        ["MudDataGrid.StartsWith"] = "zaczyna się od",
        ["MudDataGrid.Ungroup"] = "Rozgrupuj",
        ["MudDataGrid.Unsort"] = "Usuń sortowanie",
        ["MudDataGrid.Value"] = "Wartość",
        ["MudDataGrid.CollapseAllGroups"] = "Zwiń wszystkie grupy",
        ["MudDataGrid.ExpandAllGroups"] = "Rozwiń wszystkie grupy",
        ["MudDataGrid.MoveUp"] = "Przesuń w górę",
        ["MudDataGrid.MoveDown"] = "Przesuń w dół"
    };

    public override LocalizedString this[string key] =>
        Translations.TryGetValue(key, out var value)
            ? new(key, value)
            : new LocalizedString(key, key, true);
}