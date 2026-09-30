using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ReportV2.Infrastructure.Sql;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HisDb")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? string.Empty;
    }

    public SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Connection string 'HisDb' is not configured.");
        }

        return new SqlConnection(_connectionString);
    }
}
