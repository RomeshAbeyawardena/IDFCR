﻿using IDFCR.Shared.Http.Links;
using IDFCR.Shared.Http.Results;

namespace IDFCR.Shared.Http.Extensions;

public static class HypermediaExtensions
{
    public static IHypermedia<T> AddLink<T>(this Hypermedia<T> hypermedia, string rel, ILink link)
    {
        hypermedia._links[rel] = link;
        return hypermedia;
    }

    public static IHypermedia<T> AddMeta<T>(this Hypermedia<T> hypermedia, string key, object? value)
    {
        hypermedia._meta[key] = value;
        return hypermedia;
    }
}