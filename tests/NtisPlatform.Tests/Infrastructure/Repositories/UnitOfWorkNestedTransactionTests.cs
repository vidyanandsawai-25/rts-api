using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Regression tests for the nested-transaction bug: UnitOfWork is scoped (one instance per
/// request), so a caller that opens a transaction and then triggers other application code
/// synchronously in-process (e.g. certificate save publishing PropertyCertificateChangedEvent,
/// which the RV recalculation pipeline handles inline via MediatR) can end up calling
/// BeginTransactionAsync a second time on the SAME connection. EF Core's InMemory provider can't
/// exercise real transactions at all, so these use SQLite in-memory (matching the existing
/// pattern in DataEntryIntegrationTests.cs) to prove the actual commit/rollback semantics.
/// </summary>
[Collection("Sequential")]
public class UnitOfWorkNestedTransactionTests
{
    private static (ApplicationDbContext Context, UnitOfWork UnitOfWork) CreateSqliteContext(SqliteConnection connection)
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);

        // EnsureCreated() generates the full ApplicationDbContext schema (100+ entities) in one
        // script; at least one existing CHECK constraint elsewhere in that schema uses T-SQL
        // syntax SQLite's parser rejects, unrelated to anything under test here. These tests only
        // need WardMaster, so create just that table directly instead of the whole model.
        context.Database.ExecuteSqlRaw(
            "CREATE TABLE WardMaster (" +
            "Id INTEGER PRIMARY KEY AUTOINCREMENT, WardNo TEXT NOT NULL, ZoneId INTEGER NOT NULL, " +
            "Description TEXT, SequenceNo INTEGER, IsActive INTEGER NOT NULL DEFAULT 1, " +
            "CreatedDate TEXT, UpdatedDate TEXT, CreatedBy INTEGER, UpdatedBy INTEGER);");

        return (context, new UnitOfWork(context));
    }

    [Fact]
    public async Task BeginTransactionAsync_CalledWhileAlreadyInTransaction_JoinsAmbientTransaction_DoesNotThrow()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var (context, unitOfWork) = CreateSqliteContext(connection);
        await using var _ = context;

        await unitOfWork.BeginTransactionAsync();

        // Simulates a nested caller (e.g. an event handler triggered synchronously from within
        // the outer transaction) trying to begin its own transaction on the SAME scoped
        // UnitOfWork -- must not throw "connection already in a transaction".
        var exception = await Record.ExceptionAsync(() => unitOfWork.BeginTransactionAsync());
        Assert.Null(exception);

        await unitOfWork.CommitTransactionAsync();
    }

    [Fact]
    public async Task NestedCommitTransactionAsync_DoesNotFinalizeUntilOutermostCommits()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var (context, unitOfWork) = CreateSqliteContext(connection);
        await using var _ = context;

        await unitOfWork.BeginTransactionAsync(); // outer
        context.WardMaster.Add(new WardEntity { WardNo = "OUTER", ZoneId = 1, IsActive = true });
        await unitOfWork.SaveChangesAsync();

        await unitOfWork.BeginTransactionAsync(); // nested join
        context.WardMaster.Add(new WardEntity { WardNo = "INNER", ZoneId = 1, IsActive = true });
        await unitOfWork.CommitTransactionAsync(); // nested "commit" -- must only flush, not finalize

        // If the nested commit had (incorrectly) finalized the physical transaction, this
        // rollback would either throw or silently no-op, leaving both rows committed. Since the
        // physical transaction must still be open, this rolls back BOTH rows.
        await unitOfWork.RollbackTransactionAsync();

        var count = await context.WardMaster.CountAsync(w => w.WardNo == "OUTER" || w.WardNo == "INNER");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task NestedRollbackTransactionAsync_TearsDownWholeAmbientTransaction()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var (context, unitOfWork) = CreateSqliteContext(connection);
        await using var _ = context;

        await unitOfWork.BeginTransactionAsync(); // outer
        context.WardMaster.Add(new WardEntity { WardNo = "OUTER", ZoneId = 1, IsActive = true });
        await unitOfWork.SaveChangesAsync();

        await unitOfWork.BeginTransactionAsync(); // nested join
        await unitOfWork.RollbackTransactionAsync(); // nested failure -- tears down the whole ambient transaction

        // The outermost caller's own eventual Commit must not throw or resurrect anything -- it
        // should simply no-op since the transaction is already gone.
        var commitException = await Record.ExceptionAsync(() => unitOfWork.CommitTransactionAsync());
        Assert.Null(commitException);

        var count = await context.WardMaster.CountAsync(w => w.WardNo == "OUTER");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task BeginThenCommitTwice_OutermostCommitPersistsData()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var (context, unitOfWork) = CreateSqliteContext(connection);
        await using var _ = context;

        await unitOfWork.BeginTransactionAsync(); // outer
        await unitOfWork.BeginTransactionAsync(); // nested join

        context.WardMaster.Add(new WardEntity { WardNo = "PERSISTED", ZoneId = 1, IsActive = true });

        await unitOfWork.CommitTransactionAsync(); // nested -- flush only
        await unitOfWork.CommitTransactionAsync(); // outermost -- actually commits

        var saved = await context.WardMaster.FirstOrDefaultAsync(w => w.WardNo == "PERSISTED");
        Assert.NotNull(saved);
    }
}
