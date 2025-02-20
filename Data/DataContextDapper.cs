using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DotnetAPI.Data
{
    class DataContextDapper
    {
        private readonly IConfiguration _config;

        public DataContextDapper(IConfiguration config)
        {
            _config = config;
        }

        public IEnumerable<T> LoadData<T>(string sql)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            return dbConnection.Query<T>(sql);
        }

        public T LoadDataSingle<T>(string sql)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            return dbConnection.QuerySingle<T>(sql);
        }

        public bool ExecuteSql(string sql)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            return dbConnection.Execute(sql) > 0;
        }

        public int ExecuteSqlWithRowCount(string sql)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            return dbConnection.Execute(sql);
        }

        public bool ExecuteSqlWithParameters(string sql, List<SqlParameter> parameters)
        {

            SqlConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            SqlCommand commandWithParams = new(sql, dbConnection);
            foreach (SqlParameter param in parameters)
            {
                commandWithParams.Parameters.Add(param);
            }
            dbConnection.Open();
            int rowsAffected = commandWithParams.ExecuteNonQuery();
            dbConnection.Close();
            return rowsAffected > 0;
        }

        public bool ExecuteSqlWithDynamicParameters(string sql, DynamicParameters parameters)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            return dbConnection.Execute(sql, param: parameters) > 0;
        }

        public IEnumerable<T?> LoadDataWithParameters<T>(string sql, DynamicParameters parameters)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            return dbConnection.Query<T>(sql, param: parameters);
        }

        public T? LoadDataSingleWithParameters<T>(string sql, DynamicParameters parameters)
        {
            IDbConnection dbConnection = new SqlConnection(
                _config.GetConnectionString("DefaultConnection")
            );
            // QuerySingleOrDefault returns data or null, unlike the QuerySingle method
            return dbConnection.QuerySingleOrDefault<T>(sql, param: parameters);
        }
    }
}