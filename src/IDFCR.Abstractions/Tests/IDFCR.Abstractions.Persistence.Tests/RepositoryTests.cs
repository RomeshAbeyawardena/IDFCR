using IDFCR.Abstractions.Filters;
using IDFCR.Abstractions.Filters.Extensions;
using IDFCR.Abstractions.Interceptors;
using IDFCR.Abstractions.Interceptors.Factories;
using IDFCR.Abstractions.Interceptors.Interceptors;
using IDFCR.Abstractions.Persistence.Tests.Assets;
using IDFCR.Abstractions.Results.Extensions;
using IDFCR.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace IDFCR.Abstractions.Persistence.Tests;

[TestFixture]
public class RepositoryTests
{
    private IFilterFactory _filterFactory;
    private IEntityInterceptorFactory _entityInterceptorFactory;
    private InternalMemoryMockRepository<ICustomer, DbCustomer, Customer> _mockRepository;
    private IServiceProvider _serviceProvider;
    private FakeTimeProvider _timeProvider;

    [SetUp]
    public void SetUp()
    {
        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2025, 03, 1, 10, 40, 0, TimeSpan.Zero));

        var services = new ServiceCollection();

        services
            .AddSingleton<TimeProvider>(_timeProvider)
            .AddTransient<IEntityInterceptor, AuditCreatedTimestampEntityInterceptor>()
            .AddTransient<IEntityInterceptor, AuditModifiedTimestampEntityInterceptor>()
            .AddSingleton<IEntityInterceptorFactory, DefaultEntityInterceptorFactory>()
            .ScanFilters(true, typeof(RepositoryTests).Assembly)
            .AddGenericFilter(typeof(PagedGlobalFilter<,>));

        _serviceProvider = services.BuildServiceProvider();
        _entityInterceptorFactory = _serviceProvider.GetRequiredService<IEntityInterceptorFactory>();
        _filterFactory = _serviceProvider.GetRequiredService<IFilterFactory>();
        _mockRepository = new(_entityInterceptorFactory, _filterFactory, new DefaultScopedResources());

        new DbCustomer();
    }

    [TearDown]
    public void TearDown()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }

    [Test]
    public async Task AddAsync_Should_SetCreatedTimestamp_And_NotModified()
    {
        var entity = new Customer();

        var upsertResult = await _mockRepository.UpsertAsync(entity, CancellationToken.None);

        Assert.That(upsertResult.IsSuccess, Is.True, $"Internal error: {upsertResult.Exception?.Message}");

        var id = upsertResult.GetResultOrDefault();

        var stored = (await _mockRepository.FindAsync(id, CancellationToken.None)).GetResultOrDefault();

        Assert.That(stored, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored!.CreatedTimestampUtc,
                        Is.EqualTo(_timeProvider.GetUtcNow()));
            Assert.That(stored.ModifiedTimestampUtc, Is.Null);
        }
    }

    [Test]
    public async Task UpsertAsync_Update_Should_SetModifiedTimestamp_And_PreserveCreated()
    {
        var entity = new Customer();

        var insertResult = await _mockRepository.UpsertAsync(entity, CancellationToken.None);
        var id = insertResult.GetResultOrDefault();

        var stored = (await _mockRepository.FindAsync(id, CancellationToken.None)).GetResultOrDefault();
        stored?.FirstName = "Bob";
        var originalCreated = stored!.CreatedTimestampUtc;

        _timeProvider.Advance(TimeSpan.FromHours(1));

        var updateResult = await _mockRepository.UpsertAsync(stored, CancellationToken.None);

        Assert.That(updateResult.IsSuccess, Is.True, $"Internal error: {updateResult.Exception?.Message}");

        var updated = (await _mockRepository.FindAsync(id, CancellationToken.None)).GetResultOrDefault();
        Assert.That(updated, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.CreatedTimestampUtc, Is.EqualTo(originalCreated));
            Assert.That(updated.ModifiedTimestampUtc, Is.EqualTo(_timeProvider.GetUtcNow()));
        }
    }

    [Test]
    public async Task UpsertAsync_Update_WithDefaultCreatedTimestamp_ShouldPreserveCreatedTimestamp()
    {
        var insertResult = await _mockRepository.UpsertAsync(new Customer { FirstName = "Alice" }, CancellationToken.None);
        var id = insertResult.GetResultOrDefault();
        var originalCreatedTimestamp = _timeProvider.GetUtcNow();

        _timeProvider.Advance(TimeSpan.FromHours(1));

        var updateResult = await _mockRepository.UpsertAsync(new Customer
        {
            Id = id,
            FirstName = "Bob",
            CreatedTimestampUtc = default
        }, CancellationToken.None);

        Assert.That(updateResult.IsSuccess, Is.True, $"Internal error: {updateResult.Exception?.Message}");

        var updated = (await _mockRepository.FindAsync(id, CancellationToken.None)).GetResultOrDefault();
        Assert.That(updated, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated!.CreatedTimestampUtc, Is.EqualTo(originalCreatedTimestamp));
            Assert.That(updated.ModifiedTimestampUtc, Is.EqualTo(_timeProvider.GetUtcNow()));
        }
    }

    [Test]
    public async Task GetPaged_Should_Filter_By_NameContains_And_Return_Correct_Total()
    {
        await _mockRepository.UpsertAsync(new Customer { FirstName = "Alice", LastName = "Smith" }, default);
        await _mockRepository.UpsertAsync(new Customer { FirstName = "Bob", LastName = "Jones" }, default);
        await _mockRepository.UpsertAsync(new Customer { FirstName = "Alicia", LastName = "Brown" }, default);
        await _mockRepository.UpsertAsync(new Customer { FirstName = "Alicia", LastName = "Thomas", Suppressed = true }, default);
        await _mockRepository.UpsertAsync(new Customer { FirstName = "Aliyanna", LastName = "Humphreys", Suppressed = true }, default);

        var request = new PagedCustomerRequest
        {
            NameContains = "Ali",
            PageIndex = 0,
            PageSize = 10
        };

        var result = await _mockRepository.GetPagedAsync(request, default);

        Assert.That(result.IsSuccess, Is.True);

        var data = result.GetResultOrDefault()?.ToArray();

        Assert.That(data, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data, Has.Length.EqualTo(2));
            Assert.That(result.TotalRows, Is.EqualTo(2));
        }

        Assert.That(data.All(x => x.FirstName.Contains("Ali")), Is.True);
    }

    [Test]
    public async Task UpsertAsync_Update_WithNoChanges_ShouldFail()
    {
        var result = await _mockRepository.UpsertAsync(new Customer { FirstName = "Aliyanna", LastName = "Humphreys", Suppressed = true }, default);

        Assert.That(result.IsSuccess, Is.True);

        result = await _mockRepository.UpsertAsync(new Customer { Id = result.Result, FirstName = "Aliyanna", LastName = "Humphreys", Suppressed = true }, default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());
        }
    }
}
