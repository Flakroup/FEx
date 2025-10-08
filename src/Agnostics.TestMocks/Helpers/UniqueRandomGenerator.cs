using System;
using System.Collections.Generic;

namespace FEx.Agnostics.TestMocks.Helpers;

public class UniqueRandomGenerator
{
    private readonly int _minValue;
    private readonly int _maxValue;
    private readonly Random _random = new(DateTime.Now.Millisecond);
    private readonly HashSet<int> _generatedNumbers = [];
    private readonly int _totalCount;

    public UniqueRandomGenerator(int minValue = 1, int maxValue = 500)
    {
        _minValue = minValue;
        _maxValue = maxValue;

        if (_minValue > _maxValue)
            throw new ArgumentException("minValue should be less than or equal to maxValue.");

        _totalCount = _maxValue - _minValue + 1;
    }

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