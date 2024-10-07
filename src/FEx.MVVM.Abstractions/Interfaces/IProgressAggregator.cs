using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Events;
using System;
using System.Diagnostics;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IProgressAggregator : IProgressStatus, IDisposable
{
    event ProgressPropertyChangedEventHandler ProgressPropertyChanged;
    Stopwatch Stopwatch { get; }
    IFExTimer Timer { get; }
    string Id { get; }
    void Start();
    void Stop();

    /// <summary>
    /// Sets current progress value and maximal allowed value of the ProgressBar
    /// </summary>
    /// <param name="value">Progress value to be added or set</param>
    /// <param name="maximum">Maximal allowed value.</param>
    /// <param name="mode">Progress change mode. ProgressChangeMode.End sets ProgressValue to current ProgressMaximum.</param>
    void PrgSet(double? value, double? maximum = null, ProgressChangeMode mode = ProgressChangeMode.Set);

    /// <summary>
    ///     Sets progress value of the ProgressBar to the maximal value
    /// </summary>
    void PrgSetEnd();

    /// <summary>
    ///     Increments current progress value of the ProgressBar
    /// </summary>
    /// <param name="addedValue">The added value.</param>
    void PrgAdd(double addedValue = 1);

    /// <summary>
    ///     Adds value to the maximum of progress value.
    /// </summary>
    /// <param name="addedValue">The added value.</param>
    void PrgMaxAdd(double addedValue);

    /// <summary>
    ///     Sets maximal allowed value of the ProgressBar and resets current progress
    /// </summary>
    /// <param name="max"></param>
    void PrgSetMax(double max);

    void Report(string propertyName, object value);
}