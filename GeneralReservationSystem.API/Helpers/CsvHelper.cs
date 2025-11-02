using FluentValidation;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace GeneralReservationSystem.API.Helpers
{
    public static class CsvHelper
    {
        public static async IAsyncEnumerable<T> ParseAndValidateCsvAsync<T>(Stream csvStream, IValidator<T> validator, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using StreamReader reader = new(csvStream, Encoding.UTF8);

            PropertyInfo[] properties = EntityHelper.GetNonComputedProperties<T>();
            int expectedColumns = properties.Length;
            int lineNumber = 0;
            List<ValidationError> errors = [];
            while (!reader.EndOfStream)
            {
                lineNumber++;
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values = line.Split(',');
                if (values.Length != expectedColumns)
                {
                    errors.Add(new ValidationError($"Formato inválido en la línea {lineNumber}. Se esperan {expectedColumns} columnas.", $"csv[{lineNumber}]"));
                    continue;
                }

                T dto = Activator.CreateInstance<T>();
                for (int i = 0; i < expectedColumns; i++)
                {
                    object? converted = EntityTypeConverter.ConvertFromDbValue(values[i].Trim(), properties[i].PropertyType);
                    properties[i].SetValue(dto, converted);
                }

                FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    foreach (FluentValidation.Results.ValidationFailure? ve in validationResult.Errors)
                    {
                        errors.Add(new ValidationError($"Error de validación en la línea {lineNumber}: {ve.ErrorMessage}", $"csv[{lineNumber}].{ve.PropertyName}"));
                    }

                    continue;
                }
                yield return dto;
            }
            if (errors.Count > 0)
            {
                throw new ServiceValidationException("Errores de validación en el archivo CSV.", [.. errors]);
            }
        }

        public static byte[] ExportToCsv<T>(IEnumerable<T> items)
        {
            StringBuilder csv = new();
            PropertyInfo[] properties = EntityHelper.GetNonComputedProperties<T>();
            // Header
            _ = csv.AppendLine(string.Join(',', properties.Select(p => EscapeCsvField(EntityHelper.GetColumnName(p)))));
            // Rows
            foreach (T item in items)
            {
                _ = csv.AppendLine(string.Join(',', properties.Select(p => EscapeCsvField(p.GetValue(item)?.ToString() ?? ""))));
            }
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public static string EscapeCsvField(string field)
        {
            return field.Contains(',') || field.Contains('"') || field.Contains('\n') ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
        }
    }
}
