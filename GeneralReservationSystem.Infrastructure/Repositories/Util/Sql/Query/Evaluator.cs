using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    public static class Evaluator
    {
        public static Expression? PartialEval(Expression? expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return SubtreeEvaluator.Eval(Nominator.Nominate(fnCanBeEvaluated, expression), expression);
        }

        public static Expression? PartialEval(Expression? expression)
        {
            return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        private class SubtreeEvaluator : DbExpressionVisitor
        {
            private readonly HashSet<Expression> candidates;

            private SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal static Expression? Eval(HashSet<Expression> candidates, Expression? exp)
            {
                return new SubtreeEvaluator(candidates).Visit(exp);
            }

            public override Expression? Visit(Expression? exp)
            {
                return exp == null ? null : candidates.Contains(exp) ? Evaluate(exp) : base.Visit(exp);
            }

            private static Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                Type type = e.Type;
                if (type.IsValueType)
                {
                    e = Expression.Convert(e, typeof(object));
                }
                Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(e);
                Func<object> fn = lambda.Compile();
                return Expression.Constant(fn(), type);
            }
        }

        private class Nominator : DbExpressionVisitor
        {
            private readonly Func<Expression, bool> fnCanBeEvaluated;
            private readonly HashSet<Expression> candidates;
            private bool cannotBeEvaluated;

            private Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                candidates = [];
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal static HashSet<Expression> Nominate(Func<Expression, bool> fnCanBeEvaluated, Expression? expression)
            {
                Nominator nominator = new(fnCanBeEvaluated);
                _ = nominator.Visit(expression);
                return nominator.candidates;
            }

            public override Expression? Visit(Expression? expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = cannotBeEvaluated;
                    cannotBeEvaluated = false;
                    _ = base.Visit(expression);
                    if (!cannotBeEvaluated)
                    {
                        if (fnCanBeEvaluated(expression))
                        {
                            _ = candidates.Add(expression);
                        }
                        else
                        {
                            cannotBeEvaluated = true;
                        }
                    }
                    cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }
    }
}