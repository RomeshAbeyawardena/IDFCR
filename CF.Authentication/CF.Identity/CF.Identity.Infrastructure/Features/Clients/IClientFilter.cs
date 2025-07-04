﻿using IDFCR.Shared.Abstractions.Filters;
using IDFCR.Shared.Abstractions.Paging;

namespace CF.Identity.Infrastructure.Features.Clients;

public interface IPagedClientFilter : IClientFilter, IPagedQuery;
public interface IClientFilter : IFilter<IClientFilter>, IValidityFilter
{
    bool ShowAll { get; }
    string? Key { get; }
}
