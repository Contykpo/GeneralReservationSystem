using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class DbExpressionVisitor : ExpressionVisitor
    {
        public override Expression? Visit(Expression? exp)
        {
            return exp == null
                ? null
                : (DbExpressionType)exp.NodeType switch
                {
                    DbExpressionType.Table => VisitTable((TableExpression)exp),
                    DbExpressionType.Column => VisitColumn((ColumnExpression)exp),
                    DbExpressionType.Select => VisitSelect((SelectExpression)exp),
                    DbExpressionType.Join => VisitJoin((JoinExpression)exp),
                    DbExpressionType.Aggregate => VisitAggregate((AggregateExpression)exp),
                    DbExpressionType.Scalar or DbExpressionType.Exists or DbExpressionType.In => VisitSubquery((SubqueryExpression)exp),
                    DbExpressionType.AggregateSubquery => VisitAggregateSubquery((AggregateSubqueryExpression)exp),
                    DbExpressionType.IsNull => VisitIsNull((IsNullExpression)exp),
                    DbExpressionType.Between => VisitBetween((BetweenExpression)exp),
                    DbExpressionType.RowCount => VisitRowNumber((RowNumberExpression)exp),
                    DbExpressionType.Projection => VisitProjection((ProjectionExpression)exp),
                    DbExpressionType.NamedValue => VisitNamedValue((NamedValueExpression)exp),
                    _ => base.Visit(exp),
                };
        }
        protected virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }
        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }
        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression from = VisitSource(select.From)!;
            Expression where = Visit(select.Where)!;
            ReadOnlyCollection<OrderExpression>? orderBy = VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression>? groupBy = VisitExpressionList(select.GroupBy);
            Expression? skip = Visit(select.Skip);
            Expression? take = Visit(select.Take);
            ReadOnlyCollection<ColumnDeclaration> columns = VisitColumnDeclarations(select.Columns);
            return from != select.From
                || where != select.Where
                || orderBy != select.OrderBy
                || groupBy != select.GroupBy
                || take != select.Take
                || skip != select.Skip
                || columns != select.Columns
                ? new SelectExpression(select.Type, select.Alias, columns, from, where, orderBy, groupBy, select.IsDistinct, skip, take)
                : select;
        }
        protected virtual Expression VisitJoin(JoinExpression join)
        {
            Expression left = VisitSource(join.Left)!;
            Expression right = VisitSource(join.Right)!;
            Expression condition = Visit(join.Condition)!;
            return left != join.Left || right != join.Right || condition != join.Condition
                ? new JoinExpression(join.Type, join.Join, left, right, condition)
                : join;
        }
        protected virtual Expression VisitAggregate(AggregateExpression aggregate)
        {
            Expression arg = Visit(aggregate.Argument)!;
            return arg != aggregate.Argument
                ? new AggregateExpression(aggregate.Type, aggregate.AggregateType, arg, aggregate.IsDistinct)
                : aggregate;
        }
        protected virtual Expression VisitIsNull(IsNullExpression isnull)
        {
            Expression expr = Visit(isnull.Expression)!;
            return expr != isnull.Expression ? new IsNullExpression(expr) : isnull;
        }
        protected virtual Expression VisitBetween(BetweenExpression between)
        {
            Expression expr = Visit(between.Expression)!;
            Expression lower = Visit(between.Lower)!;
            Expression upper = Visit(between.Upper)!;
            return expr != between.Expression || lower != between.Lower || upper != between.Upper
                ? new BetweenExpression(expr, lower, upper)
                : between;
        }
        protected virtual Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            ReadOnlyCollection<OrderExpression> orderby = VisitOrderBy(rowNumber.OrderBy)!;
            return orderby != rowNumber.OrderBy ? new RowNumberExpression(orderby) : rowNumber;
        }
        protected virtual Expression VisitNamedValue(NamedValueExpression value)
        {
            return value;
        }
        protected virtual Expression VisitSubquery(SubqueryExpression subquery)
        {
            return (DbExpressionType)subquery.NodeType switch
            {
                DbExpressionType.Scalar => VisitScalar((ScalarExpression)subquery),
                DbExpressionType.Exists => VisitExists((ExistsExpression)subquery),
                DbExpressionType.In => VisitIn((InExpression)subquery),
                _ => subquery,
            };
        }

        protected virtual Expression VisitScalar(ScalarExpression scalar)
        {
            SelectExpression select = (SelectExpression)Visit(scalar.Select)!;
            return select != scalar.Select ? new ScalarExpression(scalar.Type, select) : scalar;
        }

        protected virtual Expression VisitExists(ExistsExpression exists)
        {
            SelectExpression select = (SelectExpression)Visit(exists.Select)!;
            return select != exists.Select ? new ExistsExpression(select) : exists;
        }

        protected virtual Expression VisitIn(InExpression @in)
        {
            Expression? expr = Visit(@in.Expression);
            if (@in.Select != null)
            {
                SelectExpression select = (SelectExpression)Visit(@in.Select)!;
                if (expr != @in.Expression || select != @in.Select)
                {
                    return new InExpression(expr!, select);
                }
            }
            else
            {
                IEnumerable<Expression>? values = VisitExpressionList(@in.Values);
                if (expr != @in.Expression || values != @in.Values)
                {
                    return new InExpression(expr!, values!);
                }
            }
            return @in;
        }

        protected virtual Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            Expression? e = Visit(aggregate.AggregateAsSubquery);
            System.Diagnostics.Debug.Assert(e is ScalarExpression);
            ScalarExpression subquery = (ScalarExpression)e;
            return subquery != aggregate.AggregateAsSubquery
                ? new AggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.AggregateInGroupSelect, subquery)
                : aggregate;
        }

        protected virtual Expression? VisitSource(Expression? source)
        {
            return Visit(source);
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)Visit(proj.Source)!;
            Expression projector = Visit(proj.Projector)!;
            return source != proj.Source || projector != proj.Projector ? new ProjectionExpression(source, projector, proj.Aggregator) : proj;
        }

        protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            List<ColumnDeclaration>? alternate = null;
            for (int i = 0, n = columns.Count; i < n; i++)
            {
                ColumnDeclaration column = columns[i];
                Expression? e = Visit(column.Expression);
                if (alternate == null && e != column.Expression)
                {
                    alternate = [.. columns.Take(i)];
                }
                alternate?.Add(new ColumnDeclaration(column.Name, e!));
            }
            return alternate != null ? alternate.AsReadOnly() : columns;
        }

        protected ReadOnlyCollection<OrderExpression>? VisitOrderBy(ReadOnlyCollection<OrderExpression>? expressions)
        {
            if (expressions != null)
            {
                List<OrderExpression>? alternate = null;
                for (int i = 0, n = expressions.Count; i < n; i++)
                {
                    OrderExpression expr = expressions[i];
                    Expression? e = Visit(expr.Expression);
                    if (alternate == null && e != expr.Expression)
                    {
                        alternate = [.. expressions.Take(i)];
                    }
                    alternate?.Add(new OrderExpression(expr.OrderType, e!));
                }
                if (alternate != null)
                {
                    return alternate.AsReadOnly();
                }
            }
            return expressions;
        }

        protected virtual ReadOnlyCollection<Expression>? VisitExpressionList(ReadOnlyCollection<Expression>? original)
        {
            if (original != null)
            {
                List<Expression>? list = null;
                for (int i = 0, n = original.Count; i < n; i++)
                {
                    Expression p = Visit(original[i])!;
                    if (list != null)
                    {
                        list.Add(p);
                    }
                    else if (p != original[i])
                    {
                        list = new List<Expression>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }
                        list.Add(p);
                    }
                }
                if (list != null)
                {
                    return list.AsReadOnly();
                }
            }
            return original;
        }
    }
}
