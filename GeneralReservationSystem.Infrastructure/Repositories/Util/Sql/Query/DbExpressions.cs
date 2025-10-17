using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal enum DbExpressionType
    {
        Table = 1000, // To not overlap with ExpressionType
        Column,
        Select,
        Projection,
        Join
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
        private readonly Type type;

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Table;

        public override Type Type => type;

        internal TableExpression(Type type, string alias, string name)
        {
            Alias = alias;
            Name = name;

            this.type = type;
        }

        internal string Alias { get; }

        internal string Name { get; }
    }

    internal class ColumnExpression : Expression
    {
        private readonly Type type;

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Column;

        public override Type Type => type;

        internal ColumnExpression(Type type, string alias, string name, int ordinal)
        {
            Alias = alias;
            Name = name;
            Ordinal = ordinal;

            this.type = type;
        }

        internal string Alias { get; }

        internal string Name { get; }

        internal int Ordinal { get; }
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

    internal class SelectExpression : Expression
    {
        private readonly Type type;

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Select;

        public override Type Type => type;

        internal SelectExpression(Type type, string alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression? where)
        {
            Alias = alias;
            Columns = columns as ReadOnlyCollection<ColumnDeclaration> ?? new List<ColumnDeclaration>(columns).AsReadOnly();

            From = from;
            Where = where;

            this.type = type;
        }

        internal string Alias { get; }

        internal ReadOnlyCollection<ColumnDeclaration> Columns { get; }

        internal Expression From { get; }

        internal Expression? Where { get; }
    }

    internal class ProjectionExpression : Expression
    {
        private readonly Type type;

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Projection;

        public override Type Type => type;

        internal ProjectionExpression(SelectExpression source, Expression projector)
        {
            Source = source;
            Projector = projector;

            type = projector.Type;
        }

        internal SelectExpression Source { get; }

        internal Expression Projector { get; }
    }

    internal enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply,
    }

    internal class JoinExpression : Expression
    {
        private readonly Type type;

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Join;

        public override Type Type => type;

        internal JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression? condition)
        {
            this.Join = joinType;
            this.Left = left;
            this.Right = right;
            this.Condition = condition;

            this.type = type;
        }
        internal JoinType Join { get; }
        internal Expression Left { get; }
        internal Expression Right { get; }
        internal new Expression? Condition { get; }
    }
}
