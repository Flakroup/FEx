using System;
using System.Collections.Generic;

namespace FEx.Agnostics.TestMocks.Helpers;

/// <summary>Generates random integers from an inclusive range without repeating a value until the pool is exhausted.</summary>
public class UniqueRandomGenerator
{
    private readonly int _minValue;
    private readonly int _maxValue;
    private readonly Random _random = new(DateTime.Now.Millisecond);
    private readonly HashSet<int> _generatedNumbers = [];
    private readonly int _totalCount;

    /// <summary>Initializes a generator that draws from the range 1 to 500 inclusive.</summary>
    public UniqueRandomGenerator()
        : this(1, 500)
    {
    }

    /// <summary>Initializes a generator that draws from the given inclusive range.</summary>
    /// <param name="minValue">The smallest value that can be generated.</param>
    /// <param name="maxValue">The largest value that can be generated.</param>
    /// <exception cref="ArgumentException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
    public UniqueRandomGenerator(int minValue, int maxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;

        if (_minValue > _maxValue)
            throw new ArgumentException("minValue should be less than or equal to maxValue.");

        _totalCount = _maxValue - _minValue + 1;
    }

    /// <summary>Returns a random integer from the configured range that has not been returned before.</summary>
    /// <returns>A previously unreturned integer within the range.</returns>
    /// <exception cref="Exception">Every value in the range has already been generated.</exception>
    public int GenerateUniqueRandomInteger()
    {
        if (_generatedNumbers.Count == _totalCount)
            throw new("Random numbers pool has been exhausted");

        int candidate;

        do
            candidate = _random.Next(_minValue, _maxValue + 1);
        while (_generatedNumbers.Contains(candidate));

        _generatedNumbers.Add(candidate);

        return candidate;
    }
}