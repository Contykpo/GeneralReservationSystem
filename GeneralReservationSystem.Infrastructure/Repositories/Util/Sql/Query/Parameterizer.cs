using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class Parameterizer : DbExpressionVisitor
    {
        private readonly Dictionary<object, NamedValueExpression> map = [];
        private readonly Dictionary<ParameterExpression, NamedValueExpression> pmap = [];

        private Parameterizer()
        {
        }

        internal static Expression? Parameterize(Expression? expression)
        {
            return new Parameterizer().Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression select = (SelectExpression)Visit(proj.Source)!;
            return select != proj.Source ? new ProjectionExpression(select, proj.Projector, proj.Aggregator) : proj;
        }

        private int iParam = 0;
        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value != null && !IsNumeric(c.Value.GetType()))
            {
                if (!map.TryGetValue(c.Value, out NamedValueExpression? nv))
                {
                    string name = "p" + iParam++;
                    nv = new NamedValueExpression(name, c);
                    map.Add(c.Value, nv);
                }
                return nv;
            }
            return c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            if (!pmap.TryGetValue(p, out NamedValueExpression? nv))
            {
                string name = "p" + iParam++;
                nv = new NamedValueExpression(name, p);
                pmap.Add(p, nv);
            }
            return nv;
        }

        private static bool IsNumeric(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean or TypeCode.Byte or TypeCode.Decimal or TypeCode.Double or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.SByte or TypeCode.Single or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
                _ => false,
            };
        }
    }

    internal class NamedValueGatherer : DbExpressionVisitor
    {
        private readonly HashSet<NamedValueExpression> namedValues = [];

        private NamedValueGatherer()
        {
        }

        internal static ReadOnlyCollection<NamedValueExpression> Gather(Expression expr)
        {
            NamedValueGatherer gatherer = new();
            _ = gatherer.Visit(expr);
            return gatherer.namedValues.ToList().AsReadOnly();
        }

        protected override Expression VisitNamedValue(NamedValueExpression value)
        {
            _ = namedValues.Add(value);
            return value;
        }
    }
}