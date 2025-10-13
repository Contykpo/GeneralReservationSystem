using System.Text.RegularExpressions;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static partial class RegexHelpers
    {
        [GeneratedRegex(
            @"(?<PrimaryKey>PRIMARY KEY constraint)|(?<Unique>UNIQUE KEY constraint)|(?<ForeignKey>FOREIGN KEY constraint)|(?<Check>CHECK constraint)|(?<NotNull>NOT NULL)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex ConstraintTypeRegex();

        [GeneratedRegex(@"constraint ['\[]?(?<name>[\w\d_]+)['\]]?", RegexOptions.IgnoreCase)]
        public static partial Regex ConstraintNameRegex();

        [GeneratedRegex(@"Column ['\[]?(?<name>[\w\d_]+)['\]]? is defined as NOT NULL", RegexOptions.IgnoreCase)]
        public static partial Regex NotNullColumnRegex();
    }
}
