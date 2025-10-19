using System;
using System.Configuration;
using Npgsql;
using System.Collections.Generic;

namespace consistent_system
{
    public class SensorDatabase
    {
        private readonly string _connectionString;

        public SensorDatabase(SensorName name)
        {
            string sensorName = name.ToString();
            _connectionString = ConfigurationManager.ConnectionStrings[sensorName].ConnectionString;
        }

        public void InsertMeasurement(double temperature)
        {
            string sql = "INSERT INTO measurements (temperature) VALUES (@temp)";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("temp", temperature);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public double? GetLastMeasurement()
        {
            string sql = "SELECT temperature FROM measurements ORDER BY id DESC LIMIT 1";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return null;

                    return Convert.ToDouble(result);
                }
            }
        }

        public List<double> GetAllMeasurements()
        {
            string sql = "SELECT temperature FROM measurements ORDER BY id ASC";
            var measurements = new List<double>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                measurements.Add(reader.GetDouble(0));
                        }
                    }
                }
            }

            return measurements;
        }

        public void ClearMeasurements()
        {
            string sql = "DELETE FROM measurements";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
