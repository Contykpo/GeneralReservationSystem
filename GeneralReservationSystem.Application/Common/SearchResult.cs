using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Common
{
	public abstract record SearchResult<TValue> : OptionalResult<TValue>
	{
		public static Value<TFound> Found<TFound>(TFound value) => new(value);

		public static NoValue<TNotFound> NotFound<TNotFound>() => new();
	}
}
