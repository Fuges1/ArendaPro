using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Data.SqlClient;
namespace ArendaPro
{
    // Логика класса: BD инкапсулирует соответствующий экран/сервис и его сценарии работы.
    internal class BD
    {
        private readonly string connectionString;

        public BD(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Логика: метод GetConnection выполняет соответствующий шаг бизнес-процесса этого окна/сервиса.
        public SqlConnection GetConnection() => new(connectionString);

        public DataTable ExecuteQuery(string query)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(query, conn);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public DataTable ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            AddDictionaryParameters(cmd, parameters);

            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            AddDictionaryParameters(cmd, parameters);

            return cmd.ExecuteNonQuery();
        }

        public T ExecuteScalar<T>(string sql, Dictionary<string, object> parameters)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    AddDictionaryParameters(cmd, parameters, convertDateTimeToDate: true);

                    try
                    {
                        var result = cmd.ExecuteScalar();
                        return (T)Convert.ChangeType(result, typeof(T));
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText("sql_errors.log",
                            $"\n[{DateTime.Now}] SQL: {sql}\nParams: {string.Join(", ", parameters)}\nError: {ex}\n");
                        throw;
                    }
                }
            }
        }

        public SqlTransaction BeginTransaction()
        {
            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn.BeginTransaction();
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, object parameters = null)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null)
            {
                AddObjectParameters(cmd, parameters, addAtPrefix: true);
            }

            var result = await cmd.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, object parameters = null)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null)
            {
                AddObjectParameters(cmd, parameters, addAtPrefix: true);
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, object parameters = null)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null)
            {
                AddObjectParameters(cmd, parameters, addAtPrefix: true);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        private static void AddDictionaryParameters(SqlCommand command, Dictionary<string, object> parameters, bool convertDateTimeToDate = false)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (var param in parameters)
            {
                var sqlParam = command.CreateParameter();
                sqlParam.ParameterName = NormalizeParameterName(param.Key);
                if (convertDateTimeToDate && param.Value is DateTime dateValue)
                {
                    sqlParam.Value = dateValue.Date;
                    sqlParam.SqlDbType = SqlDbType.Date;
                }
                else
                {
                    sqlParam.Value = param.Value ?? DBNull.Value;
                }

                command.Parameters.Add(sqlParam);
            }
        }

        private static void AddObjectParameters(SqlCommand command, object parameters, bool addAtPrefix)
        {
            var props = parameters.GetType().GetProperties();
            foreach (var prop in props)
            {
                var name = addAtPrefix ? $"@{prop.Name}" : prop.Name;
                command.Parameters.AddWithValue(name, prop.GetValue(parameters) ?? DBNull.Value);
            }
        }

        private static string NormalizeParameterName(string parameterName)
            => parameterName.StartsWith("@", StringComparison.Ordinal) ? parameterName : "@" + parameterName;

    }
}
