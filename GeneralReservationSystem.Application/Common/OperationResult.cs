using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Infrastructure.Common
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
			action?.Invoke(ErrorMessage);
			return this;
		}
	}
}