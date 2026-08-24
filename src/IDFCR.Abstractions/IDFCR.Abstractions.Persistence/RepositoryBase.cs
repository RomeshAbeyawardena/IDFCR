using IDFCR.Abstractions.Interceptors;
using IDFCR.Abstractions.Interceptors.Factories;
using IDFCR.Abstractions.Interceptors.Interceptors;
using IDFCR.Abstractions.Mapper;
using IDFCR.Abstractions.Metadata;
using IDFCR.Abstractions.Persistence.Extensions;
using IDFCR.Abstractions.Results;
using IDFCR.Abstractions.Results.Exceptions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IDFCR.Abstractions.Persistence
{
    /// <summary>
    /// Base class for repositories that map between a common abstraction, a database model, and a domain model.
    /// </summary>
    /// <typeparam name="TCommon">The shared abstraction implemented by both models.</typeparam>
    /// <typeparam name="TDb">The persistence model type.</typeparam>
    /// <typeparam name="T">The domain model type.</typeparam>
    /// <typeparam name="TKey">The identifier type.</typeparam>
    /// <param name="entityInterceptorFactory">The interceptor factory used to resolve repository interceptors.</param>
    public abstract class RepositoryBase<TCommon, TDb, T, TKey>(IEntityInterceptorFactory entityInterceptorFactory) : IRepository<T, TKey>
        where TKey : struct
        where TDb : class, IMapper<TCommon>, TCommon, IIdentifiable<TKey>
        where T : class, IMapper<TCommon>, TCommon
    {
        /// <summary>
        /// 
        /// </summary>
        protected IEntityInterceptorFactory EntityInterceptorFactory => entityInterceptorFactory;

        /// <summary>
        /// Gets the computed entity name based on the domain model type. This is used for generating consistent result metadata and can be overridden by derived classes to provide a custom name if needed.
        /// </summary>
        protected virtual string ComputedEntityName => typeof(T).Name;

        /// <summary>
        /// Gets or sets the custom entity name. This can be used to override the computed entity name for generating consistent result metadata.
        /// </summary>
        protected string? EntityName { get; set; }

        private async Task<IUnitResult<T>> WrapFindResult(Func<CancellationToken, Task<TDb?>> onFindAsync, object key, CancellationToken cancellationToken)
        {
            Exception? caughtException;
            bool success = false;
            T? value = null;
            try
            {
                var result = await onFindAsync(cancellationToken);

                if (result is not null)
                {
                    value = Map(result);
                    success = value is not null;
                    caughtException = value is null ? new EntityNotFoundException(typeof(T), key) : null;
                }
                else
                {
                    caughtException = new EntityNotFoundException(typeof(T), key);
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            return UnitResult.FromResult(value, UnitAction.Get, success, caughtException);
        }

        private async Task<RepositoryInterceptorContext> InvokeInterceptorsAsync(EntityContextBehaviorStage stage,
            EntityContextBehavior behavior,
            object model, object? oldModel, CancellationToken cancellationToken)
        {
            var context = RepositoryInterceptorContext.Create(stage, behavior, model);

            if (oldModel is not null)
            {
                context = RepositoryInterceptorContext.Create(stage, behavior, model,
                    b => b.AddOrUpdate(AuditEntityChangesInterceptor.OldDataKey, oldModel));
            }

            var interceptors = await entityInterceptorFactory.GetEntityInterceptorsAsync(context, cancellationToken);
            await entityInterceptorFactory.InvokeAsync(interceptors, context, cancellationToken);

            return context;
        }

        /// <summary>
        /// Maps the domain model to the database model.
        /// </summary>
        protected virtual TDb? Map(T value)
        {
            return value.Map<TDb>();
        }

        /// <summary>
        /// Maps the database model to the domain model.
        /// </summary>
        protected virtual T? Map(TDb value)
        {
            return value.Map<T>();
        }

        /// <summary>
        /// Determines whether there are changes between the original and updated entities. By default, this performs a deep comparison of the serialized JSON representations of the objects, but can be overridden for more efficient change detection based on the specific properties of the entities.
        /// <para>As it this does a deep comparison it will compare collections that may not be relevant for change detection. Overriding this method is recommended when the data model contains large or complex collections that do not impact the entity's state.</para>
        /// </summary>
        /// <param name="original">The original entity.</param>
        /// <param name="updated">The updated entity.</param>
        /// <returns>True if there are changes; otherwise, false.</returns>
        protected virtual bool HasChanges(TDb original, TDb updated)
        {
            var originalNode = JsonSerializer.SerializeToNode(original);
            var updatedNode = JsonSerializer.SerializeToNode(updated);

            if (originalNode?.AsObject() is JsonObject originalObj && updatedNode?.AsObject() is JsonObject updatedObj)
            {
                // Remove audit timestamp fields before comparison
                originalObj.Remove("CreatedTimestampUtc");
                originalObj.Remove("ModifiedTimestampUtc");
                updatedObj.Remove("CreatedTimestampUtc");
                updatedObj.Remove("ModifiedTimestampUtc");
            }

            return !JsonNode.DeepEquals(originalNode, updatedNode);
        }


        /// <summary>
        /// Persists a new record.
        /// </summary>
        protected abstract Task<TKey> OnAddAsync(TDb entry, T rawEntry, CancellationToken cancellationToken);
        /// <summary>
        /// Persists an updated record.
        /// </summary>
        protected abstract Task<TKey> OnUpdateAsync(TDb entry, T rawEntry, CancellationToken cancellationToken);
        /// <summary>
        /// Finds a record by primary key.
        /// </summary>
        protected abstract Task<TDb?> OnFindAsync(TKey key, bool trackChanges, CancellationToken cancellationToken);
        /// <summary>
        /// Finds a record by composite keys.
        /// </summary>
        protected abstract Task<TDb?> OnFindAsync(object[] keys, bool trackChanges, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a record by its primary key.
        /// </summary>
        protected abstract Task<bool> OnDeleteAsync(TKey key, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the data and total row count for a paged query.
        /// </summary>
        protected abstract Task<(IEnumerable<TDb> data, int totalRows)> OnGetPagedAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
            where TRequest : IPagedQuery;

        /// <summary>
        /// Determines whether an exception is handled by the repository.
        /// </summary>
        protected abstract bool IsHandled(Exception exception);

        /// <inheritdoc />
        public async Task<IUnitResult> DeleteAsync(TKey key, CancellationToken cancellationToken)
        {
            Exception? caughtException = null;
            bool success = false;
            try
            {
                var dbValue = await OnFindAsync(key, true, cancellationToken);

                if (dbValue is null)
                {
                    return UnitResult.NotFound<TKey>(key, caughtException);
                }

                var context = await InvokeInterceptorsAsync(EntityContextBehaviorStage.Pre,
                        EntityContextBehavior.Delete, dbValue, null, cancellationToken);

                if (context.BypassOperation)
                {
                    return UnitResult.FromResult(key, UnitAction.None)
                            .AddMeta("bypassed", true).As<TKey>();
                }

                success = await OnDeleteAsync(key, cancellationToken);

                await InvokeInterceptorsAsync(EntityContextBehaviorStage.Post,
                        EntityContextBehavior.Delete, dbValue, null, cancellationToken);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            return UnitResult.FromResult(key, UnitAction.Delete, success, caughtException);
        }

        /// <inheritdoc />
        public Task<IUnitResult<T>> FindAsync(object[] keys, CancellationToken cancellationToken)
        {
            return WrapFindResult(ct => OnFindAsync(keys, false, ct), keys, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IUnitResult<T>> FindAsync(TKey key, CancellationToken cancellationToken)
        {
            return WrapFindResult(ct => OnFindAsync(key, false, ct), key, cancellationToken);
        }

        /// <summary>
        /// On add hook for performing actions before or after the add operation. The raw entry is provided for reference but should not be modified.
        /// </summary>
        /// <param name="db">The database entity.</param>
        /// <param name="dto">The data transfer object.</param>
        protected abstract void OnUpdate(TDb db, T dto);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract Task OnReloadEntityAsync(TDb entity);

        /// <inheritdoc />
        public virtual async Task<IUnitResult<TKey>> UpsertAsync(T entry, CancellationToken cancellationToken)
        {
            TDb? foundEntry = null;
            try
            {
                string resultName = $"{EntityName ?? ComputedEntityName}Id";
                var valueToApply = Map(entry) ?? throw new InvalidOperationException($"Mapping from {typeof(T)} to {typeof(TDb)} failed");
                RepositoryInterceptorContext context;
                TKey id;

                if (EqualityComparer<TKey>.Default.Equals(valueToApply.Id, default))
                {
                    context = await InvokeInterceptorsAsync(EntityContextBehaviorStage.Pre,
                        EntityContextBehavior.Insert, valueToApply, null, cancellationToken);

                    if (context.BypassOperation)
                    {
                        return UnitResult.FromResult(default(TKey), UnitAction.None)
                            .AddMeta("bypassed", true).As<TKey>();
                    }

                    id = await OnAddAsync(valueToApply, entry, cancellationToken);

                    await InvokeInterceptorsAsync(EntityContextBehaviorStage.Post,
                        EntityContextBehavior.Insert, valueToApply, null, cancellationToken);

                    var addedResult = UnitResult.FromResult(id, UnitAction.Add, namedResult: resultName);

                    addedResult.AddMeta(Meta.CurrentEntityState, Map(valueToApply));

                    return addedResult;
                }

                foundEntry = await OnFindAsync(valueToApply.Id, true, cancellationToken);

                if (foundEntry is null)
                {
                    return UnitResult.NotFound<TKey>(valueToApply.Id, new EntityNotFoundException(typeof(T), valueToApply.Id));
                }

                var oldEntry = foundEntry.Map<TDb>() ?? throw new NullReferenceException(ErrorMessages.MappingFailure);

                var clonedEntity = foundEntry.Map<TDb>() ?? throw new NullReferenceException(ErrorMessages.MappingFailure);
                clonedEntity.Apply(valueToApply);

                if (!HasChanges(clonedEntity, foundEntry))
                {
                    return UnitResult.Failed(new InvalidOperationException(ErrorMessages.HasNoChanges), 
                        UnitAction.Update, FailureReason.Conflict, defaultValue: clonedEntity.Id);
                }

                OnUpdate(foundEntry, entry);

                foundEntry.Apply(valueToApply);

                context = await InvokeInterceptorsAsync(EntityContextBehaviorStage.Pre,
                    EntityContextBehavior.Update, foundEntry, clonedEntity, cancellationToken);

                if (context.BypassOperation)
                {
                    return UnitResult.FromResult(valueToApply.Id, UnitAction.None)
                        .AddMeta("bypassed", true).As<TKey>();
                }

                id = await OnUpdateAsync(foundEntry, entry, cancellationToken);
                await InvokeInterceptorsAsync(EntityContextBehaviorStage.Post,
                    EntityContextBehavior.Update, foundEntry, oldEntry, cancellationToken);

                var result = UnitResult.FromResult(id, UnitAction.Update, namedResult: resultName);

                result.AddMeta(Meta.CurrentEntityState, Map(valueToApply));
                return result;
            }
            catch (Exception exception)
            {
                if (foundEntry is not null)
                {
                    await OnReloadEntityAsync(foundEntry);
                }

                if (IsHandled(exception))
                {
                    return UnitResult.Failed<TKey>(exception);
                }

                return UnitResult.Failed<TKey>(exception).AddMeta("unexpected", "true").As<TKey>();
            }
        }

        /// <summary>
        /// Retrieves a paged result based on the specified request.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="request">The request object containing pagination parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the paged result.</returns>
        /// <exception cref="InvalidOperationException">Thrown when mapping fails.</exception>
        protected async Task<IPagedUnitResult<TResult>> GetPagedAsync<TRequest, TResult>(TRequest request, CancellationToken cancellationToken) 
            where TRequest : IPagedQuery
            where TResult : class, IMapper<TCommon>
        {
            var (data, totalRows) = await OnGetPagedAsync(request, cancellationToken);

            var mappedData = data.Select(x => x.Map<TResult>()!) ?? throw new InvalidOperationException($"Mapping from {typeof(T)} to {typeof(TDb)} failed");

            return PagedUnitResult.FromResult(mappedData!,
                totalRows, request, UnitAction.Get);
        }

        /// <inheritdoc />
        public async virtual Task<IPagedUnitResult<T>> GetPagedAsync<TRequest>(TRequest request, CancellationToken cancellationToken) where TRequest : IPagedQuery
        {
            var (data, totalRows) = await OnGetPagedAsync(request, cancellationToken);

            var mappedData = data.Select(Map!) ?? throw new InvalidOperationException($"Mapping from {typeof(T)} to {typeof(TDb)} failed");

            return PagedUnitResult.FromResult<T>(mappedData!,
                totalRows, request, UnitAction.Get);
        }

        /// <inheritdoc />
        public abstract Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
