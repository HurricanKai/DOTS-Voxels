using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

public struct ComputeBufferPool : IDisposable
{
    private Stack<ComputeBuffer> _available;
    private Stack<ComputeBuffer> _used;

    private int _count;
    private int _stride;
    private ComputeBufferType _type;
    [CanBeNull] private string _name;
    private int _rentedCount;

    public ComputeBufferPool(int count, int stride, ComputeBufferType type, string name = null)
    {
        _available = new Stack<ComputeBuffer>();
        _used = new Stack<ComputeBuffer>();
        _count = count;
        _stride = stride;
        _type = type;
        _name = name;
        _rentedCount = 0;
    }

    public ComputeBuffer Rent()
    {
        Profiler.BeginSample("Rent Computer Buffer");
        ComputeBuffer c;
		
        if (_available.Count > 0)
        {
            c = _available.Pop();
        }
        else
        {
            c = new ComputeBuffer(_count, _stride, _type);
            if (_name != null)
                c.name = _name + _rentedCount.ToString();
            _rentedCount++;
        }

        _used.Push(c);
        Profiler.EndSample();
        return c;
    }

    public void Swap()
    {
        if (_used.Count == 0)
            return;
        Profiler.BeginSample("Swap Buffers");
        while (_available.Count > 0)
        {
            _used.Push(_available.Pop());
        }

        var newUsed = _available;
        _available = _used;
        _used = newUsed;
        Profiler.EndSample();
    }

    public void Dispose()
    {
        if (_available != null)
        {
            foreach (var computeBuffer in _available)
            {
                computeBuffer.Dispose();
            }

            _available = null;
        }

        if (_used != null)
        {
            foreach (var computeBuffer in _used)
            {
                computeBuffer.Dispose();
            }

            _used = null;
        }
    }
}