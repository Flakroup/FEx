using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Collections.Generic;

namespace FEx.Blazorx;

/// <summary>
/// Polish for the MudBlazor strings this library has been asked to translate. Keys are the ones MudBlazor
/// requests verbatim (underscore-separated, e.g. <c>MudDataGrid_Filter</c>). Unknown keys fall through to
/// MudBlazor's English defaults (resourceNotFound = true).
/// </summary>
public sealed class PolishMudLocalizer : MudLocalizer
{
    private static readonly Dictionary<string, string> Translations = new()
    {
        // Thrown by MudBlazor's value converters when typed text does not parse into DateTime, DateOnly,
        // DateTimeOffset or TimeOnly. MudFormComponent reads ConversionErrorMessage through its Localizer,
        // so this is the label an operator sees under the field.
        //
        // NOT the time picker: MudTimePicker binds TimeSpan? and its converter throws
        // Converter_InvalidTimeSpan, which this class does not carry. Ten more Converter_* keys exist.
        // MudDataGridPager_InfoFormat is not translated either: it is a format string with MudBlazor's placeholders.
        ["Converter_InvalidDateTime"] = "Niepoprawna data lub godzina",
        ["MudDataGrid_AddFilter"] = "Dodaj filtr",
        ["MudDataGrid_Apply"] = "Zastosuj",
        ["MudDataGrid_Cancel"] = "Anuluj",
        ["MudDataGrid_Clear"] = "Wyczyść",
        ["MudDataGrid_Column"] = "Kolumna",
        ["MudDataGrid_Columns"] = "Kolumny",
        ["MudDataGrid_Contains"] = "zawiera",
        ["MudDataGrid_EndsWith"] = "kończy się na",
        ["MudDataGrid_Equals"] = "równe",
        ["MudDataGrid_Filter"] = "Filtr",
        ["MudDataGrid_FilterValue"] = "Wartość filtra",
        ["MudDataGrid_Group"] = "Grupuj",
        ["MudDataGrid_Hide"] = "Ukryj",
        ["MudDataGrid_HideAll"] = "Ukryj wszystkie",
        ["MudDataGrid_IsEmpty"] = "jest puste",
        ["MudDataGrid_IsNotEmpty"] = "nie jest puste",
        ["MudDataGrid_NotContains"] = "nie zawiera",
        ["MudDataGrid_NotEquals"] = "różne od",
        ["MudDataGrid_Operator"] = "Operator",
        ["MudDataGrid_RefreshData"] = "Odśwież",
        ["MudDataGrid_Save"] = "Zapisz",
        ["MudDataGrid_ShowAll"] = "Pokaż wszystkie",
        ["MudDataGrid_Sort"] = "Sortuj",
        ["MudDataGrid_StartsWith"] = "zaczyna się od",
        ["MudDataGrid_Ungroup"] = "Rozgrupuj",
        ["MudDataGrid_Unsort"] = "Usuń sortowanie",
        ["MudDataGrid_Value"] = "Wartość",
        ["MudDataGrid_CollapseAllGroups"] = "Zwiń wszystkie grupy",
        ["MudDataGrid_ExpandAllGroups"] = "Rozwiń wszystkie grupy",
        ["MudDataGrid_MoveUp"] = "Przesuń w górę",
        ["MudDataGrid_MoveDown"] = "Przesuń w dół",
        ["MudDataGridPager_AllItems"] = "Wszystkie",
        ["MudDataGridPager_FirstPage"] = "Pierwsza strona",
        ["MudDataGridPager_LastPage"] = "Ostatnia strona",
        ["MudDataGridPager_NextPage"] = "Następna strona",
        ["MudDataGridPager_PreviousPage"] = "Poprzednia strona",
        ["MudDataGridPager_RowsPerPage"] = "Wierszy na stronie:",
        ["MudTablePager_FirstPage"] = "Pierwsza strona",
        ["MudTablePager_LastPage"] = "Ostatnia strona",
        ["MudTablePager_NextPage"] = "Następna strona",
        ["MudTablePager_PreviousPage"] = "Poprzednia strona"
    };

    public override LocalizedString this[string key] =>
        Translations.TryGetValue(key, out var value)
            ? new(key, value)
            : new LocalizedString(key, key, true);
}