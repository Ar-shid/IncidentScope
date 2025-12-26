using ClickHouse.Client.ADO;

namespace IncidentScope.Data.ClickHouse;

public class ClickHouseService
{
    private readonly ClickHouseConnection _connection;

    public ClickHouseService(string connectionString)
    {
        _connection = new ClickHouseConnection(connectionString);
    }

    public async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        await _connection.OpenAsync(cancellationToken);
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        Func<System.Data.IDataReader, T> mapper,
        CancellationToken cancellationToken = default)
    {
        await _connection.OpenAsync(cancellationToken);
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(mapper(reader));
        }
        return results;
    }
}

