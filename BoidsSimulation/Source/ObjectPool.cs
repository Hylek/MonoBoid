using System;
using System.Collections.Generic;

namespace BoidsSimulation.Source;

public class ObjectPool<T>(Func<T> createFunc, Action<T> actionOnGet, Action<T> actionOnReturn, int defaultCapacity)
{
    private readonly Stack<T> _objects = new(defaultCapacity);

    public T Get()
    {
        var item = _objects.Count > 0 ? _objects.Pop() : createFunc();
        actionOnGet?.Invoke(item);
        return item;
    }

    public void Return(T item)
    {
        actionOnReturn?.Invoke(item);
        _objects.Push(item);
    }
}