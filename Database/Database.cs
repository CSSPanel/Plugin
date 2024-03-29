using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CSSPanel
{
	public class Database
	{
		private readonly string _dbConnectionString;

		public Database(string dbConnectionString)
		{
			_dbConnectionString = dbConnectionString;
		}

		public async Task<MySqlConnection> GetConnectionAsync()
		{
			try
			{
				var connection = new MySqlConnection(_dbConnectionString);
				await connection.OpenAsync();
				return connection;
			}
			catch (Exception ex)
			{
				if (CSSPanel._logger != null)
					CSSPanel._logger.LogCritical($"Unable to connect to database: {ex.Message}");
				throw;
			}
		}
	}
}