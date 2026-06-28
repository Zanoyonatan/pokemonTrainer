using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;

namespace pokemonTrainer.Services;

public class DatabaseAvailabilityService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseAvailabilityService> _logger;

    public DatabaseAvailabilityService(
        ApplicationDbContext dbContext,
        ILogger<DatabaseAvailabilityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return result;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsDatabaseUnavailableException(Exception exception)
    {
        // Walk through exception and all inner exceptions
        var current = exception;
        while (current != null)
        {
            // Check for SQL Server specific exceptions
            if (current is SqlException)
            {
                return true;
            }

            // Check for generic database exceptions
            if (current is System.Data.Common.DbException)
            {
                return true;
            }

            // Check for Entity Framework Core specific exceptions
            if (current is InvalidOperationException && 
                current.Message.Contains("database", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for timeout/connection failures
            if (current is TimeoutException)
            {
                return true;
            }

            if (current is OperationCanceledException &&
                current.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
