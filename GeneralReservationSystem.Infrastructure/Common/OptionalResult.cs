using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public abstract TMatchResult Match<TMatchResult>(Func<TOptional, TMatchResult>? onValue = null, Func<TMatchResult>? onEmpty = null, Func<string?, TMatchResult>? onError = null);

		public abstract void Match(Action<TOptional>? onValue = null, Action? onEmpty = null, Action<string?>? onError = null);
	}

	public record Value<TValue>(TValue value) : OptionalResult<TValue>
	{
		public override OptionalResult<TValue> IfValue(Action<TValue> action)
		{
			action?.Invoke(value);
			return this;
		}

		public override TMatchResult Match<TMatchResult>(Func<TValue, TMatchResult>? onValue, Func<TMatchResult>? onEmpty, Func<string?, TMatchResult>? onError)
		{
			Debug.Assert(onValue != null, "Unhandled Value Present Case");

			return onValue(value);
		}

		public override void Match(Action<TValue>? onValue = null, Action? onEmpty = null, Action<string?>? onError = null)
		{
			Debug.Assert(onValue != null, "Unhandled Value Present Case");

			onValue(value);
		}
	}

	public record NoValue<TValue> : OptionalResult<TValue>
	{
		public override OptionalResult<TValue> IfEmpty(Action action)
		{
			action?.Invoke();
			return this;
		}

		public override TMatchResult Match<TMatchResult>(Func<TValue, TMatchResult>? onValue, Func<TMatchResult>? onEmpty, Func<string?, TMatchResult>? onError)
		{
			Debug.Assert(onEmpty != null, "Unhandled No Value Case");

			return onEmpty();
		}

		public override void Match(Action<TValue>? onValue = null, Action? onEmpty = null, Action<string?>? onError = null)
		{
			Debug.Assert(onEmpty != null, "Unhandled No Value Case");

			onEmpty();
		}
	}

	public record ErrorValue<TValue>(string? error) : OptionalResult<TValue>
	{
		public override OptionalResult<TValue> IfError(Action<string?> action)
		{
			action?.Invoke(error);
			return this;
		}

		public override TMatchResult Match<TMatchResult>(Func<TValue, TMatchResult>? onValue, Func<TMatchResult>? onEmpty, Func<string?, TMatchResult>? onError)
		{
			Debug.Assert(onError != null, "Unhandled Error Case");

			return onError(error);
		}

		public override void Match(Action<TValue>? onValue = null, Action? onEmpty = null, Action<string?>? onError = null)
		{
			Debug.Assert(onError != null, "Unhandled Error Case");

			onError(error);
		}
	}
}
