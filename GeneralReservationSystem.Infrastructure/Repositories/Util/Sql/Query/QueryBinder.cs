using GeneralReservationSystem.Application.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class TranslateResult
    {
        internal required string CommandText;
        internal required LambdaExpression Projector;
    }

    internal class QueryBinder : ExpressionVisitor
    {
        private readonly ColumnProjector columnProjector;
        private Dictionary<ParameterExpression, Expression> map = [];
        private int aliasCount;

        internal QueryBinder()
        {
            columnProjector = new ColumnProjector(CanBeColumn);
        }

        private static bool CanBeColumn(Expression expression)
        {
            return expression.NodeType == (ExpressionType)DbExpressionType.Column;
        }

        internal Expression Bind(Expression expression)
        {
            map = [];
            return Visit(expression);
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            return e;
        }

        private string GetNextAlias()
        {
            return "t" + aliasCount++;
        }

        private ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
        {
            return columnProjector.ProjectColumns(expression, newAlias, existingAlias);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            return m.Method.DeclaringType == typeof(Queryable) ||

                m.Method.DeclaringType == typeof(Enumerable)
                ? m.Method.Name switch
                {
                    "Where" => BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1])),
                    "Select" => BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1])),
                    _ => throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name)),
                }
                : base.VisitMethodCall(m);
        }

        private ProjectionExpression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = (ProjectionExpression)Visit(source);

            map[predicate.Parameters[0]] = projection.Projector;

            Expression where = Visit(predicate.Body);

            string alias = GetNextAlias();

            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));

            return new ProjectionExpression
            (
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
                pc.Projector
            );
        }

        private ProjectionExpression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = (ProjectionExpression)Visit(source);

            map[selector.Parameters[0]] = projection.Projector;

            Expression expression = Visit(selector.Body);

            string alias = GetNextAlias();

            ProjectedColumns pc = ProjectColumns(expression, alias, GetExistingAlias(projection.Source));

            return new ProjectionExpression
            (
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
                pc.Projector
            );
        }

        private static string GetExistingAlias(Expression source)
        {
            return (DbExpressionType)source.NodeType switch
            {
                DbExpressionType.Select => ((SelectExpression)source).Alias,
                DbExpressionType.Table => ((TableExpression)source).Alias,
                _ => throw new InvalidOperationException(string.Format("Invalid source node type '{0}'", source.NodeType)),
            };
        }

        private static bool IsTable(object? value)
        {
            return value is IQueryable q && q.Expression.NodeType == ExpressionType.Constant;
        }

        private static string GetTableName(object table)
        {
            IQueryable tableQuery = (IQueryable)table;
            Type rowType = tableQuery.ElementType;

            //return rowType.Name;

            return EntityHelper.GetTableName(rowType);
        }

        private static string GetColumnName(MemberInfo mi)
        {
            //return pi.Name;

            return mi is not PropertyInfo and not FieldInfo
                ? throw new ArgumentException("MemberInfo must be either FieldInfo or PropertyInfo", nameof(mi))
                : EntityHelper.GetColumnName(mi);
        }

        private static Type GetColumnType(MemberInfo mi)
        {
            return mi switch
            {
                FieldInfo fi => fi.FieldType,
                PropertyInfo pi => pi.PropertyType,
                _ => throw new ArgumentException("MemberInfo must be either FieldInfo or PropertyInfo", nameof(mi)),
            };
        }

        private static IEnumerable<MemberInfo> GetMappedMembers(Type rowType)
        {
            return rowType
                .GetFields()
                .Cast<MemberInfo>()
                .Concat(
                    rowType.GetProperties()
                );
        }

        private ProjectionExpression GetTableProjection(object value)
        {
            IQueryable table = (IQueryable)value;
            string tableAlias = GetNextAlias();
            string selectAlias = GetNextAlias();

            List<MemberBinding> bindings = [];
            List<ColumnDeclaration> columns = [];

            foreach (MemberInfo mi in GetMappedMembers(table.ElementType))
            {
                string columnName = GetColumnName(mi);
                Type columnType = GetColumnType(mi);
                int ordinal = columns.Count;

                bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));

                columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));
            }

            Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);

            Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);

            return new ProjectionExpression
            (
                new SelectExpression(
                    resultType,
                    selectAlias,
                    columns,
                    new TableExpression(resultType, tableAlias, GetTableName(table)),
                    null
                ),
                projector
            );
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            return IsTable(c.Value) ? GetTableProjection(c.Value!) : c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetValue(p, out Expression? e) ? e : p;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            Expression source = Visit(m.Expression)!;

            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    MemberInitExpression min = (MemberInitExpression)source;

                    for (int i = 0, n = min.Bindings.Count; i < n; i++)
                    {
                        if (min.Bindings[i] is MemberAssignment assign && MembersMatch(assign.Member, m.Member))
                        {
                            return assign.Expression;
                        }
                    }
                    break;

                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;

                    if (nex.Members != null)
                    {
                        for (int i = 0, n = nex.Members.Count; i < n; i++)
                        {
                            if (MembersMatch(nex.Members[i], m.Member))
                            {
                                return nex.Arguments[i];
                            }
                        }
                    }
                    break;
            }

            return source == m.Expression ? m : MakeMember(source, m.Member);
        }

        private static bool MembersMatch(MemberInfo a, MemberInfo b)
        {
            if (a == b)
            {
                return true;
            }

            if (a is MethodInfo && b is PropertyInfo info1)
            {
                return a == info1.GetGetMethod();
            }
            else if (a is PropertyInfo info && b is MethodInfo)
            {
                return info.GetGetMethod() == b;
            }

            return false;
        }

        private static MemberExpression MakeMember(Expression source, MemberInfo mi)
        {
            FieldInfo? fi = mi as FieldInfo;

            if (fi != null)
            {
                return Expression.Field(source, fi);
            }

            PropertyInfo pi = (mi as PropertyInfo)!;

            return Expression.Property(source, pi);
        }
    }
}
