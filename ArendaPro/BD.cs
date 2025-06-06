using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

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

        // SELECT с параметрами (например, @from и @to)
        public DataTable ExecuteQuery(string sql, Dictionary<string, object> parameters)
        {
            var dt = new DataTable();
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
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

    }
}
