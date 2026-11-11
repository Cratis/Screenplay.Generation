// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

/// <summary>
/// A read-only list that reports every enumeration, count read and index read to the observer.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="inner">The wrapped items.</param>
/// <param name="observer">Called with the kind of read, "enumerate", "count" or "index".</param>
public sealed class an_observed_list<T>(IReadOnlyList<T> inner, Action<string> observer) : IReadOnlyList<T>
{
    public int Count
    {
        get
        {
            observer("count");
            return inner.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            observer("index");
            return inner[index];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        observer("enumerate");
        return inner.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
