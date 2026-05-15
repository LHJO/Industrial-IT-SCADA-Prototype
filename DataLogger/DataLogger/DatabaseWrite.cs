using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DataLogger
{
    public class DatabaseWriter : IDisposable
    {
        private readonly string _connectionString;

        public DatabaseWriter(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Writes OPC UA measurement data to both Measurement and CurrentMeasurement tables
        /// </summary>
        public async Task WriteDataAsync(List<DataPoint> dataPoints)
        {
            if (dataPoints?.Count == 0)
                return;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var dataPoint in dataPoints)
                    {
                        try
                        {
                            // Get OpcDatapointId by OPC Node ID
                            var opcDatapointId = await GetOpcDatapointIdByNodeIdAsync(connection, dataPoint.NodeId);

                            if (opcDatapointId.HasValue)
                            {
                                // Insert into Measurement table (historical)
                                await InsertMeasurementAsync(connection, opcDatapointId.Value, dataPoint);

                                // Update CurrentMeasurement table (latest value)
                                await UpdateCurrentMeasurementAsync(connection, opcDatapointId.Value, dataPoint);

                            }
                            else
                            {
                                Console.WriteLine($"[DB] ✗ Not mapped: {dataPoint.DisplayName} ({dataPoint.NodeId})");
                            }
                        }
                        catch (Exception itemEx)
                        {
                            Console.WriteLine($"[DB] ✗ Error writing {dataPoint.DisplayName}: {itemEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] ✗ Database connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets OpcDatapointId by OPC Node ID
        /// </summary>
        private async Task<int?> GetOpcDatapointIdByNodeIdAsync(SqlConnection connection, string opcNodeId)
        {
            const string query = @"
                SELECT OpcDatapointId
                FROM dbo.OpcDatapoints
                WHERE OpcNodeId = @OpcNodeId
                AND IsActive = 1";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@OpcNodeId", SqlDbType.VarChar).Value = opcNodeId ?? "";
                var result = await command.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);

                return null;
            }
        }

        /// <summary>
        /// Inserts a measurement record into the Measurement table (historical data)
        /// </summary>
        private async Task InsertMeasurementAsync(SqlConnection connection, int opcDatapointId, DataPoint dataPoint)
        {
            const string query = @"
                INSERT INTO dbo.Measurement 
                    (OpcDatapointId, MeasurementValue, MeasurementText, StatusCode, SourceTimestamp, ServerTimestamp, MeasurementTimeStamp)
                VALUES 
                    (@OpcDatapointId, @MeasurementValue, @MeasurementText, @StatusCode, @SourceTimestamp, @ServerTimestamp, @MeasurementTimeStamp)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@OpcDatapointId", SqlDbType.Int).Value = opcDatapointId;
                command.Parameters.Add("@MeasurementValue", SqlDbType.Float).Value = ConvertToDouble(dataPoint.Value) ?? (object)DBNull.Value;
                command.Parameters.Add("@MeasurementText", SqlDbType.VarChar).Value = dataPoint.Value?.ToString() ?? (object)DBNull.Value;
                command.Parameters.Add("@StatusCode", SqlDbType.VarChar).Value = dataPoint.Quality ?? (object)DBNull.Value;
                command.Parameters.Add("@SourceTimestamp", SqlDbType.DateTime2).Value = dataPoint.Timestamp ?? (object)DBNull.Value;
                command.Parameters.Add("@ServerTimestamp", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                command.Parameters.Add("@MeasurementTimeStamp", SqlDbType.DateTime2).Value = DateTime.UtcNow;

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Updates or inserts the latest measurement value into the CurrentMeasurement table
        /// </summary>
        private async Task UpdateCurrentMeasurementAsync(SqlConnection connection, int opcDatapointId, DataPoint dataPoint)
        {
            const string query = @"
                MERGE INTO dbo.CurrentMeasurement AS target
                USING (VALUES (@OpcDatapointId)) AS source(OpcDatapointId)
                ON target.OpcDatapointId = source.OpcDatapointId
                WHEN MATCHED THEN
                    UPDATE SET 
                        MeasurementValue = @MeasurementValue,
                        MeasurementText = @MeasurementText,
                        StatusCode = @StatusCode,
                        SourceTimestamp = @SourceTimestamp,
                        ServerTimestamp = @ServerTimestamp,
                        LastUpdated = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (OpcDatapointId, MeasurementValue, MeasurementText, StatusCode, SourceTimestamp, ServerTimestamp, LastUpdated)
                    VALUES (@OpcDatapointId, @MeasurementValue, @MeasurementText, @StatusCode, @SourceTimestamp, @ServerTimestamp, SYSUTCDATETIME());";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@OpcDatapointId", SqlDbType.Int).Value = opcDatapointId;
                command.Parameters.Add("@MeasurementValue", SqlDbType.Float).Value = ConvertToDouble(dataPoint.Value) ?? (object)DBNull.Value;
                command.Parameters.Add("@MeasurementText", SqlDbType.VarChar).Value = dataPoint.Value?.ToString() ?? (object)DBNull.Value;
                command.Parameters.Add("@StatusCode", SqlDbType.VarChar).Value = dataPoint.Quality ?? (object)DBNull.Value;
                command.Parameters.Add("@SourceTimestamp", SqlDbType.DateTime2).Value = dataPoint.Timestamp ?? (object)DBNull.Value;
                command.Parameters.Add("@ServerTimestamp", SqlDbType.DateTime2).Value = DateTime.UtcNow;

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Gets the latest measurement value for a specific sensor
        /// </summary>
        public async Task<double?> GetLatestMeasurementAsync(int opcDatapointId)
        {
            const string sql = @"
                SELECT MeasurementValue
                FROM dbo.CurrentMeasurement
                WHERE OpcDatapointId = @OpcDatapointId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.Add("@OpcDatapointId", SqlDbType.Int).Value = opcDatapointId;
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                        return null;

                    return Convert.ToDouble(result);
                }
            }
        }

        /// <summary>
        /// Convert object to double
        /// </summary>
        private static double? ConvertToDouble(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is double d)
                return d;

            if (value is int i)
                return Convert.ToDouble(i);

            if (double.TryParse(value.ToString(), out double result))
                return result;

            return null;
        }

        public void Dispose()
        {
        }
    }
}
