using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
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
        private readonly Dictionary<ColumnExpression, ColumnExpression> map;
        private readonly List<ColumnDeclaration> columns;
        private readonly HashSet<string> columnNames;
        private readonly HashSet<Expression> candidates;
        private readonly string[] existingAliases;
        private readonly string newAlias;
        private int iColumn;

        private ColumnProjector(Func<Expression, bool> fnCanBeColumn, Expression expression, string newAlias, params string[] existingAliases)
        {
            this.newAlias = newAlias;
            this.existingAliases = existingAliases;
            map = [];
            columns = [];
            columnNames = [];
            candidates = Nominator.Nominate(fnCanBeColumn, expression);
        }

        internal static ProjectedColumns ProjectColumns(Func<Expression, bool> fnCanBeColumn, Expression expression, string newAlias, params string[] existingAliases)
        {
            ColumnProjector projector = new(fnCanBeColumn, expression, newAlias, existingAliases);
            Expression expr = projector.Visit(expression)!;
            return new ProjectedColumns(expr, projector.columns.AsReadOnly());
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
                        _ = columns.Count;
                        string columnName = GetUniqueColumnName(column.Name);
                        columns.Add(new ColumnDeclaration(columnName, column));
                        mapped = new ColumnExpression(column.Type, newAlias, columnName);
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
                    columns.Add(new ColumnDeclaration(columnName, expression));
                    return new ColumnExpression(expression.Type, newAlias, columnName);
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

        /// <summary>
        /// Nominator is a class that walks an expression tree bottom up, determining the set of 
        /// candidate expressions that are possible columns of a select expression
        /// </summary>
        private class Nominator : DbExpressionVisitor
        {
            private readonly Func<Expression, bool> fnCanBeColumn;
            private bool isBlocked;
            private readonly HashSet<Expression> candidates;

            private Nominator(Func<Expression, bool> fnCanBeColumn)
            {
                this.fnCanBeColumn = fnCanBeColumn;
                candidates = [];
                isBlocked = false;
            }

            internal static HashSet<Expression> Nominate(Func<Expression, bool> fnCanBeColumn, Expression expression)
            {
                Nominator nominator = new(fnCanBeColumn);
                _ = nominator.Visit(expression);
                return nominator.candidates;
            }

            public override Expression? Visit(Expression? expression)
            {
                if (expression != null)
                {
                    bool saveIsBlocked = isBlocked;
                    isBlocked = false;
                    if (expression.NodeType != (ExpressionType)DbExpressionType.Scalar)
                    {
                        _ = base.Visit(expression);
                    }
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
