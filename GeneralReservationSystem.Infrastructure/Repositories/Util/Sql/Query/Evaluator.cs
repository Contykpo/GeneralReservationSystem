using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    public static class Evaluator
    {
        public static Expression? PartialEval(Expression? expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }

        public static Expression? PartialEval(Expression? expression)
        {
            return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        private class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression?> candidates;

            internal SubtreeEvaluator(HashSet<Expression?> candidates)
            {
                this.candidates = candidates;
            }

            internal Expression? Eval(Expression? expression)
            {
                return Visit(expression);
            }

            public override Expression? Visit(Expression? expression)
            {
                return expression == null ? null : candidates.Contains(expression) ? Evaluate(expression) : base.Visit(expression);
            }

            private static Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }

                LambdaExpression lambda = Expression.Lambda(e);
                Delegate fn = lambda.Compile();

                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }

        private class Nominator : ExpressionVisitor
        {
            private readonly Func<Expression, bool> fnCanBeEvaluated;
            private HashSet<Expression?> candidates;
            private bool cannotBeEvaluated;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.fnCanBeEvaluated = fnCanBeEvaluated;
                candidates = [];
            }

            internal HashSet<Expression?> Nominate(Expression? expression)
            {
                candidates = [];
                _ = Visit(expression);
                return candidates;
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
