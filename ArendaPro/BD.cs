using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
namespace ArendaPro
{
    internal class BD
    {
        private readonly string connectionString;

        public BD(string connectionString)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public NpgsqlConnection GetConnection() => new NpgsqlConnection(connectionString);

        public DataTable ExecuteQuery(string query)
        {
            var dt = new DataTable();
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(query, conn);
            using var adapter = new NpgsqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public DataTable ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            var dt = new DataTable();
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(sql, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            return cmd.ExecuteNonQuery();
        }

        public T ExecuteScalar<T>(string sql, Dictionary<string, object> parameters)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var param in parameters)
                    {
                        var pgParam = cmd.CreateParameter();
                        pgParam.ParameterName = param.Key;

                        if (param.Value is DateTime dateValue)
                        {
                            pgParam.Value = dateValue.Date;
                            pgParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Date;
                        }
                        else
                        {
                            pgParam.Value = param.Value ?? DBNull.Value;
                        }

                        cmd.Parameters.Add(pgParam);
                    }

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

        public NpgsqlTransaction BeginTransaction()
        {
            var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            return conn.BeginTransaction();
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, object parameters = null)
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);

            if (parameters != null)
            {
                var props = parameters.GetType().GetProperties();
                foreach (var prop in props)
                {
                    cmd.Parameters.AddWithValue(prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
                }
            }

            var result = await cmd.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, object parameters = null)
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);

            if (parameters != null)
            {
                var props = parameters.GetType().GetProperties();
                foreach (var prop in props)
                {
                    cmd.Parameters.AddWithValue(prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
                }
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, object parameters = null)
        {
            var dt = new DataTable();
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);

            if (parameters != null)
            {
                var props = parameters.GetType().GetProperties();
                foreach (var prop in props)
                {
                    cmd.Parameters.AddWithValue(prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
                }
            }

            using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

    }
}