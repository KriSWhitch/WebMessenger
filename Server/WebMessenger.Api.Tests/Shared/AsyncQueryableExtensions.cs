using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace WebMessenger.Api.Tests.Shared;

/// <summary>
/// Enables EF Core async LINQ operations (AnyAsync, FirstOrDefaultAsync, ToListAsync, etc.)
/// on plain in-memory <see cref="IQueryable{T}"/> mocks.
///
/// Usage:
///   var data = new[] { user }.AsQueryable();
///   repoMock.Setup(r => r.GetAll()).Returns(data.AsAsyncQueryable());
/// </summary>
internal static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source) =>
        new TestAsyncEnumerable<T>(source.AsQueryable());
}

internal sealed class TestAsyncEnumerable<T>(IQueryable<T> queryable)
    : EnumerableQuery<T>(queryable.Expression), IQueryable<T>, IAsyncEnumerable<T>
{
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(queryable.Provider);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
}

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>((IQueryable<TEntity>)inner.CreateQuery<TEntity>(expression));
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(inner.CreateQuery<TElement>(expression));
    public object? Execute(Expression expression) => inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // TResult is Task<TValue>; extract TValue, execute sync, wrap in Task.FromResult
        var resultType  = typeof(TResult).GetGenericArguments()[0];
        var syncResult  = inner.Execute(expression);
        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [syncResult])!;
    }
}
