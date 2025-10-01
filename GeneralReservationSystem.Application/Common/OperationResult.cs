using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Common
{
	public abstract record OperationResult
	{
		public static Success Success() => new();

		public static Failure Failure(string? errorMessage = null) => new(errorMessage);

		public virtual OperationResult IfSuccess(Action action) => this;

		public virtual OperationResult IfFailure(Action<string?> action) => this;
	}

	public record Success : OperationResult
	{
		public override OperationResult IfSuccess(Action action)
		{
			action?.Invoke();
			return this;
		}
	}

	public record Failure(string? ErrorMessage) : OperationResult
	{
		public override OperationResult IfFailure(Action<string?> action)
		{
            // NOTA/TODO: Esto esconde errores de constraints de la base de datos. Hay que manejarlo
            // mejor en una capa superior. Si se esconden esos errores, el usuario recibe un mensaje críptico
            // y no sabe qué hacer con esa información.
            // Si el mensaje de error está en inglés, traducirlo aquí
            var mensaje = ErrorMessage;
			if (mensaje == "Error while executing SQL transaction")
				mensaje = "Error al ejecutar transacción SQL";
			if (mensaje == "Error while executing SQL command")
				mensaje = "Error al ejecutar comando SQL";
			if (mensaje == "Error while creating SQL connection")
				mensaje = "Error al crear conexión con la base de datos";
			action?.Invoke(mensaje);
			return this;
		}
	}
}