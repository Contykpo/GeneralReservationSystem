using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Infrastructure.Common
{
	public abstract record OptionalResult<TOptional>
	{
		public static Value<TValue> Value<TValue>(TValue value) => new(value);

		public static NoValue<TValue> NoValue<TValue>() => new();

		public static ErrorValue<TValue> Error<TValue>(string? error = null) => new(error);

		public virtual OptionalResult<TOptional> IfValue(Action<TOptional> action) => this;
		public virtual OptionalResult<TOptional> IfEmpty(Action action) => this;

		public virtual OptionalResult<TOptional> IfError(Action<string?> action) => this;

		public abstract TMatchResult Match<TMatchResult>(Func<TOptional, TMatchResult> onValue, Func<TMatchResult> onEmpty, Func<string?, TMatchResult> onError);
	}

	public record Value<TValue>(TValue value) : OptionalResult<TValue>
	{
		public override OptionalResult<TValue> IfValue(Action<TValue> action)
		{
			action?.Invoke(value);
			return this;
		}

		public override TMatchResult Match<TMatchResult>(Func<TValue, TMatchResult> onValue, Func<TMatchResult> onEmpty, Func<string?, TMatchResult> onError)
		{
			return onValue(value);
		}
	}

	public record NoValue<TValue> : OptionalResult<TValue>
	{
		public override OptionalResult<TValue> IfEmpty(Action action)
		{
			action?.Invoke();
			return this;
		}

		public override TMatchResult Match<TMatchResult>(Func<TValue, TMatchResult> onValue, Func<TMatchResult> onEmpty, Func<string?, TMatchResult> onError)
		{
			return onEmpty();
		}
	}

	public record ErrorValue<TValue>(string? error) : OptionalResult<TValue>
	{
		public override OptionalResult<TValue> IfError(Action<string?> action)
		{
			action?.Invoke(error);
			return this;
		}

		public override TMatchResult Match<TMatchResult>(Func<TValue, TMatchResult> onValue, Func<TMatchResult> onEmpty, Func<string?, TMatchResult> onError)
		{
			return onError(error);
		}	
	}
}
