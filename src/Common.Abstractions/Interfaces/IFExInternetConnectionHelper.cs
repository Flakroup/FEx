namespace FEx.Common.Abstractions.Interfaces;

public interface IFExInternetConnectionHelper
{
    /// <summary>
    /// Checks if device is connected to internet and returns straight away if it already is,
    /// or executes OnLackOfInternetAsync if not.
    /// </summary>
    /// <param name="triggersCallbackOnLackOfInternet">If set to <c>true</c> triggers callback method</param>
    /// <returns>True when it is connected, false in case of any exception</returns>
    bool HasInternet(bool triggersCallbackOnLackOfInternet = true);
}