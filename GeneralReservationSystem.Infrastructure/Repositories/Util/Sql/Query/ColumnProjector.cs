using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    public abstract class ProjectionRow
    {
        public abstract object? GetValue(int index);
        public abstract IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query);
    }

    internal sealed class ProjectedColumns
    {
        internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
        {
            Projector = projector;
            Columns = columns;
        }

        internal Expression Projector { get; }

        internal ReadOnlyCollection<ColumnDeclaration> Columns { get; }
    }

    internal class ColumnProjector : DbExpressionVisitor
    {
        private readonly Nominator nominator;
        private Dictionary<ColumnExpression, ColumnExpression> map = null!;
        private List<ColumnDeclaration> columns = null!;
        private HashSet<string> columnNames = null!;
        private HashSet<Expression> candidates = null!;
        private string[] existingAliases = null!;
        private string newAlias = null!;
        private int iColumn;

        internal ColumnProjector(Func<Expression, bool> fnCanBeColumn)
        {
            nominator = new Nominator(fnCanBeColumn);
        }

        internal ProjectedColumns ProjectColumns(Expression expression, string newAlias, params string[] existingAliases)
        {
            map = [];
            columns = [];
            columnNames = [];
            this.newAlias = newAlias;
            this.existingAliases = existingAliases;
            candidates = nominator.Nominate(expression);

            return new ProjectedColumns(Visit(expression)!, columns.AsReadOnly());
        }

        public override Expression? Visit(Expression? expression)
        {
            if (expression != null && candidates.Contains(expression))
            {

                if (expression.NodeType == (ExpressionType)DbExpressionType.Column)
                {
                    ColumnExpression column = (ColumnExpression)expression;

                    if (map.TryGetValue(column, out ColumnExpression? mapped))
                    {
                        return mapped;
                    }

                    if (existingAliases.Contains(column.Alias))
                    {
                        int ordinal = columns.Count;
                        string columnName = GetUniqueColumnName(column.Name);
                        columns.Add(new ColumnDeclaration(columnName, column));
                        mapped = new ColumnExpression(column.Type, newAlias, columnName, ordinal);
                        map[column] = mapped;
                        _ = columnNames.Add(columnName);

                        return mapped;
                    }

                    // must be referring to outer scope
                    return column;
                }
                else
                {
                    string columnName = GetNextColumnName();
                    int ordinal = columns.Count;

                    columns.Add(new ColumnDeclaration(columnName, expression));

                    return new ColumnExpression(expression.Type, newAlias, columnName, ordinal);
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }

        private bool IsColumnNameInUse(string name)
        {
            return columnNames.Contains(name);
        }

        private string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;

            while (IsColumnNameInUse(name))
            {
                name = baseName + suffix++;
            }

            return name;
        }

        private string GetNextColumnName()
        {
            return GetUniqueColumnName("c" + iColumn++);
        }

        private class Nominator : DbExpressionVisitor
        {
            private readonly Func<Expression, bool> fnCanBeColumn;
            private bool isBlocked;
            private HashSet<Expression> candidates = null!;

            internal Nominator(Func<Expression, bool> fnCanBeColumn)
            {
                this.fnCanBeColumn = fnCanBeColumn;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                candidates = [];
                isBlocked = false;
                _ = Visit(expression);

                return candidates;
            }

            public override Expression? Visit(Expression? expression)
            {
                if (expression != null)
                {
                    bool saveIsBlocked = isBlocked;
                    isBlocked = false;
                    _ = base.Visit(expression);

                    if (!isBlocked)
                    {
                        if (fnCanBeColumn(expression))
                        {
                            _ = candidates.Add(expression);
                        }
                        else
                        {
                            isBlocked = true;
                        }
                    }

                    isBlocked |= saveIsBlocked;
                }

                return expression;
            }
        }
    }
}
