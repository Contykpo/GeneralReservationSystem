using System.Text.RegularExpressions;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static partial class RegexHelpers
    {
        [GeneratedRegex(
            @"(?<PrimaryKey>duplicate key value violates unique constraint ""(?<name>\w+)"".*Key \(.*\)=\(.*\) already exists)|(?<Unique>duplicate key value violates unique constraint ""(?<name>\w+)"")|(?<ForeignKey>violates foreign key constraint ""(?<name>\w+)"")|(?<Check>violates check constraint ""(?<name>\w+)"")|(?<NotNull>null value in column ""(?<columnName>\w+)"" (?:of relation ""\w+"" )?violates not-null constraint)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex ConstraintTypeRegex();

        [GeneratedRegex(@"constraint ""(?<name>\w+)""", RegexOptions.IgnoreCase)]
        public static partial Regex ConstraintNameRegex();

        [GeneratedRegex(@"null value in column ""(?<name>\w+)""", RegexOptions.IgnoreCase)]
        public static partial Regex NotNullColumnRegex();
    }
}
