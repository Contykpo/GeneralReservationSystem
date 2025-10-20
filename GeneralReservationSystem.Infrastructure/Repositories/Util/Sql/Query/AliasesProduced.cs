using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class AliasesProduced : DbExpressionVisitor
    {
        private readonly HashSet<string> aliases;

        private AliasesProduced()
        {
            aliases = [];
        }

        internal static HashSet<string> Gather(Expression source)
        {
            AliasesProduced produced = new();
            _ = produced.Visit(source);
            return produced.aliases;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            _ = aliases.Add(select.Alias);
            return select;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            _ = aliases.Add(table.Alias);
            return table;
        }
    }
}
