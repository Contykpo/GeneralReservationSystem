using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        public string Name { get; }
        public TableNameAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute : Attribute
    {
        public string Name { get; }
        public ColumnNameAttribute(string name) => Name = name;
    }

    // NOTE: [Key] doesn't imply that the key is computed/auto-incremented/identity.
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ComputedAttribute : Attribute
    {
    }
}