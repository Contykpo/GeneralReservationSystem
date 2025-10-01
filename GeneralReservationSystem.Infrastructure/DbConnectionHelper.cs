using GeneralReservationSystem.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using static GeneralReservationSystem.Application.Common.OperationResult;
using static GeneralReservationSystem.Application.Common.OptionalResult<object>;

namespace GeneralReservationSystem.Infrastructure
{
    public class DbConnectionHelper
    {
        //TODO: Agregar asserts a todos los execute para verificar que no se pasen variables nulas
        //TODO: Mucha repeticion de codigo en los Execute, ver de crear un metodo generico que reciba un delegate

        private readonly string _connectionString;
        private readonly ILogger<DbConnectionHelper> _logger;

        public DbConnectionHelper(IConfiguration configuration, ILogger<DbConnectionHelper> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;

            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentNullException("No se encontró la cadena de conexión 'DefaultConnection'.");

            _logger.LogDebug("Using connection string: {ConnectionString}", _connectionString);
        }

        private SqlCommand CreateCommand(string sql, SqlConnection connection, Dictionary<string, object>? parameters = null, SqlTransaction? transaction = null)
        {
            var command = 
                transaction == null ? new SqlCommand(sql, connection) 
                                    : new SqlCommand(sql, connection, transaction);

            if (parameters != null)
                SetCommandParameters(command, parameters);

            return command;
        }

        public async Task<OptionalResult<int>> ExecuteAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                return await ExecuteAsync(sql, connection, parameters);
            }
            catch(Exception ex)
            {              
                _logger.LogError(ex, $"Error al crear conexion con la base de datos");
                return Error<int>($"Error al crear conexion con la base de datos");
            }
        }

        public async Task<OptionalResult<int>> ExecuteAsync(string sql, SqlConnection connection, Dictionary<string, object>? parameters = null, SqlTransaction? transaction = null)
        {
            try
            {
                using var command = CreateCommand(sql, connection, parameters, transaction);

                return Value(await command.ExecuteNonQueryAsync());
            }
            catch (Exception ex)
            {
                //En release no logueamos parametros por seguridad
                _logger.LogError(ex, $"Error al ejecutar comando SQL: {sql}");

                _logger.LogDebug($"Error al ejecutar comando SQL: {sql} con parametros: {parameters}");

                // NOTA: Es útil devolver el mensaje de error SQL para poder devolver mensajes de error de
                // constraint violations hacia el usuario.
                // IMPORTANTE: El mensaje de error debe ser censurado en un servicio superior para evitar
                // fugas de información sensible. 
                return Error<int>($"Error al ejecutar comando SQL: {ex.Message}");
            }
        }

        public async Task<OptionalResult<T>> ExecuteScalarAsync<T>(string sql, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                return await ExecuteScalarAsync<T>(sql, connection, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear conexion con la base de datos");
                return Error<T>($"Error al crear conexion con la base de datos");
            }
        }

        public async Task<OptionalResult<T>> ExecuteScalarAsync<T>(string sql, SqlConnection connection, Dictionary<string, object>? parameters = null, SqlTransaction? transaction = null)
        {
            try
            {
                using var command = CreateCommand(sql, connection, parameters, transaction);

                var result = await command.ExecuteScalarAsync();

                // Convert the result to T and wrap in Value<T>
                if (result == null || result is DBNull)
                    return NoValue<T>();

                return Value((T)Convert.ChangeType(result, typeof(T)));
            }
            catch (Exception ex)
            {
                //En release no logueamos parametros por seguridad
                _logger.LogError(ex, $"Error al ejecutar comando SQL: {sql}");

                _logger.LogDebug($"Error al ejecutar comando SQL: {sql} con parametros: {parameters}");

                return Error<T>($"Error al ejecutar comando SQL");
            }
        }

        public async Task<OptionalResult<IList<T>>> ExecuteReaderAsync<T>(string sql, Func<SqlDataReader, T> converter, Dictionary<string, object>? parameters = null)
            where T : class
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                return await ExecuteReaderAsync(sql, connection, converter, parameters);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al crear conexion con la base de datos");
                return Error<IList<T>>($"Error al crear conexion con la base de datos");
            }
        }


        public async Task<OptionalResult<IList<T>>> ExecuteReaderAsync<T>(string sql, SqlConnection connection,  Func<SqlDataReader, T> converter, Dictionary<string, object>? parameters = null, SqlTransaction? transaction = null)
            where T: class
        {
            try
            {
                using var command = CreateCommand(sql, connection, parameters, transaction);

                using var reader = await command.ExecuteReaderAsync();

                var result = new List<T>();

                while (await reader.ReadAsync())
                {
                    result.Add(converter(reader));
                }

                return Value<IList<T>>(result);
            }
            catch (Exception ex)
            {
                //En release no logueamos parametros por seguridad
                _logger.LogError(ex,$"Error al ejecutar comando SQL: {sql}");

                _logger.LogDebug($"Error al ejecutar comando SQL: {sql} con parametros: {parameters}");
                return Error<IList<T>>($"Error al ejecutar comando SQL");
            }
        }

        public async Task<OptionalResult<T>> ExecuteReaderSingleAsync<T>(string sql, Func<SqlDataReader, T> converter, Dictionary<string, object>? parameters = null, SqlTransaction? transaction = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                await connection.OpenAsync();

                return await ExecuteReaderSingleAsync(sql, connection, converter, parameters, transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear conexion con la base de datos");
                return Error<T>($"Error al crear conexion con la base de datos");
            }
        }

        public async Task<OptionalResult<T>> ExecuteReaderSingleAsync<T>(string sql, SqlConnection connection, Func<SqlDataReader, T> converter, Dictionary<string, object>? parameters = null, SqlTransaction? transaction = null)
        {
            try
            {			
                using var command = CreateCommand(sql, connection, parameters, transaction);

                using var reader = await command.ExecuteReaderAsync();

                if(reader.HasRows && await reader.ReadAsync())
                {
                    return Value(converter(reader));
                }

                return NoValue<T>();
            }
            catch (Exception ex)
            {
                //En release no logueamos parametros por seguridad
                _logger.LogError(ex, $"Error al ejecutar comando SQL: {sql}");

                _logger.LogDebug($"Error al ejecutar comando SQL: {sql} con parametros: {parameters}");
                return Error<T>($"Error al ejecutar comando SQL");
            }
        }

        public async Task<OperationResult> ExecuteInTransactionAsync(Func<SqlConnection, SqlTransaction, Task> operations)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                await operations(connection, transaction);
                await transaction.CommitAsync();
                return Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error al ejecutar transaccion SQL. Rolling back....");

                return Failure($"Error al ejecutar transaccion SQL");
            }
        }

        private void SetCommandParameters(SqlCommand command, Dictionary<string, object> parameters)
        {
            //Modificar para que asigne el tipo correcto en vez de usar AddWithValue
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }
    }
}