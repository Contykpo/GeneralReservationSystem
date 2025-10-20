using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal enum DbExpressionType
    {
        Table = 1000, // To make sure these don't overlap with ExpressionType
        Column,
        Select,
        Projection,
        Join,
        Aggregate,
        Scalar,
        Exists,
        In,
        Grouping,
        AggregateSubquery,
        IsNull,
        Between,
        RowCount,
        NamedValue
    }

    internal static class DbExpressionExtensions
    {
        internal static bool IsDbExpression(this ExpressionType et)
        {
            return ((int)et) >= 1000;
        }
    }

    internal class TableExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Table;

        public override Type Type { get; }

        internal TableExpression(Type type, string alias, string name)
        {
            Type = type;

            Alias = alias;
            Name = name;
        }
        internal string Alias { get; }
        internal string Name { get; }
    }

    internal class ColumnExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Column;

        public override Type Type { get; }

        internal ColumnExpression(Type type, string alias, string name)
        {
            Type = type;

            Alias = alias;
            Name = name;
        }
        internal string Alias { get; }
        internal string Name { get; }
    }

    internal class ColumnDeclaration
    {
        internal ColumnDeclaration(string name, Expression expression)
        {
            Name = name;
            Expression = expression;
        }
        internal string Name { get; }
        internal Expression Expression { get; }
    }

    internal enum OrderType
    {
        Ascending,
        Descending
    }

    internal class OrderExpression
    {
        internal OrderExpression(OrderType orderType, Expression expression)
        {
            OrderType = orderType;
            Expression = expression;
        }
        internal OrderType OrderType { get; }
        internal Expression Expression { get; }
    }

    internal class SelectExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Select;

        public override Type Type { get; }

        internal SelectExpression(
            Type type,
            string alias,
            IEnumerable<ColumnDeclaration> columns,
            Expression? from,
            Expression? where,
            IEnumerable<OrderExpression>? orderBy,
            IEnumerable<Expression>? groupBy,
            bool isDistinct,
            Expression? skip,
            Expression? take)
        {
            Type = type;

            Alias = alias;
            Columns = columns as ReadOnlyCollection<ColumnDeclaration> ?? new List<ColumnDeclaration>(columns).AsReadOnly();

            IsDistinct = isDistinct;
            From = from;
            Where = where;
            if (orderBy != null)
            {
                OrderBy = orderBy as ReadOnlyCollection<OrderExpression> ?? new List<OrderExpression>(orderBy).AsReadOnly();
            }
            if (groupBy != null)
            {
                GroupBy = groupBy as ReadOnlyCollection<Expression> ?? new List<Expression>(groupBy).AsReadOnly();
            }
            Take = take;
            Skip = skip;
        }
        internal SelectExpression(
            Type type,
            string alias,
            IEnumerable<ColumnDeclaration> columns,
            Expression? from,
            Expression? where,
            IEnumerable<OrderExpression>? orderBy,
            IEnumerable<Expression>? groupBy
            )
            : this(type, alias, columns, from, where, orderBy, groupBy, false, null, null)
        {
        }
        internal SelectExpression(
            Type type, string alias, IEnumerable<ColumnDeclaration> columns,
            Expression? from, Expression? where
            )
            : this(type, alias, columns, from, where, null, null)
        {
        }
        internal string Alias { get; }
        internal ReadOnlyCollection<ColumnDeclaration> Columns { get; }
        internal Expression? From { get; }
        internal Expression? Where { get; }
        internal ReadOnlyCollection<OrderExpression>? OrderBy { get; }
        internal ReadOnlyCollection<Expression>? GroupBy { get; }
        internal bool IsDistinct { get; }
        internal Expression? Skip { get; }
        internal Expression? Take { get; }
    }

    internal enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply,
        LeftOuter
    }

    internal class JoinExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Join;

        public override Type Type { get; }

        internal JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression? condition)
        {
            Type = type;

            Join = joinType;
            Left = left;
            Right = right;
            Condition = condition;
        }
        internal JoinType Join { get; }
        internal Expression Left { get; }
        internal Expression Right { get; }
        internal new Expression? Condition { get; }
    }

    internal abstract class SubqueryExpression : Expression
    {
        public override ExpressionType NodeType { get; }

        public override Type Type { get; }
        protected SubqueryExpression(DbExpressionType eType, Type type, SelectExpression? select)
        {
            NodeType = (ExpressionType)eType;
            Type = type;

            System.Diagnostics.Debug.Assert(eType is DbExpressionType.Scalar or DbExpressionType.Exists or DbExpressionType.In);
            Select = select;
        }
        internal SelectExpression? Select
        {
            get;
        }
    }

    internal class ScalarExpression : SubqueryExpression
    {
        internal ScalarExpression(Type type, SelectExpression select)
            : base(DbExpressionType.Scalar, type, select)
        {
        }
    }

    internal class ExistsExpression : SubqueryExpression
    {
        internal ExistsExpression(SelectExpression select)
            : base(DbExpressionType.Exists, typeof(bool), select)
        {
        }
    }

    internal class InExpression : SubqueryExpression
    {
        internal InExpression(Expression expression, SelectExpression select)
            : base(DbExpressionType.In, typeof(bool), select)
        {
            Expression = expression;
        }
        internal InExpression(Expression expression, IEnumerable<Expression> values)
            : base(DbExpressionType.In, typeof(bool), null)
        {
            Expression = expression;
            Values = values as ReadOnlyCollection<Expression>;
            if (Values == null && values != null)
            {
                Values = new List<Expression>(values).AsReadOnly();
            }
        }
        internal Expression Expression { get; }
        internal ReadOnlyCollection<Expression>? Values { get; }
    }

    internal enum AggregateType
    {
        Count,
        Min,
        Max,
        Sum,
        Average
    }

    internal class AggregateExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Aggregate;

        public override Type Type { get; }

        internal AggregateExpression(Type type, AggregateType aggType, Expression argument, bool isDistinct)
        {
            Type = type;

            AggregateType = aggType;
            Argument = argument;
            IsDistinct = isDistinct;
        }
        internal AggregateType AggregateType { get; }
        internal Expression Argument { get; }
        internal bool IsDistinct { get; }
    }

    internal class AggregateSubqueryExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.AggregateSubquery;

        public override Type Type { get; }

        internal AggregateSubqueryExpression(string groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
        {
            Type = aggregateAsSubquery.Type;

            AggregateInGroupSelect = aggregateInGroupSelect;
            GroupByAlias = groupByAlias;
            AggregateAsSubquery = aggregateAsSubquery;
        }
        internal string GroupByAlias { get; }
        internal Expression AggregateInGroupSelect { get; }
        internal ScalarExpression AggregateAsSubquery { get; }
    }

    internal class IsNullExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.IsNull;

        public override Type Type => typeof(bool);

        internal IsNullExpression(Expression expression)
        {
            Expression = expression;
        }
        internal Expression Expression { get; }
    }

    internal class BetweenExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Between;

        public override Type Type { get; }

        internal BetweenExpression(Expression expression, Expression lower, Expression upper)
        {
            Type = expression.Type;

            Expression = expression;
            Lower = lower;
            Upper = upper;
        }
        internal Expression Expression { get; }
        internal Expression Lower { get; }
        internal Expression Upper { get; }
    }

    internal class RowNumberExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.RowCount;

        public override Type Type => typeof(int);

        internal RowNumberExpression(IEnumerable<OrderExpression>? orderBy)
        {
            if (orderBy != null)
            {
                OrderBy = orderBy as ReadOnlyCollection<OrderExpression> ?? new List<OrderExpression>(orderBy).AsReadOnly();
            }
        }
        internal ReadOnlyCollection<OrderExpression>? OrderBy { get; }
    }

    internal class NamedValueExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.NamedValue;

        public override Type Type { get; }

        internal NamedValueExpression(string name, Expression value)
        {
            Type = value.Type;

            Name = name;
            Value = value;
        }
        internal string Name { get; }
        internal Expression Value { get; }
    }

    internal class ProjectionExpression : Expression
    {
        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Projection;

        public override Type Type { get; }

        internal ProjectionExpression(SelectExpression source, Expression projector)
            : this(source, projector, null)
        {
        }
        internal ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression? aggregator)
        {
            Type = aggregator != null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type);

            Source = source;
            Projector = projector;
            Aggregator = aggregator;
        }
        internal SelectExpression Source { get; }
        internal Expression Projector { get; }
        internal LambdaExpression? Aggregator { get; }
    }
}
