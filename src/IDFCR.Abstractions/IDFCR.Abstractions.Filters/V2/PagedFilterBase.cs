using IDFCR.Abstractions.Metadata;
using IDFCR.Abstractions.Results;
using System.Linq.Dynamic.Core;

namespace IDFCR.Abstractions.Filters.V2;

/// <summary>
/// Represents a base class for paged filters that derive their predicate from the request and apply paging to the query.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TDb">The type of the database entity.</typeparam>
public abstract class PagedFilterBase<TRequest, TDb> : FilterBase<TRequest, TDb>, IPagedFilter<TRequest, TDb>
    where TRequest : IPagedQuery
{
    /// <summary>
    /// Applies paging to the given query based on the provided request.
    /// <para>
    /// This method can be overridden by derived classes to customize paging,
    /// ordering, or other query shaping behavior before the page is returned.
    /// </para>
    /// </summary>
    /// <param name="query">The query to apply paging to.</param>
    /// <param name="request">The request containing paging information.</param>
    /// <returns>The query with paging applied.</returns>
    public virtual IQueryable<TDb> ApplyPaging(IQueryable<TDb> query, TRequest request)
    {
        if (request is IOrderedRequest orderedRequest)
        {
            if (!string.IsNullOrWhiteSpace(orderedRequest.OrderBy))
            {
                query = query.OrderBy($"{orderedRequest.OrderBy} {orderedRequest.DefaultOrderDirection ?? OrderDirection.Ascending}");
            }
        }

        return query;
    }

    /// <summary>
    /// Called after paging is applied to the query. Can be overridden to customize the final query and total entries.
    /// </summary>
    /// <param name="queryable">The queryable with paging applied.</param>
    /// <param name="request">The request containing paging information.</param>
    /// <param name="totalEntries">The total number of entries before paging.</param>
    /// <returns>The final queryable and total entries after paging is applied.</returns>
    public virtual (IQueryable<TDb> query, int totalEntries) OnPagingApply(IQueryable<TDb> queryable, TRequest request, int totalEntries)
    {
        return (queryable, totalEntries);
    }

    (IQueryable<TDb> query, int totalEntries) IPagedFilter<TRequest, TDb>.Apply(IQueryable<TDb> queryable, TRequest request)
    {
        var query = Apply(queryable, request);
        int totalEntries = query.Count();

        query = ApplyPaging(query, request);

        return OnPagingApply(query, request, totalEntries);
    }
}
