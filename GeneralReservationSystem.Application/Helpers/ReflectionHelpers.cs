using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Helpers
{
	/// <summary>
	/// Clase con metodos helpers para ayudar a trabajar con reflexion.
	/// </summary>
	/// <remarks>
	/// Implementa caching para mejorar el rendimiento en llamadas repetidas sobre los mismos tipos.
	/// </remarks>
	public static class ReflectionHelpers
	{
		public static ConcurrentDictionary<Type, PropertyInfo[]> TypePropertiesCache { get; private set; } = new();
		public static ConcurrentDictionary<MemberInfo, Attribute[]> AttributeCache { get; private set; } = new();

		public static PropertyInfo[] GetProperties(this Type type) => TypePropertiesCache.GetOrAdd(type, t => t.GetProperties());

		public static PropertyInfo[] GetProperties<TEntity>() => GetProperties(typeof(TEntity));

		public static PropertyInfo[] GetFilteredProperties<TEntity>(Func<PropertyInfo, bool> predicate)
		{
			ThrowHelpers.ThrowIfNull(predicate, nameof(predicate));
			return GetProperties<TEntity>()
				.Where(predicate)
				.ToArray();
		}

		public static PropertyInfo[] GetPropertiesWithAttribute<TEntity, TAttribute>()
			where TAttribute : Attribute
		{
			return GetFilteredProperties<TEntity>(p => p.HasAttribute<TAttribute>());
		}

		public static PropertyInfo[] GetPropertiesWithoutAttribute<TEntity, TAttribute>()
			where TAttribute : Attribute
		{
			return GetFilteredProperties<TEntity>(p => !p.HasAttribute<TAttribute>());
		}

		public static Attribute[] GetPropertyAttributes(this PropertyInfo propertyInfo)
		{
			ThrowHelpers.ThrowIfNull(propertyInfo, nameof(propertyInfo));
			return AttributeCache.GetOrAdd(propertyInfo, p => propertyInfo.GetCustomAttributes().ToArray());
		}

		public static Attribute[] GetTypeAttributes(this Type type)
		{
			ThrowHelpers.ThrowIfNull(type, nameof(type));
			return AttributeCache.GetOrAdd(type, t => type.GetCustomAttributes().ToArray());
		}

		public static Attribute[] GetTypeAttributes<TEntity>() =>
			GetTypeAttributes(typeof(TEntity));

		public static Attribute[] GetAttributes(this MemberInfo attrProvider)
		{
			ThrowHelpers.ThrowIfNull(attrProvider, nameof(attrProvider));
			return attrProvider switch
			{
				PropertyInfo p => p.GetPropertyAttributes(),
				Type t => t.GetTypeAttributes(),
				_ => throw new ArgumentException($"{nameof(attrProvider)} must be of type {nameof(PropertyInfo)} or {nameof(Type)}", nameof(attrProvider))
			};
		}

		public static bool HasAttribute<TAttribute>(this MemberInfo attrProvider)
			where TAttribute : Attribute
		{
			return attrProvider.GetAttributes().Any(a => a is TAttribute);
		}

		public static TAttribute? TryGetAttribute<TAttribute>(this MemberInfo attrProvider)
			where TAttribute : Attribute
		{
			//TODO: Considerar sacar el optional y tirar excepcion si no lo encuentra

			var propertyAttributes = attrProvider.GetAttributes();
			TAttribute? attr = propertyAttributes.OfType<TAttribute>().FirstOrDefault()!;
			return attr;
		}
	}
}
