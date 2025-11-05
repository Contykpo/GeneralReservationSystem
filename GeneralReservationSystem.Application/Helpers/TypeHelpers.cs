using System.Reflection;

namespace GeneralReservationSystem.Application.Helpers
{
    public static class TypeHelpers
    {
        private static Type? FindIEnumerable(Type? seqType)
        {
            if (seqType is null || seqType == typeof(string))
            {
                return null;
            }

            if (seqType.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType()!);
            }

            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }

            foreach (Type iface in seqType.GetInterfaces())
            {
                Type? ienum = FindIEnumerable(iface);
                if (ienum is not null)
                {
                    return ienum;
                }
            }

            return seqType.BaseType is not null && seqType.BaseType != typeof(object) ? FindIEnumerable(seqType.BaseType) : null;
        }

        public static Type GetSequenceType(Type elementType)
        {
            return typeof(IEnumerable<>).MakeGenericType(elementType);
        }

        public static Type GetElementType(Type seqType)
        {
            Type? ienum = FindIEnumerable(seqType);
            return ienum is null ? seqType : ienum.GetGenericArguments()[0];
        }

        public static bool IsNullableType(Type? type)
        {
            return type is not null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsNullAssignable(Type type)
        {
            return !type.IsValueType || IsNullableType(type);
        }

        public static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        public static Type? GetMemberType(MemberInfo mi)
        {
            return mi switch
            {
                FieldInfo fi => fi.FieldType,
                PropertyInfo pi => pi.PropertyType,
                EventInfo ei => ei.EventHandlerType,
                _ => null
            };
        }
    }
}
