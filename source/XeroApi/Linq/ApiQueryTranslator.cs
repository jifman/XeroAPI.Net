﻿using System;
using System.Linq;
using System.Linq.Expressions;

namespace XeroApi.Linq
{
    internal class ApiQueryTranslator : ExpressionVisitor
    {
        private ApiQueryDescription _query;
        private ApiQuerystringName _currentQueryStringName = ApiQuerystringName.Unknown;

        
        internal ApiQueryDescription Translate(Expression expression)
        {
            _query = new ApiQueryDescription();

            if (expression.Type.IsGenericType)
            {
                _query.ElementType = expression.Type.GetGenericArguments()[0];
            }
            
            Visit(expression);

            return _query;
        }
        
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Where":

                    using (new QuerystringScope(this, ApiQuerystringName.Where))
                    {
                        return base.VisitMethodCall(m);
                    }

                case "FirstOrDefault":
                case "First":

                    if (!string.IsNullOrEmpty(_query.ClientSideExpression) && !_query.ClientSideExpression.Equals(m.Method.Name))
                    {
                        throw new NotImplementedException("Only 1 aggregator expression can currently be performed.");
                    }

                    _query.ClientSideExpression = m.Method.Name;

                    using (new QuerystringScope(this, ApiQuerystringName.Where))
                    {
                        return base.VisitMethodCall(m);
                    }

                case "Count":

                    if (!string.IsNullOrEmpty(_query.ClientSideExpression) && !_query.ClientSideExpression.Equals(m.Method.Name))
                    {
                        throw new NotImplementedException("Only 1 aggregator expression can currently be performed.");
                    }

                    _query.ClientSideExpression = m.Method.Name;
                    return base.VisitMethodCall(m);
                    
                case "OrderBy":
                case "ThenBy":

                    using (new QuerystringScope(this, ApiQuerystringName.OrderBy))
                    {
                        return base.VisitMethodCall(m);
                    }
                    
                case "OrderByDescending":
                case "ThenByDescending":

                    using (new QuerystringScope(this, ApiQuerystringName.OrderBy))
                    {
                        var expression = base.VisitMethodCall(m);
                        Append(" DESC");
                        return expression;
                    }

                case "LongCount":
                case "Select":
                case "Take":
                case "Skip":
                case "Single":
                case "SingleOrDefault":
                case "SelectMany":
                case "Join":
                case "GroupJoin":
                case "GroupBy":
                case "Distinct":
                case "Min":
                case "Max":
                case "Sum":
                case "Average":
                case "Aggregate":
                case "Any":
                case "All":
                case "Except":
                case "Intersect":
                case "Union":
                case "OfType":
                case "ElementAt":
                case "Reverse":
                case "WithAggregate":
                case "IncludeDeleted":
                case "WithProviderOptions":
                case "WithHints":
                case "Concat":
                case "SequenceEqual":
                case "TakeWhile":
                case "SkipWhile":
                case "DefaultIfEmpty":
                case "Contains":
                case "Cast":
                    throw new NotImplementedException(string.Format("The method '{0}' can't currently be used in a XeroApi WHERE querystring.", m.Method.Name));
            }

            // Static method call
            EvaluateAndAppendSymbol(m);
            return m;
        }


        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    Append(" NOT ");
                    Visit(u.Operand);
                    break;
                default:
                    Visit(u.Operand);
                    break;
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            MemberExpression mExp = b.Left as MemberExpression;
            
            // Check if the LHS is an ItemId, ItemNumber or UpdatedDate. If so, record away from the main where clause.
            if (mExp != null && (mExp.Member.DeclaringType.Name == _query.ElementName))
            {
                if (mExp.Member.Name == _query.ElementIdProperty.SafeName())
                {
                    _query.ElementId = EvaluateExpression<Guid>(b.Right).ToString();
                    return b;
                }
                if (mExp.Member.Name == _query.ElementNumberProperty.SafeName())
                {
                    _query.ElementId = EvaluateExpression<object>(b.Right).ToString();
                    return b;
                }
                if (mExp.Member.Name == _query.ElementUpdatedDateProperty.SafeName())
                {
                    _query.ElementUpdatedDate = EvaluateExpression<DateTime>(b.Right);
                    return b;
                }
            }


            // Otherswise, parse as a normal (operand1 operator operand2)
            Append("(");
            Visit(b.Left);
            switch (b.NodeType)
            {
                case ExpressionType.And:
                    Append(" AND ");
                    break;
                case ExpressionType.Or:
                    Append(" OR ");
                    break;
                case ExpressionType.Equal:
                    Append(" == ");
                    break;
                case ExpressionType.NotEqual:
                    Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
            }
            Visit(b.Right);
            Append(")");
            return b;
        }



        protected override Expression VisitConstant(ConstantExpression c)
        {
            IQueryable q = c.Value as IQueryable;
            if (q != null)
            {
                // sb.Append("SELECT * FROM ");
                // sb.Append(q.ElementType.Name);
                _query.ElementType = q.ElementType;
            }
            else if (c.Value == null)
            {
                Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        Append(((bool)c.Value) ? "true" : "false");
                        break;

                    case TypeCode.String:
                        Append("\"");
                        Append(c.Value.ToString());
                        Append("\"");
                        break;

                    case TypeCode.DateTime:
                        //DateTime value = (DateTime) c.Value;
                        //sb.Append(string.Format("DateTime({0},{1},{2})", value.Year, value.Month, value.Day));
                        //break;
                        return c;

                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                    default:
                        Append(c.Value.ToString());
                        break;
                }
            }

            return c;
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            EvaluateAndAppendSymbol(nex);
            return nex;
        }
        
        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
            {
                Append(m.Member.Name);
                return m;
            }
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.MemberAccess)
            {
                Append(m.Member.DeclaringType.Name + "." + m.Member.Name);
                return m;
            }
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Constant)
            {
                EvaluateAndAppendSymbol(m);
                return m;
            }

            throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }



        protected void EvaluateAndAppendSymbol(Expression exp)
        {
            switch (exp.Type.Name)
            {
                case "DateTime":
                    DateTime date = EvaluateExpression<DateTime>(exp);

                    if (date.Date == date)
                        Append(string.Format("DateTime({0},{1},{2})", date.Year, date.Month, date.Day));
                    else
                        Append(string.Format("DateTime({0},{1},{2},{3},{4},{5})", date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second));

                    return;

                case "Guid":
                    Guid guid = EvaluateExpression<Guid>(exp);

                    if (guid == Guid.Empty)
                        Append("Guid.Empty");
                    else
                        Append(string.Format("Guid(\"{0}\")", guid));

                    return;

                case "String" :
                    string value = EvaluateExpression<string>(exp);

                    if (value == null)
                        Append("null");
                    else
                        Append(string.Format("\"{0}\"", value));

                    return;
            }

            // Does the function return a single element??
            if (exp.Type.Equals(_query.ElementType))
            {
                // ??
            }
            
            throw new NotSupportedException(string.Format("The Expression return type '{0}' is not supported", exp.Type.Name));
        }

        private static T EvaluateExpression<T>(Expression expression)
        {
            Expression<Func<T>> lambda = Expression.Lambda<Func<T>>(expression);
            Func<T> func = lambda.Compile();
            return (func).Invoke();
        }


        private void Append(string term)
        {
            _query.AppendTerm(term, _currentQueryStringName);
        }

        private class QuerystringScope : IDisposable
        {
            private readonly ApiQuerystringName _originalQuerystringName;
            private readonly ApiQueryTranslator _queryTranslator;

            public QuerystringScope(ApiQueryTranslator queryTranslator, ApiQuerystringName querystringName)
            {
                _originalQuerystringName = queryTranslator._currentQueryStringName;
                _queryTranslator = queryTranslator;

                _queryTranslator._currentQueryStringName = querystringName;
            }

            public void Dispose()
            {
                _queryTranslator._currentQueryStringName = _originalQuerystringName;
            }
        }

    }
}