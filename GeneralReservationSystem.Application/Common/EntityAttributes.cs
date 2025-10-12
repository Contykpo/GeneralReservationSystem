namespace GeneralReservationSystem.Application.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
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