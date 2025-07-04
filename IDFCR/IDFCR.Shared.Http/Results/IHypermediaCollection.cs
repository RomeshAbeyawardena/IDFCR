﻿namespace IDFCR.Shared.Http.Results;

public interface IHypermediaCollection<T> : IEnumerable<IHypermedia<T>>
{
    IHypermedia<T> Add(T item);
    bool Remove(IHypermedia<T> items);
    IEnumerable<T>? AsRawEnumerable();
}
