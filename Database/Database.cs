using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CSSPanel.Database;

public class Database(string dbConnectionString)
{
	public MySqlConnection GetConnection()
	{
		try
		{
			var connection = new MySqlConnection(dbConnectionString);
			connection.Open();
			return connection;
		}
		catch (Exception ex)
		{
			CSSPanel._logger?.LogCritical($"Unable to connect to database: {ex.Message}");
			throw;
		}
	}

	public async Task<MySqlConnection> GetConnectionAsync()
	{
		try
		{
			var connection = new MySqlConnection(dbConnectionString);
			await connection.OpenAsync();
			return connection;
		}
		catch (Exception ex)
		{
			CSSPanel._logger?.LogCritical($"Unable to connect to database: {ex.Message}");
			throw;
		}
	}

	public bool CheckDatabaseConnection()
	{
		using var connection = GetConnection();

		try
		{
			return connection.Ping();
		}
		catch
		{
			return false;
		}
	}
}
