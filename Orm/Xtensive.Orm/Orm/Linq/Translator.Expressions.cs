// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.02.27

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm.FullTextSearchCondition.Interfaces;
using Xtensive.Orm.FullTextSearchCondition.Internals;
using Xtensive.Orm.FullTextSearchCondition.Nodes;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Linq.Expressions;
using Xtensive.Orm.Linq.Expressions.Visitors;
using Xtensive.Orm.Linq.Materialization;
using Xtensive.Orm.Linq.Rewriters;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using Xtensive.Reflection;
using FieldInfo = System.Reflection.FieldInfo;
using Tuple = Xtensive.Tuples.Tuple;
using TypeInfo = Xtensive.Orm.Model.TypeInfo;

namespace Xtensive.Orm.Linq
{
  internal sealed partial class Translator
  {
    private static readonly IReadOnlyList<ParameterExpression> ParameterContextParams = [Expression.Parameter(WellKnownOrmTypes.ParameterContext, "context")];
    private static readonly ConstantExpression
      NullExpression = Expression.Constant(null),
      NullKeyExpression = Expression.Constant(null, WellKnownOrmTypes.Key),
      FalseExpression = Expression.Constant(false),
      TrueExpression = Expression.Constant(true);

    private static IReadOnlyDictionary<Parameter<Tuple>, Tuple> EmptyTupleParameterBindings { get; } = new Dictionary<Parameter<Tuple>, Tuple>();

    protected override Expression VisitTypeBinary(TypeBinaryExpression tb)
    {
      if (tb.NodeType != ExpressionType.TypeIs) {
        return base.VisitTypeBinary(tb);
      }

      var expression = tb.Expression;
      Type expressionType = expression.Type;
      Type operandType = tb.TypeOperand;
      if (operandType.IsAssignableFrom(expressionType)) {
        return TrueExpression;
      }

      // Structure
      var memberType = expression.GetMemberType();
      if (memberType == MemberType.Structure
        && WellKnownOrmTypes.Structure.IsAssignableFrom(operandType)) {
        return FalseExpression;
      }

      // Entity
      if (memberType == MemberType.Entity
          && WellKnownOrmInterfaces.Entity.IsAssignableFrom(operandType)) {
        TypeInfo type = context.Model.Types[operandType];

        var typeInfos = type.AllDescendants.ToHashSet();
        typeInfos.UnionWith(type.AllImplementors);
        _ = typeInfos.Add(type);
        var typeIds = typeInfos.Select(context.TypeIdRegistry.GetTypeId);
        Expression memberExpression = Expression.MakeMemberAccess(expression, WellKnownMembers.TypeId);
        Expression boolExpression = null;
        foreach (int typeId in typeIds) {
          boolExpression = MakeBooleanExpression(
            boolExpression,
            memberExpression,
            Expr.Constant(typeId),
            ExpressionType.Equal,
            ExpressionType.OrElse);
        }

        return Visit(boolExpression);
      }

      throw new NotSupportedException(Strings.ExTypeIsMethodSupportsOnlyEntitiesAndStructures);
    }

    public override Expression Visit(Expression e)
    {
      if (e == null)
        return null;
      if (e.StripMarkers().IsProjection())
        return e;
      if (context.Evaluator.CanBeEvaluated(e)) {
        if (WellKnownInterfaces.Queryable.IsAssignableFrom(e.Type))
          return base.Visit(ExpressionEvaluator.Evaluate(e));
        return context.ParameterExtractor.IsParameter(e)
          ? e
          : ExpressionEvaluator.Evaluate(e);
      }
      return base.Visit(e);
    }

    protected override Expression VisitUnknown(Expression e)
    {
      if (e is ExtendedExpression)
        return e;
      return base.VisitUnknown(e);
    }

    /// <exception cref="NotSupportedException"><c>NotSupportedException</c>.</exception>
    protected override Expression VisitUnary(UnaryExpression u)
    {
      switch (u.NodeType) {
        case ExpressionType.TypeAs:
          if (u.GetMemberType() == MemberType.Entity)
            return VisitTypeAs(u.Operand, u.Type);
          break;
        case ExpressionType.Convert:
        case ExpressionType.ConvertChecked:
          if (u.GetMemberType() == MemberType.Entity) {
            if (u.Type == u.Operand.Type
              || u.Type.IsAssignableFrom(u.Operand.Type)
              || !WellKnownOrmInterfaces.Entity.IsAssignableFrom(u.Operand.Type))
              return base.VisitUnary(u);
            throw new InvalidOperationException(String.Format(Strings.ExDowncastFromXToXNotSupportedUseOfTypeOrAsOperatorInstead, u, u.Operand.Type, u.Type));
          }
          else if (u.Type == WellKnownTypes.Object && State.ShouldOmitConvertToObject) {
            var expression = u.StripCasts();
            return Visit(expression);
          }
          break;
      }
      return u.Type == WellKnownInterfaces.Queryable
        ? Visit(u.Operand)
        : base.VisitUnary(u);
    }

    protected override Expression VisitLambda<T>(Expression<T> le)
    {
      using (CreateLambdaScope(le, allowCalculableColumnCombine: false)) {
        Expression body = le.Body;
        if (!State.IsTailMethod)
          body = NullComparisonRewriter.Rewrite(body);
        body = Visit(body);
        ParameterExpression parameter = le.Parameters[0];
        var nodeType = body.NodeType;
        var shouldTranslate =
          (nodeType != ExpressionType.New || body.IsNewExpressionSupportedByStorage())
          && nodeType != ExpressionType.MemberInit
          && !(nodeType == ExpressionType.Constant && State.BuildingProjection);
        if (shouldTranslate)
          body = body.StripMarkers().IsProjection()
            ? BuildSubqueryResult((ProjectionExpression) body, le.Body.Type)
            : ProcessProjectionElement(body);
        ProjectionExpression projection = context.Bindings[parameter];
        return new ItemProjectorExpression(body, projection.ItemProjector.DataSource, context);
      }
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment ma)
    {
      var maExpression = ma.Expression;
      Expression expression;
      using (CreateScope(new TranslatorState(State) { CalculateExpressions = false })) {
        expression = Visit(maExpression);
      }

      expression = expression.StripMarkers().IsProjection()
        ? BuildSubqueryResult((ProjectionExpression) expression, maExpression.Type)
        : ProcessProjectionElement(expression);

      if (expression != maExpression)
        return Expression.Bind(ma.Member, expression);
      return ma;
    }

    /// <exception cref="NotSupportedException"><c>NotSupportedException</c>.</exception>
    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
      Expression left;
      Expression right;
      var (binaryLeft, binaryRight) = (binaryExpression.Left, binaryExpression.Right);
      MemberType memberType = binaryLeft.Type == WellKnownTypes.Object
        ? binaryRight.GetMemberType()
        : binaryLeft.GetMemberType();
      if (memberType == MemberType.EntitySet) {
        if (context.Evaluator.CanBeEvaluated(binaryLeft)) {
          left = ExpressionEvaluator.Evaluate(binaryLeft);
        }
        else {
          left = binaryLeft is MemberExpression leftMemberAccess && leftMemberAccess.Member.ReflectedType.IsClosure()
            ? ExpressionEvaluator.Evaluate(leftMemberAccess)
            : Visit(binaryLeft);
        }
        if (context.Evaluator.CanBeEvaluated(binaryRight)) {
          right = ExpressionEvaluator.Evaluate(binaryRight);
        }
        else {
          right = binaryRight is MemberExpression rightMemberAccess && rightMemberAccess.Member.ReflectedType.IsClosure()
            ? ExpressionEvaluator.Evaluate(rightMemberAccess)
            : Visit(binaryRight);
        }
      }
      else if (memberType is MemberType.Entity or MemberType.Structure) {
        if (binaryExpression.NodeType == ExpressionType.Coalesce) {
          if ((context.Evaluator.CanBeEvaluated(binaryRight) && !(binaryRight is ConstantExpression))
            || (context.Evaluator.CanBeEvaluated(binaryLeft) && !(binaryLeft is ConstantExpression)))
            throw new NotSupportedException(
              string.Format(Strings.ExXExpressionsWithConstantValuesOfYTypeNotSupported, "Coalesce", memberType.ToString()));

          return Visit(Expression.Condition(
            Expression.NotEqual(binaryLeft, NullExpression),
            binaryLeft,
            binaryRight));
        }
        else {
          left = Visit(binaryLeft);
          right = Visit(binaryRight);
        }
      }
      else if (EnumRewritableOperations(binaryExpression)) {
        // Following two checks for enums are here to improve result query
        // performance because they let not to cast columns to integer.
        var leftNoCasts = binaryLeft.StripCasts();
        var leftNoCastsType = leftNoCasts.Type;
        var bareLeftType = leftNoCastsType.StripNullable();
        var rightNoCasts = binaryRight.StripCasts();
        var rightNoCastsType = rightNoCasts.Type;
        var bareRightType = rightNoCastsType.StripNullable();

        if (bareLeftType.IsEnum && rightNoCasts.NodeType == ExpressionType.Constant) {
          var typeToCast = leftNoCastsType.IsNullable()
            ? bareLeftType.GetEnumUnderlyingType().ToNullable()
            : leftNoCastsType.GetEnumUnderlyingType();
          left = Visit(Expression.Convert(leftNoCasts, typeToCast));
          right = Visit(Expression.Convert(binaryRight, typeToCast));
        }
        else if (bareRightType.IsEnum && leftNoCasts.NodeType == ExpressionType.Constant) {
          var typeToCast = rightNoCastsType.IsNullable()
            ? bareRightType.GetEnumUnderlyingType().ToNullable()
            : rightNoCastsType.GetEnumUnderlyingType();
          left = Visit(Expression.Convert(rightNoCasts, typeToCast));
          right = Visit(Expression.Convert(binaryLeft, typeToCast));
        }
        else {
          left = Visit(binaryLeft);
          right = Visit(binaryRight);
        }
      }
      else {
        left = Visit(binaryLeft);
        right = Visit(binaryRight);
      }
      var resultBinaryExpression = Expression.MakeBinary(binaryExpression.NodeType,
        left,
        right,
        binaryExpression.IsLiftedToNull,
        binaryExpression.Method);

      if (binaryExpression.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
        return VisitBinaryRecursive(resultBinaryExpression, binaryExpression);

      if (binaryExpression.NodeType == ExpressionType.ArrayIndex) {
        if (left.StripCasts() is NewArrayExpression newArrayExpression
            && right.StripCasts() is ConstantExpression indexExpression
            && indexExpression.Type == WellKnownTypes.Int32)
          return newArrayExpression.Expressions[(int) indexExpression.Value];

        throw new NotSupportedException(String.Format(Strings.ExBinaryExpressionXOfTypeXIsNotSupported, binaryExpression.ToString(true), binaryExpression.NodeType));
      }

      return resultBinaryExpression;

      static bool EnumRewritableOperations(BinaryExpression b)
      {
        return b.NodeType is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
          or ExpressionType.LessThan or ExpressionType.LessThanOrEqual;
      }        
    }

    protected override Expression VisitConditional(ConditionalExpression c)
    {
      var memberType = c.IfTrue.Type == typeof(object)
        ? c.IfFalse.GetMemberType()
        : c.IfTrue.GetMemberType();
      if (memberType == MemberType.Entity || memberType == MemberType.Structure) {
        if ((context.Evaluator.CanBeEvaluated(c.IfFalse) && !(c.IfFalse is ConstantExpression))
          || (context.Evaluator.CanBeEvaluated(c.IfTrue) && !(c.IfTrue is ConstantExpression)))
          throw new NotSupportedException(string.Format(Strings.ExXExpressionsWithConstantValuesOfYTypeNotSupported, "Conditional", memberType.ToString()));
      }
      return base.VisitConditional(c);
    }

    private Expression ConvertEnum(Expression left)
    {
      var underlyingType = Enum.GetUnderlyingType(left.Type.StripNullable());
      if (left.Type.IsNullable())
        underlyingType = underlyingType.ToNullable();
      left = left.NodeType == ExpressionType.Convert
        ? Expression.Convert(((UnaryExpression) left).Operand, underlyingType)
        : Expression.Convert(left, underlyingType);
      return left;
    }

    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    protected override Expression VisitParameter(ParameterExpression p)
    {
      bool isInnerParameter = State.Parameters.Contains(p);
      bool isOuterParameter = State.OuterParameters.Contains(p);

      if (!isInnerParameter && !isOuterParameter)
        throw new InvalidOperationException(Strings.ExLambdaParameterIsOutOfScope);
      ItemProjectorExpression itemProjector = context.Bindings[p].ItemProjector;
      if (isOuterParameter)
        return context.GetBoundItemProjector(p, itemProjector).Item;
      return itemProjector.Item;
    }

    protected override Expression VisitMember(MemberExpression ma)
    {
      var memberInfo = ma.Member;
      var sourceExpression = ma.Expression;

      if (sourceExpression != null) {
        if (sourceExpression.Type != memberInfo.ReflectedType
          && memberInfo is PropertyInfo
          && !memberInfo.ReflectedType.IsInterface) {
          ma = Expression.MakeMemberAccess(
            sourceExpression, sourceExpression.Type.GetProperty(memberInfo.Name, memberInfo.GetBindingFlags()));

          memberInfo = ma.Member;
          sourceExpression = ma.Expression;
        }
      }

      var customCompiler = context.CustomCompilerProvider.GetCompiler(memberInfo);

      // Reflected type doesn't have custom compiler defined, so falling back to base class compiler
      var declaringType = memberInfo.DeclaringType;
      var reflectedType = memberInfo.ReflectedType;
      if (customCompiler == null && declaringType != reflectedType && declaringType.IsAssignableFrom(reflectedType)) {
        var root = declaringType;
        var current = reflectedType;
        while (current != root && customCompiler == null) {
          current = current.BaseType;
          var member = current.GetProperty(memberInfo.Name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
          customCompiler = context.CustomCompilerProvider.GetCompiler(member);
        }
      }

      if (customCompiler != null) {
        var expression = customCompiler.Invoke(sourceExpression, Array.Empty<Expression>());
        if (expression == null) {
          if (reflectedType.IsInterface)
            return Visit(BuildInterfaceExpression(ma));
          if (reflectedType.IsClass)
            return Visit(BuildHierarchyExpression(ma));
        }
        else
          return Visit(expression);
      }

      if (context.Evaluator.CanBeEvaluated(ma) && context.ParameterExtractor.IsParameter(ma)) {
        if (WellKnownInterfaces.Queryable.IsAssignableFrom(ma.Type)) {
          var lambda = FastExpression.Lambda<Func<IQueryable>>(ma).CachingCompile();
          var rootPoint = lambda();
          if (rootPoint != null)
            return base.Visit(rootPoint.Expression);
        }
        return ma;
      }
      if (ma.Expression == null) {
        if (WellKnownInterfaces.Queryable.IsAssignableFrom(ma.Type)) {
          var lambda = FastExpression.Lambda<Func<IQueryable>>(ma).CachingCompile();
          var rootPoint = lambda();
          if (rootPoint != null)
            return VisitSequence(rootPoint.Expression);
        }
      }
      else if (sourceExpression.NodeType == ExpressionType.Constant) {
        if (memberInfo is FieldInfo rfi && (rfi.FieldType.IsGenericType && WellKnownInterfaces.Queryable.IsAssignableFrom(rfi.FieldType))) {
          var lambda = FastExpression.Lambda<Func<IQueryable>>(ma).CachingCompile();
          var rootPoint = lambda();
          if (rootPoint != null)
            return VisitSequence(rootPoint.Expression);
        }
      }
      else if (sourceExpression.GetMemberType() == MemberType.Entity && memberInfo.Name != "Key") {
        var type = sourceExpression.Type;
        if (sourceExpression is ParameterExpression parameter) {
          var projection = context.Bindings[parameter];
          type = projection.ItemProjector.Item.Type;
        }
        if (!context.Model.Types[type].Fields.Contains(context.Domain.Handlers.NameBuilder.BuildFieldName((PropertyInfo) memberInfo))) {
          throw new NotSupportedException(string.Format(Strings.ExFieldMustBePersistent, ma.ToString(true)));
        }
      }
      Expression source;
      using (CreateScope(new TranslatorState(State) { /* BuildingProjection = false */ })) {
        source = Visit(sourceExpression);
      }

      var result = context.CheckIfQueryReusePossible(memberInfo)
        ? GetMemberWithRemap(source, memberInfo, ma)
        : GetMember(source, memberInfo, ma);

      return result ?? base.VisitMember(ma);
    }

    protected override Expression VisitMethodCall(MethodCallExpression mc)
    {
      using (CreateScope(new TranslatorState(State) { IsTailMethod = mc == context.Query && mc.IsQuery() })) {
        var method = mc.Method;
        var customCompiler = context.CustomCompilerProvider.GetCompiler(method);
        if (customCompiler != null) {
          return Visit(customCompiler.Invoke(mc.Object, mc.Arguments.ToArray()));
        }

        var methodDeclaringType = method.DeclaringType;
        var methodName = method.Name;

        // Visit Query. Deprecated.
#pragma warning disable 612,618
        if (methodDeclaringType == WellKnownOrmTypes.Query) {
          // Query.All<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.All)) {
            return ConstructQueryable(mc);
          }

          // Query.FreeText<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.FreeTextString)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.FreeTextExpression)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.FreeTextExpressionTopNByRank)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.FreeTextStringTopNByRank)) {
            return ConstructFreeTextQueryRoot(method.GetGenericArguments()[0], mc.Arguments);
          }

          // Query.ContainsTable<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.ContainsTableExpr)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.ContainsTableExprWithColumns)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.ContainsTableExprTopNByRank)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.ContainsTableExprWithColumnsTopNByRank)) {
            return ConstructContainsTableQueryRoot(method.GetGenericArguments()[0], mc.Arguments);
          }

          // Query.Single<T> & Query.SingleOrDefault<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.SingleKey)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.Query.SingleOrDefaultKey)) {
            return VisitQuerySingle(mc);
          }

          throw new InvalidOperationException(String.Format(Strings.ExMethodCallExpressionXIsNotSupported, mc.ToString(true)));
        }
        // Visit QueryEndpoint.
        if (methodDeclaringType == typeof(QueryEndpoint)) {
          // Query.All<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.All)) {
            return ConstructQueryable(mc);
          }

          // Query.FreeText<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.FreeTextString)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.FreeTextExpression)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.FreeTextExpressionTopNByRank)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.FreeTextStringTopNByRank)) {
            return ConstructFreeTextQueryRoot(method.GetGenericArguments()[0], mc.Arguments);
          }

          // Query.ContainsTable<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.ContainsTableExpr)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.ContainsTableExprWithColumns)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.ContainsTableExprTopNByRank)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.ContainsTableExprWithColumnsTopNByRank)) {
            return ConstructContainsTableQueryRoot(method.GetGenericArguments()[0], mc.Arguments);
          }

          // Query.Single<T> & Query.SingleOrDefault<T>
          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.SingleKey)
            || method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.SingleOrDefaultKey)) {
            return VisitQuerySingle(mc);
          }

          if (method.IsGenericMethodSpecificationOf(WellKnownMembers.QueryEndpoint.Items)) {
            return VisitSequence(mc.Arguments[0].StripQuotes().Body, mc);
          }

          throw new InvalidOperationException(String.Format(Strings.ExMethodCallExpressionXIsNotSupported, mc.ToString(true)));
        }
#pragma warning restore 612,618

        // Visit Queryable extensions.
        if (methodDeclaringType == typeof(QueryableExtensions)) {
          return methodName switch {
            nameof(QueryableExtensions.LeftJoin) => VisitLeftJoin(mc),
            "In" => VisitIn(mc),
            nameof(QueryableExtensions.Lock) => VisitLock(mc),
            nameof(QueryableExtensions.Take) => VisitTake(mc.Arguments[0], mc.Arguments[1]),
            nameof(QueryableExtensions.Skip) => VisitSkip(mc.Arguments[0], mc.Arguments[1]),
            nameof(QueryableExtensions.ElementAt) => VisitElementAt(mc.Arguments[0], mc.Arguments[1], context.IsRoot(mc), method.ReturnType, false),
            nameof(QueryableExtensions.ElementAtOrDefault) => VisitElementAt(mc.Arguments[0], mc.Arguments[1], context.IsRoot(mc), method.ReturnType, true),
            nameof(QueryableExtensions.Count) => VisitAggregate(mc.Arguments[0], method, null, context.IsRoot(mc), mc),
            nameof(QueryableExtensions.Tag) => VisitTag(mc),
            nameof(QueryableExtensions.WithIndexHint) => VisitWithIndexHint(mc),
            _ => throw new InvalidOperationException(String.Format(Strings.ExMethodCallExpressionXIsNotSupported, mc.ToString(true)))
          };
        }
        // Visit Collection extensions
        if (methodDeclaringType == typeof(CollectionExtensionsEx)) {
          switch (methodName) {
            case nameof(CollectionExtensionsEx.ContainsAny):
              return VisitContainsAny(mc.Arguments[0], mc.Arguments[1], context.IsRoot(mc), method.GetGenericArguments()[0]);
            case nameof(CollectionExtensionsEx.ContainsAll):
              return VisitContainsAll(mc.Arguments[0], mc.Arguments[1], context.IsRoot(mc), method.GetGenericArguments()[0]);
            case nameof(CollectionExtensionsEx.ContainsNone):
              return VisitContainsNone(mc.Arguments[0], mc.Arguments[1], context.IsRoot(mc), method.GetGenericArguments()[0]);
          }
        }


        // Process local collections
        if (mc.Object.IsLocalCollection(context)) {
          // IList.Contains
          // List.Contains
          // Array.Contains
          ParameterInfo[] parameters = method.GetParameters();
          if (methodName=="Contains" && parameters.Length==1)
            return VisitContains(mc.Object, mc.Arguments[0], false);
        }

        var result = base.VisitMethodCall(mc);
        if (result != mc && result.NodeType == ExpressionType.Call) {
          var visitedMethodCall = (MethodCallExpression) result;
          if (visitedMethodCall.Arguments.Any(arg => arg.StripMarkers().IsProjection()))
            throw new InvalidOperationException(String.Format(Strings.ExMethodCallExpressionXIsNotSupported, mc.ToString(true)));
        }
        return result;
      }
    }

    private Expression ConstructFreeTextQueryRoot(Type elementType, System.Collections.ObjectModel.ReadOnlyCollection<Expression> expressions)
    {
      TypeInfo type;
      if (!context.Model.Types.TryGetValue(elementType, out type))
        throw new InvalidOperationException(String.Format(Strings.ExTypeNotFoundInModel, elementType.FullName));
      var fullTextIndex = type.FullTextIndex;
      if (fullTextIndex == null)
        throw new InvalidOperationException(String.Format(Strings.ExEntityDoesNotHaveFullTextIndex, elementType.FullName));
      var searchCriteria = expressions[0];
      if (compiledQueryScope != null
          && searchCriteria.NodeType == ExpressionType.Constant
          && searchCriteria.Type == WellKnownTypes.String)
        throw new InvalidOperationException(String.Format(Strings.ExFreeTextNotSupportedInCompiledQueries, ((ConstantExpression) searchCriteria).Value));

      // Prepare parameter
      Func<ParameterContext, string> compiledParameter;
      if (searchCriteria.NodeType == ExpressionType.Quote)
        searchCriteria = searchCriteria.StripQuotes();
      if (searchCriteria.Type == typeof(Func<string>)) {
        if (compiledQueryScope == null) {
          var originalSearchCriteria = (Expression<Func<string>>) searchCriteria;
          var body = originalSearchCriteria.Body;
          var searchCriteriaLambda = FastExpression.Lambda<Func<ParameterContext, string>>(body, ParameterContextParams);
          compiledParameter = searchCriteriaLambda.CachingCompile();
        }
        else {
          var replacer = compiledQueryScope.QueryParameterReplacer;
          var newSearchCriteria = (Expression<Func<string>>) replacer.Replace(searchCriteria);
          var searchCriteriaAccessor = ParameterAccessorFactory.CreateAccessorExpression<string>(newSearchCriteria.Body);
          compiledParameter = searchCriteriaAccessor.CachingCompile();
        }
      }
      else {
        var parameter = ParameterAccessorFactory.CreateAccessorExpression<string>(searchCriteria);
        compiledParameter = parameter.CachingCompile();
      }

      ColumnExpression rankExpression;
      FullTextExpression freeTextExpression;
      ItemProjectorExpression itemProjector;
      var fullFeatured = context.ProviderInfo.Supports(ProviderFeatures.FullFeaturedFullText);
      var entityExpression = EntityExpression.Create(type, 0, !fullFeatured);
      var rankColumnAlias = context.GetNextColumnAlias();

      FreeTextProvider dataSource;
      if (expressions.Count > 1) {
        var topNParameter = ParameterAccessorFactory.CreateAccessorExpression<int>(expressions[1]).CachingCompile();
        dataSource = new FreeTextProvider(fullTextIndex, compiledParameter, rankColumnAlias, topNParameter, fullFeatured);
      }
      else
        dataSource = new FreeTextProvider(fullTextIndex, compiledParameter, rankColumnAlias, fullFeatured);

      rankExpression = ColumnExpression.Create(WellKnownTypes.Double, (ColNum) (dataSource.Header.Columns.Count - 1));
      freeTextExpression = new FullTextExpression(fullTextIndex, entityExpression, rankExpression, null);
      itemProjector = new ItemProjectorExpression(freeTextExpression, dataSource, context);
      return new ProjectionExpression(WellKnownInterfaces.QueryableOfT.CachedMakeGenericType(elementType), itemProjector, EmptyTupleParameterBindings);
    }

    private Expression ConstructContainsTableQueryRoot(Type elementType, System.Collections.ObjectModel.ReadOnlyCollection<Expression> parameters)
    {
      TypeInfo type;
      if (!context.Model.Types.TryGetValue(elementType, out type))
        throw new InvalidOperationException(String.Format(Strings.ExTypeNotFoundInModel, elementType.FullName));
      var fullTextIndex = type.FullTextIndex;
      if (fullTextIndex == null)
        throw new InvalidOperationException(String.Format(Strings.ExEntityDoesNotHaveFullTextIndex, elementType.FullName));
      if (!context.ProviderInfo.Supports(ProviderFeatures.SingleKeyRankTableFullText))
        throw new NotSupportedException(Strings.ExCurrentProviderDoesNotSupportContainsTableFunctionality);

      Expression rawSearchCriteria;
      Expression searchColumns;
      Expression topNByRank;
      ArrangeContainsTableParameters(parameters, out rawSearchCriteria, out searchColumns, out topNByRank);
      IList<ColumnInfo> searchableColumns = (searchColumns != null)
        ? GetColumnsToSearch((ConstantExpression) searchColumns, elementType, type)
        : new List<ColumnInfo>();

      // Prepare parameters
      Func<ParameterContext, string> compiledParameter;
      if (rawSearchCriteria.NodeType == ExpressionType.Quote)
        rawSearchCriteria = rawSearchCriteria.StripQuotes();
      if (rawSearchCriteria.Type != typeof(Func<ConditionEndpoint, IOperand>))
        throw new InvalidOperationException(string.Format(Strings.ExUnsupportedExpressionType));

      var func = ((Expression<Func<ConditionEndpoint, IOperand>>) rawSearchCriteria).Compile();
      var conditionCompiler = context.Domain.Handler.GetSearchConditionCompiler();
      func.Invoke(SearchConditionNodeFactory.CreateConditonRoot()).AcceptVisitor(conditionCompiler);

      var preparedSearchCriteria = FastExpression.Lambda<Func<ParameterContext, string>>(
        Expression.Constant(conditionCompiler.CurrentOutput), ParameterContextParams);

      if (compiledQueryScope == null) {
        compiledParameter = preparedSearchCriteria.CachingCompile();
      }
      else {
        var replacer = compiledQueryScope.QueryParameterReplacer;
        var newSearchCriteria = replacer.Replace(preparedSearchCriteria);
        compiledParameter = ((Expression<Func<ParameterContext, string>>) newSearchCriteria).CachingCompile();
      }

      ColumnExpression rankExpression;
      FullTextExpression freeTextExpression;
      ItemProjectorExpression itemProjector;

      //var fullFeatured = context.ProviderInfo.Supports(ProviderFeatures.FullFeaturedFullText);
      var fullFeatured = false;// postgre provider has no analogue functionality by now.
      var entityExpression = EntityExpression.Create(type, 0, !fullFeatured);
      var rankColumnAlias = context.GetNextColumnAlias();

      ContainsTableProvider dataSource;
      if (topNByRank != null) {
        var topNParameter = ParameterAccessorFactory.CreateAccessorExpression<int>(parameters[1]).CachingCompile();
        dataSource = new ContainsTableProvider(fullTextIndex, compiledParameter, rankColumnAlias, searchableColumns, topNParameter, fullFeatured);
      }
      else
        dataSource = new ContainsTableProvider(fullTextIndex, compiledParameter, rankColumnAlias, searchableColumns, fullFeatured);

      rankExpression = ColumnExpression.Create(WellKnownTypes.Double, (ColNum) (dataSource.Header.Columns.Count - 1));
      freeTextExpression = new FullTextExpression(fullTextIndex, entityExpression, rankExpression, null);
      itemProjector = new ItemProjectorExpression(freeTextExpression, dataSource, context);
      return new ProjectionExpression(WellKnownInterfaces.QueryableOfT.CachedMakeGenericType(elementType), itemProjector, EmptyTupleParameterBindings);
    }

    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    protected override Expression VisitNew(NewExpression newExpression)
    {
      // ReSharper disable HeuristicUnreachableCode
      // ReSharper disable ConditionIsAlwaysTrueOrFalse

      var strippedMarkersExpression = newExpression.StripMarkers();
      var newExpressionMembers = newExpression.Members;
      if (newExpressionMembers is null && (strippedMarkersExpression.IsGroupingExpression()
                                           || strippedMarkersExpression.IsSubqueryExpression()
                                           || newExpression.IsNewExpressionSupportedByStorage())) {
        return base.VisitNew(newExpression);
      }

      // ReSharper restore ConditionIsAlwaysTrueOrFalse
      // ReSharper restore HeuristicUnreachableCode

      var arguments = VisitNewExpressionArguments(newExpression);
      if (strippedMarkersExpression.IsAnonymousConstructor()) {
        return newExpressionMembers is null
          ? Expression.New(newExpression.Constructor, arguments)
          : Expression.New(newExpression.Constructor, arguments, newExpressionMembers);
      }

      var constructorParameters = newExpression.GetConstructorParameters();
      if (constructorParameters.Length != arguments.Count)
        throw Exceptions.InternalError(Strings.ExInvalidNumberOfParametersInNewExpression, OrmLog.Instance);

      var bindings = GetBindingsForConstructor(constructorParameters, arguments, newExpression);
      var nativeBindings = new Dictionary<MemberInfo, Expression>();
      return new ConstructorExpression(newExpression.Type, bindings, nativeBindings, newExpression.Constructor, arguments);
    }

    internal static bool FilterBindings(MemberInfo mi, string name, Type type)
    {
      var result = String.Equals(mi.Name, name, StringComparison.InvariantCultureIgnoreCase);
      if (!result)
        return false;

      result = mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property;
      if (!result)
        return false;

      var field = mi as FieldInfo;
      if (field != null)
        return field.FieldType == type && !field.IsInitOnly;
      var property = mi as PropertyInfo;
      if (property == null)
        return false;
      return property.PropertyType.IsAssignableFrom(type) && property.CanWrite;
    }

    #region Private helper methods

    private Dictionary<MemberInfo, Expression> GetBindingsForConstructor(ParameterInfo[] constructorParameters, IReadOnlyList<Expression> constructorArguments, Expression newExpression)
    {
      var bindings = new Dictionary<MemberInfo, Expression>();
      var duplicateMembers = new HashSet<MemberInfo>();
      var typeMembers = newExpression.Type.GetMembers();
      for (var parameterIndex = 0; parameterIndex < constructorParameters.Length; parameterIndex++) {
        var constructorParameter = constructorParameters[parameterIndex];
        var members = typeMembers
          .Where(mi => FilterBindings(mi, constructorParameter.Name, constructorParameter.ParameterType))
          .ToList();
        if (members.Count != 1 || duplicateMembers.Contains(members[0]))
          continue;
        if (bindings.Remove(members[0])) {
          duplicateMembers.Add(members[0]);
        }
        else
          bindings.Add(members[0], constructorArguments[parameterIndex]);
      }
      return bindings;
    }

    /// <exception cref="NotSupportedException"><c>NotSupportedException</c>.</exception>
    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    private Expression VisitBinaryRecursive(BinaryExpression binaryExpression, BinaryExpression originalBinaryExpression)
    {
      if (context.Evaluator.CanBeEvaluated(binaryExpression))
        return context.ParameterExtractor.IsParameter(binaryExpression)
          ? (Expression) binaryExpression
          : ExpressionEvaluator.Evaluate(binaryExpression);

      Expression left = binaryExpression.Left.StripCasts().StripMarkers();
      Expression right = binaryExpression.Right.StripCasts().StripMarkers();

      var rightIsConstant = context.Evaluator.CanBeEvaluated(right);
      var leftIsConstant = context.Evaluator.CanBeEvaluated(left);
      bool leftOrRightIsIndex = false;

      if (left is IndexExpression leftIndexExpression) {
        left = VisitIndex(leftIndexExpression);
        leftOrRightIsIndex = true;
      }
      if (right is IndexExpression rightIndexExpression) {
        right = VisitIndex(rightIndexExpression);
        leftOrRightIsIndex = true;
      }

      IList<Expression> leftExpressions;
      IList<Expression> rightExpressions;

      // Split left and right arguments to subexpressions.
      MemberType memberType = left.Type == WellKnownTypes.Object
        ? right.GetMemberType()
        : left.GetMemberType();
      switch (memberType) {
        case MemberType.EntitySet:
          if ((leftIsConstant && ExpressionEvaluator.Evaluate(left).Value == null)
            || left is ConstantExpression && ((ConstantExpression) left).Value == null)
            return SelectBoolConstantExpression(binaryExpression.NodeType == ExpressionType.NotEqual);
          if ((rightIsConstant && ExpressionEvaluator.Evaluate(right).Value == null)
            || right is ConstantExpression && ((ConstantExpression) right).Value == null)
            return SelectBoolConstantExpression(binaryExpression.NodeType == ExpressionType.NotEqual);
          var leftEntitySetExpression = left as EntitySetExpression;
          var rightEntitySetExpression = right as EntitySetExpression;
          if (leftEntitySetExpression != null && rightEntitySetExpression != null) {
            if (leftEntitySetExpression.Field != rightEntitySetExpression.Field) {
              return FalseExpression;
            }
            var binary = Expression.MakeBinary(binaryExpression.NodeType,
              (Expression) leftEntitySetExpression.Owner,
              (Expression) rightEntitySetExpression.Owner,
              binaryExpression.IsLiftedToNull,
              binaryExpression.Method);
            return VisitBinaryRecursive(binary, originalBinaryExpression);
          }
          if (rightEntitySetExpression != null && left is ConstantExpression) {
            var leftEntitySet = (EntitySetBase) ((ConstantExpression) left).Value;
            var binary = Expression.MakeBinary(binaryExpression.NodeType,
              Expression.Constant(leftEntitySet.Owner),
              (Expression) rightEntitySetExpression.Owner,
              binaryExpression.IsLiftedToNull,
              binaryExpression.Method);
            return VisitBinaryRecursive(binary, originalBinaryExpression);
          }
          if (leftEntitySetExpression != null && right is ConstantExpression) {
            var rightEntitySet = (EntitySetBase) ((ConstantExpression) right).Value;
            var binary = Expression.MakeBinary(binaryExpression.NodeType,
              (Expression) leftEntitySetExpression.Owner,
              Expression.Constant(rightEntitySet.Owner),
              binaryExpression.IsLiftedToNull,
              binaryExpression.Method);
            return VisitBinaryRecursive(binary, originalBinaryExpression);
          }
          return binaryExpression;
        case MemberType.Key:
          var leftKeyExpression = left as KeyExpression;
          var rightKeyExpression = right as KeyExpression;
          if (leftKeyExpression == null && rightKeyExpression == null)
            throw new InvalidOperationException(String.Format(Strings.ExBothLeftAndRightPartOfBinaryExpressionXAreNULLOrNotKeyExpression, originalBinaryExpression.ToString(true)));
          // Check key compatibility
          leftKeyExpression.EnsureKeyExpressionCompatible(rightKeyExpression, originalBinaryExpression);
          // Key split to it's fields.
          IEnumerable<Type> keyFields = (leftKeyExpression ?? rightKeyExpression)
            .KeyFields
            .Select(fieldExpression => fieldExpression.Type);
          leftExpressions = GetKeyFields(left, keyFields);
          rightExpressions = GetKeyFields(right, keyFields);
          break;
        case MemberType.Entity:
          // Entity split to key fields.
          var leftEntityExpression = (Expression) (left as EntityExpression) ?? left as EntityFieldExpression;
          var rightEntityExpression = (Expression) (right as EntityExpression) ?? right as EntityFieldExpression;
          if (leftEntityExpression == null && rightEntityExpression == null)
            if (!IsConditionalOrWellknown(left) && !IsConditionalOrWellknown(right))
              throw new NotSupportedException(
                String.Format(
                  Strings.ExBothLeftAndRightPartOfBinaryExpressionXAreNULLOrNotEntityExpressionEntityFieldExpression,
                  binaryExpression));
          var type = left.Type == WellKnownTypes.Object
             ? right.Type
             : left.Type;

          var keyFieldTypes = context
            .Model
            .Types[type]
            .Key
            .TupleDescriptor;

          leftExpressions = GetEntityFields(left, keyFieldTypes);
          rightExpressions = GetEntityFields(right, keyFieldTypes);
          break;
        case MemberType.Anonymous:
          // Anonymous type split to constructor arguments.
          var anonymousType = (left.Type == WellKnownTypes.Object)
            ? right.Type
            : left.Type;
          leftExpressions = GetAnonymousArguments(left, anonymousType);
          rightExpressions = GetAnonymousArguments(right, anonymousType);
          break;
        case MemberType.Structure:
          if ((leftIsConstant && ExpressionEvaluator.Evaluate(left).Value == null)
            || left is ConstantExpression && ((ConstantExpression) left).Value == null)
            return SelectBoolConstantExpression(binaryExpression.NodeType == ExpressionType.NotEqual);
          if ((rightIsConstant && ExpressionEvaluator.Evaluate(right).Value == null)
            || right is ConstantExpression && ((ConstantExpression) right).Value == null)
            return SelectBoolConstantExpression(binaryExpression.NodeType == ExpressionType.NotEqual);
          // Structure split to it's fields.
          var leftStructureExpression = left as StructureFieldExpression;
          var rightStructureExpression = right as StructureFieldExpression;
          if (leftStructureExpression == null && rightStructureExpression == null)
            throw new NotSupportedException(String.Format(Strings.ExBothLeftAndRightPartOfBinaryExpressionXAreNULLOrNotStructureExpression, binaryExpression));

          StructureFieldExpression structureFieldExpression = (leftStructureExpression ?? rightStructureExpression);
          leftExpressions = GetStructureFields(left, structureFieldExpression.Fields, structureFieldExpression.Type);
          rightExpressions = GetStructureFields(right, structureFieldExpression.Fields, structureFieldExpression.Type);
          break;
        case MemberType.Array:
          // Special case. ArrayIndex expression.
          if (binaryExpression.NodeType == ExpressionType.ArrayIndex) {
            var arrayExpression = Visit(left);
            var arrayIndex = Visit(right);
            return Expression.ArrayIndex(arrayExpression, arrayIndex);
          }

          // If array compares to null use standard routine.
          if ((rightIsConstant && ExpressionEvaluator.Evaluate(right).Value == null)
            || (rightIsConstant && ExpressionEvaluator.Evaluate(right).Value == null)
              || (right.Type == WellKnownTypes.ByteArray && (left is FieldExpression || left is ColumnExpression || right is FieldExpression || right is ColumnExpression)))
            return Expression.MakeBinary(binaryExpression.NodeType,
              left,
              right,
              binaryExpression.IsLiftedToNull,
              binaryExpression.Method);


          // Array split to it's members.
          leftExpressions = ((NewArrayExpression) left).Expressions;
          rightExpressions = ((NewArrayExpression) right).Expressions;
          break;
        default:
          // Primitive types don't has subexpressions. Use standart routine.
          if (leftOrRightIsIndex) {
            var binary = Expression.MakeBinary(binaryExpression.NodeType, left, right, binaryExpression.IsLiftedToNull, binaryExpression.Method);
            return VisitBinaryRecursive(binary, binaryExpression);
          }
          return binaryExpression;
      }

      if (leftExpressions.Count != rightExpressions.Count || leftExpressions.Count == 0)
        throw Exceptions.InternalError(Strings.ExMistmatchCountOfLeftAndRightExpressions, OrmLog.Instance);

      // Combine new binary expression from subexpression pairs.
      Expression resultExpression = null;
      for (var i = 0; i < leftExpressions.Count; i++) {
        BinaryExpression pairExpression;
        var leftItem = leftExpressions[i];
        var rightItem = rightExpressions[i];
        var leftIsNullable = leftItem.Type.IsNullable();
        var rightIsNullable = rightItem.Type.IsNullable();
        leftItem = leftIsNullable
          ? leftItem
          : leftItem.LiftToNullable();
        rightItem = rightIsNullable
          ? rightItem
          : rightItem.LiftToNullable();

        switch (binaryExpression.NodeType) {
          case ExpressionType.Equal:
            pairExpression = Expression.Equal(leftItem, rightItem);
            break;
          case ExpressionType.NotEqual:
            pairExpression = Expression.NotEqual(leftItem, rightItem);
            break;
          default:
            throw new NotSupportedException(String.Format(Strings.ExBinaryExpressionsWithNodeTypeXAreNotSupported,
              binaryExpression.NodeType));
        }

        // visit new expression recursively
        var visitedResultExpression = VisitBinaryRecursive(pairExpression, originalBinaryExpression);

        // Combine expression chain with AndAlso
        resultExpression = resultExpression == null
          ? visitedResultExpression
          : Expression.AndAlso(resultExpression, visitedResultExpression);
      }

      // Return result.
      return resultExpression;
    }

    private static ConstantExpression SelectBoolConstantExpression(bool b) =>
      b ? TrueExpression : FalseExpression;

    protected override Expression VisitIndex(IndexExpression ie)
    {
      var objectExpression = Visit(ie.Object).StripCasts();
      var argument = Visit(ie.Arguments[0]);
      var evaluatedArgument = (string) ExpressionEvaluator.Evaluate(argument).Value;
      switch (objectExpression) {
        case EntityExpression entityExpression:
          return entityExpression.Fields.First(field => field.Name == evaluatedArgument);
        case StructureExpression structureExpression:
          return structureExpression.Fields.First(field => field.Name == evaluatedArgument);
        case EntityFieldExpression entityFieldExpression:
          return entityFieldExpression.Fields.First(field => field.Name == evaluatedArgument);
        case StructureFieldExpression structureFieldExpression:
          return structureFieldExpression.Fields.First(field => field.Name == evaluatedArgument);
      }

      var typeInfo = context.Model.Types[objectExpression.Type];
      if (objectExpression is ParameterExpression || objectExpression is ConstantExpression) {
        if (typeInfo.IsEntity) {
          var entityExpression = EntityExpression.Create(typeInfo, 0, false);
          return entityExpression.Fields.First(field => field.Name == evaluatedArgument);
        }
        if (typeInfo.IsStructure) {
          var structureExpression = StructureExpression.CreateLocalCollectionStructure(typeInfo, new Segment<ColNum>(0, typeInfo.TupleDescriptor.Count));
          return structureExpression.Fields.First(field => field.Name == evaluatedArgument);
        }
      }
      var fieldInfo = typeInfo.Fields[evaluatedArgument];
      return Expression.Convert(Expression.Call(objectExpression, objectExpression.Type.GetProperty("Item").GetGetMethod(), new[] { Expression.Constant(evaluatedArgument) }), fieldInfo.ValueType);
    }

    private static bool IsConditionalOrWellknown(Expression expression, bool isRoot = true)
    {
      var conditionalExpression = expression as ConditionalExpression;
      if (conditionalExpression != null)
        return IsConditionalOrWellknown(conditionalExpression.IfTrue, false)
          && IsConditionalOrWellknown(conditionalExpression.IfFalse, false);

      if (isRoot)
        return false;

      if (expression.NodeType == ExpressionType.Constant)
        return true;

      if (expression.NodeType == ExpressionType.Convert) {
        var unary = (UnaryExpression) expression;
        return IsConditionalOrWellknown(unary.Operand, false);
      }

      if (!(expression is ExtendedExpression))
        return false;

      var memberType = expression.GetMemberType();
      switch (memberType) {
        case MemberType.Primitive:
        case MemberType.Key:
        case MemberType.Structure:
        case MemberType.Entity:
        case MemberType.EntitySet:
          return true;
        default:
          return false;
      }
    }

    private IList<Expression> GetStructureFields(
      Expression expression,
      IEnumerable<PersistentFieldExpression> structureFields,
      Type structureType)
    {
      expression = expression.StripCasts();
      if (expression is IPersistentExpression persistentExpression) {
        return persistentExpression
          .Fields
          .Where(field => field.GetMemberType() == MemberType.Primitive)
          .Select(e => (Expression) e)
          .ToList();
      }

      ConstantExpression nullExpression = Expression.Constant(null, structureType);
      BinaryExpression isNullExpression = Expression.Equal(expression, nullExpression);

      var result = new List<Expression>();
      foreach (PersistentFieldExpression fieldExpression in structureFields) {
        if (!structureType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Contains(fieldExpression.UnderlyingProperty)) {
          if (!context.Model.Types[structureType].Fields[fieldExpression.Name].IsDynamicallyDefined) {
            continue;
          }
        }
        Type nullableType = fieldExpression.Type.ToNullable();
        Expression propertyAccessorExpression;
        if (fieldExpression.UnderlyingProperty != null) {
          propertyAccessorExpression = Expression.MakeMemberAccess(Expression.Convert(expression, structureType), fieldExpression.UnderlyingProperty);
        }
        else {
          var attributes = structureType.GetCustomAttributes(WellKnownTypes.DefaultMemberAttribute, true);
          var indexerPropertyName = ((DefaultMemberAttribute) attributes.Single()).MemberName;
          var methodInfo = structureType.GetProperty(indexerPropertyName).GetGetMethod();
          propertyAccessorExpression = Expression.Call(Expression.Convert(expression, structureType), methodInfo, Expression.Constant(fieldExpression.Name));
        }
        Expression memberExpression = Expression.Condition(
          isNullExpression,
          Expression.Constant(null, nullableType),
          Expression.Convert(
            propertyAccessorExpression,
            nullableType));

        switch (fieldExpression.GetMemberType()) {
          case MemberType.Entity:
            IEnumerable<Type> keyFieldTypes = context
              .Model
              .Types[fieldExpression.Type]
              .Key
              .TupleDescriptor;
            result.AddRange(GetEntityFields(memberExpression, keyFieldTypes));
            break;
          case MemberType.Structure:
            var structureFieldExpression = (StructureFieldExpression) fieldExpression;
            result.AddRange(GetStructureFields(memberExpression, structureFieldExpression.Fields, structureFieldExpression.Type));
            break;
          case MemberType.Primitive:
            result.Add(memberExpression);
            break;
          default:
            throw new NotSupportedException();
        }
      }
      return result;
    }

    private static IList<Expression> GetEntityFields(Expression expression, IEnumerable<Type> keyFieldTypes)
    {
      expression = expression.StripCasts();
      if (expression is IEntityExpression entityExpression) {
        return GetKeyFields(entityExpression.Key, null);
      }


      Expression keyExpression;

      if (expression.IsNull()) {
        keyExpression = NullKeyExpression;
      }
      else if (IsConditionalOrWellknown(expression)) {
        return keyFieldTypes
          .Select((type, index) => GetConditionalKeyField(expression, type, index))
          .ToList();
      }
      else {
        ConstantExpression nullEntityExpression = Expression.Constant(null, expression.Type);
        BinaryExpression isNullExpression = Expression.Equal(expression, nullEntityExpression);
        if (!WellKnownOrmInterfaces.Entity.IsAssignableFrom(expression.Type))
          expression = Expression.Convert(expression, WellKnownOrmInterfaces.Entity);
        keyExpression = Expression.Condition(
          isNullExpression,
          NullKeyExpression,
          Expression.MakeMemberAccess(expression, WellKnownMembers.IEntityKey));
      }
      return GetKeyFields(keyExpression, keyFieldTypes);
    }

    private static Expression GetConditionalKeyField(Expression expression, Type keyFieldType, int index)
    {
      var ce = expression as ConditionalExpression;
      if (ce != null)
        return Expression.Condition(
          ce.Test,
          GetConditionalKeyField(ce.IfTrue, keyFieldType, index),
          GetConditionalKeyField(ce.IfFalse, keyFieldType, index));
      if (expression.IsNull())
        return Expression.Constant(null, keyFieldType.ToNullable());
      var ee = (IEntityExpression) expression.StripCasts();
      return ee.Key.KeyFields[index].LiftToNullable();
    }

    private static IList<Expression> GetKeyFields(Expression expression, IEnumerable<Type> keyFieldTypes)
    {
      expression = expression.StripCasts();

      var keyExpression = expression as KeyExpression;
      if (keyExpression != null)
        return keyExpression
          .KeyFields
          .Select(fieldExpression => (Expression) fieldExpression)
          .ToList();

      if (expression.IsNull())
        return keyFieldTypes
          .Select(type => (Expression) Expression.Constant(null, type.ToNullable()))
          .ToList();

      var nullExpression = Expression.Constant(null, expression.Type);
      var isNullExpression = Expression.Equal(expression, nullExpression);
      var keyTupleExpression = Expression.MakeMemberAccess(expression, WellKnownMembers.Key.Value);

      return keyFieldTypes
        .Select((type, index) => {
          var resultType = type.ToNullable();
          var baseType = type.StripNullable();
          var fieldType = (baseType.IsEnum ? Enum.GetUnderlyingType(baseType) : baseType).ToNullable();
          Expression tupleAccess = keyTupleExpression.MakeTupleAccess(fieldType, index);
          if (fieldType != resultType)
            tupleAccess = Expression.Convert(tupleAccess, resultType);
          return (Expression) Expression.Condition(isNullExpression, Expression.Constant(null, resultType), tupleAccess);
        })
        .ToList();
    }

    private Expression ProcessProjectionElement(Expression body)
    {
      var originalBodyType = body.Type;
      var reduceCastBody = body.StripCasts();

      var canCalculate =
        State.CalculateExpressions
          && reduceCastBody.GetMemberType() == MemberType.Unknown
          && (reduceCastBody.NodeType != ExpressionType.New || reduceCastBody.IsNewExpressionSupportedByStorage())
          && reduceCastBody.NodeType != ExpressionType.ArrayIndex
          && (ExtendedExpressionType) reduceCastBody.NodeType != ExtendedExpressionType.Constructor
          && !ContainsEntityExpression(reduceCastBody);

      if (!canCalculate)
        return body;

      var lambdaParameter = State.Parameters[0];
      var calculator = ExpressionMaterializer.MakeLambda(body, context);
      var columnDescriptor = CreateCalculatedColumnDescriptor(calculator);
      return AddCalculatedColumn(lambdaParameter, columnDescriptor, originalBodyType);
    }

    private CalculatedColumnDescriptor CreateCalculatedColumnDescriptor(LambdaExpression expression)
    {
      var columnType = expression.Body.Type;
      var body = EnumRewriter.Rewrite(expression.Body);
      if (columnType != WellKnownTypes.Object)
        body = Expression.Convert(body, WellKnownTypes.Object);
      var calculator = (Expression<Func<Tuple, object>>) FastExpression.Lambda(body, expression.Parameters);
      return new CalculatedColumnDescriptor(context.GetNextColumnAlias(), columnType, calculator);
    }

    private ColumnExpression AddCalculatedColumn(ParameterExpression sourceParameter, CalculatedColumnDescriptor descriptor, Type originalColumnType)
    {
      var oldResult = context.Bindings[sourceParameter];
      var isInlined = !State.BuildingProjection && !State.GroupingKey;
      var dataSource = oldResult.ItemProjector.DataSource;

      SortProvider sortProvider = null;
      if (dataSource is SortProvider) {
        sortProvider = (SortProvider) dataSource;
        dataSource = sortProvider.Source;
      }

      var columns = new List<CalculatedColumnDescriptor>();
      if (State.AllowCalculableColumnCombine && dataSource is CalculateProvider && isInlined == ((CalculateProvider) dataSource).IsInlined) {
        var calculateProvider = ((CalculateProvider) dataSource);
        var presentColumns = calculateProvider
          .CalculatedColumns
          .Select(cc => new CalculatedColumnDescriptor(cc.Name, cc.Type, cc.Expression));
        columns.AddRange(presentColumns);
        dataSource = calculateProvider.Source;
      }
      columns.Add(descriptor);
      dataSource = dataSource.Calculate(isInlined, columns);

      if (sortProvider != null)
        dataSource = dataSource.OrderBy(sortProvider.Order);

      var newItemProjector = oldResult.ItemProjector.Remap(dataSource, 0);
      var newResult = new ProjectionExpression(oldResult.Type, newItemProjector, oldResult.TupleParameterBindings);
      context.Bindings.ReplaceBound(sourceParameter, newResult);

      var result = ColumnExpression.Create(originalColumnType, (ColNum) (dataSource.Header.Length - 1));
      ModifyStateAllowCalculableColumnCombine(true);

      return result;
    }

    private static bool ContainsEntityExpression(Expression expression)
    {
      var found = false;
      var entityFinder = new ExtendedExpressionReplacer(e => {
        if ((int) e.NodeType == (int) ExtendedExpressionType.Entity) {
          found = true;
          return e;
        }
        return null;
      });
      entityFinder.Replace(expression);
      return found;
    }

    private Expression ConstructQueryable(MethodCallExpression mc)
    {
      var elementType = mc.Method.GetGenericArguments()[0];
      TypeInfo type;
      if (!context.Model.Types.TryGetValue(elementType, out type))
        throw new InvalidOperationException(String.Format(Strings.ExTypeNotFoundInModel, elementType.FullName));
      var index = type.Indexes.PrimaryIndex;
      var entityExpression = EntityExpression.Create(type, 0, false);
      var itemProjector = new ItemProjectorExpression(entityExpression, index.GetQuery(), context);
      return new ProjectionExpression(WellKnownInterfaces.QueryableOfT.CachedMakeGenericType(elementType), itemProjector, EmptyTupleParameterBindings);
    }

    private Expression BuildSubqueryResult(ProjectionExpression subQuery, Type resultType)
    {
      if (State.Parameters.Count == 0)
        throw Exceptions.InternalError(String.Format(Strings.ExUnableToBuildSubqueryResultForExpressionXStateContainsNoParameters, subQuery), OrmLog.Instance);

      if (!resultType.IsOfGenericInterface(WellKnownInterfaces.EnumerableOfT))
        throw Exceptions.InternalError(String.Format(Strings.ExUnableToBuildSubqueryResultForExpressionXResultTypeIsNotIEnumerable, subQuery), OrmLog.Instance);

      ApplyParameter applyParameter = context.GetApplyParameter(context.Bindings[State.Parameters[0]]);
      if (subQuery.Type != resultType)
        subQuery = new ProjectionExpression(
          resultType,
          subQuery.ItemProjector,
          subQuery.TupleParameterBindings,
          subQuery.ResultAccessMethod);
      return new SubQueryExpression(resultType, State.Parameters[0], false, subQuery, applyParameter);
    }

    private static IList<Expression> GetAnonymousArguments(Expression expression, Type anonymousTypeForNullValues = null)
    {
      if (expression.NodeType == ExpressionType.New) {
        var newExpression = ((NewExpression) expression);
        IEnumerable<Expression> arguments = newExpression
          .Members
          .Select((methodInfo, index) => (methodInfo.Name, Argument: newExpression.Arguments[index]))
          .OrderBy(a => a.Name)
          .Select(a => a.Argument);
        return arguments.ToList();
      }

      if (expression.NodeType == ExpressionType.Constant) {
        var constantExpression = expression as ConstantExpression;
        if (constantExpression.Value == null && constantExpression.Type == WellKnownTypes.Object) {
          var newConstantExpressionType = anonymousTypeForNullValues ?? constantExpression.Type;
          constantExpression = Expression.Constant(null, newConstantExpressionType);
          return constantExpression.Type.GetProperties()
            .OrderBy(property => property.Name)
            .Select(p => Expression.MakeMemberAccess(constantExpression, p))
            .Cast<Expression>()
            .ToList();
        }
      }

      return expression.Type.GetProperties()
        .OrderBy(property => property.Name)
        .Select(p => Expression.MakeMemberAccess(expression, p))
        .Select(e => (Expression) e)
        .ToList();
    }

    protected override Expression VisitMemberInit(MemberInitExpression mi)
    {
      var newExpression = mi.NewExpression;
      var _ = VisitNewExpressionArguments(newExpression);
      var bindings = VisitBindingList(mi.Bindings).Cast<MemberAssignment>();
      var miNewExpression = mi.NewExpression;
      var constructorExpression = (ConstructorExpression) VisitNew(miNewExpression);
      foreach (var binding in bindings) {
        var member = binding.Member;
        if (member.MemberType == MemberTypes.Property) {
          member = TryGetActualPropertyInfo((PropertyInfo) member, miNewExpression.Type);
        }
        constructorExpression.Bindings[member] = binding.Expression;
        constructorExpression.NativeBindings[member] = binding.Expression;
      }
      return constructorExpression;
    }

    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    private Expression GetMember(Expression expression, MemberInfo member, Expression sourceExpression)
    {
      if (expression == null) {
        return null;
      }

      expression = expression.StripCasts();
      var isMarker = expression.TryGetMarker(out var markerType);
      expression = expression.StripMarkers();
      expression = expression.StripCasts();

      if (expression.IsAnonymousConstructor()) {
        var newExpression = (NewExpression) expression;
        var memberIndex = newExpression.Members.IndexOf(member);
        if (memberIndex < 0)
          throw new InvalidOperationException(string.Format(Strings.ExCouldNotGetMemberXFromExpression, member));
        var argument = Visit(newExpression.Arguments[memberIndex]);
        return isMarker ? new MarkerExpression(argument, markerType) : argument;
      }

      var extendedExpression = expression as ExtendedExpression;
      if (extendedExpression == null) {
        return IsConditionalOrWellknown(expression)
          ? GetConditionalMember(expression, member, sourceExpression)
          : null;
      }

      Expression result = null;

      string builtFieldName = null;
      bool propertyFilter(PersistentFieldExpression f) =>
        f.Name == (builtFieldName ??= context.Domain.Handlers.NameBuilder.BuildFieldName((PropertyInfo) member));

      switch (extendedExpression.ExtendedType) {
        case ExtendedExpressionType.FullText:
          switch (member.Name) {
            case "Rank":
              return ((FullTextExpression) expression).RankExpression;
            case "Entity":
              return ((FullTextExpression) expression).EntityExpression;
          }
          break;
        case ExtendedExpressionType.Grouping:
          if (member.Name == "Key") {
            return ((GroupingExpression) expression).KeyExpression;
          }
          break;
        case ExtendedExpressionType.Constructor:
          var nativeExpression = ((ConstructorExpression) extendedExpression);
          var bindings = nativeExpression.Bindings;
          // only make sure that type has needed member
          if (!bindings.TryGetValue(member, out result)) {
            // Key in bindings might be a property/field reflected from a base type
            // but our member might be reflected from child type.
            var baseType = member.DeclaringType;
            if (baseType.IsInterface) {
              var implementor = member.GetImplementation(nativeExpression.Type);
              if (implementor == null) {
                throw new InvalidOperationException(string.Format(Strings.ExThereIsNoImplemetationOfXYMemberInZType,
                  member.DeclaringType.Name, member.Name, nativeExpression.Type.ToString()));
              }
              _ = bindings.TryGetValue(implementor, out result);
            }
            else {
              var baseMember = baseType.GetMember(member.Name).FirstOrDefault();
              if (baseMember == null) {
                throw new InvalidOperationException(string.Format(
                  Strings.ExMemberXOfTypeYIsNotInitializedCheckIfConstructorArgumentIsCorrectOrFieldInitializedThroughInitializer,
                  member.Name, member.ReflectedType.Name));
              }
            }
          }
          result = Visit(result);
          break;
        case ExtendedExpressionType.Structure:
        case ExtendedExpressionType.StructureField:
          var persistentExpression = (IPersistentExpression) expression;
          result = persistentExpression.Fields.First(propertyFilter);
          break;
        case ExtendedExpressionType.LocalCollection:
          var localCollectionExpression = (LocalCollectionExpression) expression;
          result = (Expression) localCollectionExpression.Fields[member];
          break;
        case ExtendedExpressionType.Entity:
          var entityExpression = (EntityExpression) expression;
          result = entityExpression.Fields.FirstOrDefault(propertyFilter);
          if (result == null) {
            EnsureEntityFieldsAreJoined(entityExpression);
            result = entityExpression.Fields.First(propertyFilter);
          }
          break;
        case ExtendedExpressionType.Field:
          if (isMarker && ((markerType & MarkerType.Single) == MarkerType.Single)) {
            throw new InvalidOperationException(string.Format(Strings.ExUseMethodXOnFirstInsteadOfSingle, sourceExpression.ToString(true), member.Name));
          }
          if (member.DeclaringType.IsNullable()) {
            expression = Expression.Convert(expression, member.DeclaringType);
          }
          return Expression.MakeMemberAccess(expression, member);
        case ExtendedExpressionType.EntityField:
          var entityFieldExpression = (EntityFieldExpression) expression;
          result = entityFieldExpression.Fields.FirstOrDefault(propertyFilter);
          if (result == null) {
            EnsureEntityReferenceIsJoined(entityFieldExpression);
            result = entityFieldExpression.Entity.Fields.First(propertyFilter);
          }
          break;
      }

      return isMarker
        ? new MarkerExpression(result, markerType)
        : result;
    }

    private Expression GetMemberWithRemap(Expression expression, MemberInfo memberInfo, Expression sourceExpression)
    {
      var original = GetMember(expression, memberInfo, sourceExpression);
      if (original.IsSubqueryExpression()) {
        var subquery = (SubQueryExpression) original;
        var projectionExpression = subquery.ProjectionExpression;
        var itemProjector = projectionExpression.ItemProjector;
        using var columnIndexes = new ColumnMap(itemProjector.GetColumns(ColumnExtractionModes.KeepSegment));

        var expReplacer = new ExtendedExpressionReplacer((e) => {
          if (e is GroupingExpression ge && ge.SelectManyInfo.GroupByProjection != null) {
            var geProjectionExpression = ge.ProjectionExpression;
            var geItemProjector = geProjectionExpression.ItemProjector;
            using var columnIndexes = new ColumnMap(geItemProjector.GetColumns(ColumnExtractionModes.KeepSegment));

            var newProjectionExpression = new ProjectionExpression(geProjectionExpression.Type,
              geItemProjector.Remap(geItemProjector.DataSource, columnIndexes),
              geProjectionExpression.TupleParameterBindings);

            var groupByProjection = ge.SelectManyInfo.GroupByProjection;
            var groupByProjector = groupByProjection.ItemProjector;
            using var groupByColumnIndexes = new ColumnMap(groupByProjector.GetColumns(ColumnExtractionModes.KeepSegment));

            var newGroupByProjection = new ProjectionExpression(groupByProjection.Type,
              groupByProjector.Remap(groupByProjector.DataSource, groupByColumnIndexes),
              groupByProjection.TupleParameterBindings);

            var result = new GroupingExpression(ge.Type,
              ge.OuterParameter, ge.DefaultIfEmpty, newProjectionExpression, ge.ApplyParameter, ge.KeyExpression,
              new GroupingExpression.SelectManyGroupingInfo(newGroupByProjection));
            return result;
          }
          else
            return null;
        });

        var newItem = expReplacer.Replace(itemProjector.Item);
        var staysTheSame = newItem == itemProjector.Item;
        var newItemProjector = staysTheSame
          ? itemProjector.Remap(itemProjector.DataSource, columnIndexes)
          : new ItemProjectorExpression(newItem, itemProjector.DataSource, itemProjector.Context);

        var result = new SubQueryExpression(subquery.Type,
          subquery.OuterParameter,
          subquery.DefaultIfEmpty,
          new ProjectionExpression(projectionExpression.Type, newItemProjector, projectionExpression.TupleParameterBindings),
          subquery.ApplyParameter,
          subquery.ExtendedType);
        return result;
      }
      return original;
    }

    private Expression GetConditionalMember(Expression expression, MemberInfo member, Expression sourceExpression)
    {
      var ce = expression as ConditionalExpression;
      if (ce != null) {
        var ifTrue = GetConditionalMember(ce.IfTrue, member, sourceExpression);
        var ifFalse = GetConditionalMember(ce.IfFalse, member, sourceExpression);
        if (ifTrue == null || ifFalse == null)
          return null;
        return Expression.Condition(ce.Test, ifTrue, ifFalse);
      }
      if (expression.IsNull()) {
        var mt = member.MemberType;
        Type valueType;
        switch (mt) {
          case MemberTypes.Field:
            var fi = (FieldInfo) member;
            valueType = fi.FieldType;
            break;
          case MemberTypes.Property:
            var pi = (PropertyInfo) member;
            valueType = pi.PropertyType;
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
        return Expression.Constant(null, valueType.ToNullable());
      }
      return GetMember(expression, member, sourceExpression);
    }

    /// <exception cref="NotSupportedException"><c>NotSupportedException</c>.</exception>
    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    private Expression VisitTypeAs(Expression source, Type targetType)
    {
      if (source.GetMemberType() != MemberType.Entity)
        throw new NotSupportedException(Strings.ExAsOperatorSupportsEntityOnly);

      // Expression is already of requested type.
      var visitedSource = Visit(source);
      if (source.Type == targetType)
        return visitedSource;

      // Call convert to parent type.
      if (targetType.IsAssignableFrom(source.Type))
        return Visit(Expression.Convert(source, targetType));

      // Cast to subclass or interface.
      var targetTypeInfo = context.Model.Types[targetType];
      // Using of state.Parameter[0] is a very weak approach.
      // `as` operator could be applied on expression that has no relation with current parameter
      // thus the later code will fail.
      // We can't easily find real parameter that need replacement.
      // We work around this situation by supporting some known cases.
      // The simplest (and the only at moment) case is a source being chain of MemberExpressions.
      var currentParameter = State.Parameters[0];
      var parameter = (source.StripMemberAccessChain() as ParameterExpression) ?? currentParameter;
      var entityExpression = visitedSource.StripCasts().StripMarkers() as IEntityExpression;

      if (entityExpression == null)
        throw new InvalidOperationException(Strings.ExAsOperatorSupportsEntityOnly);

      // Replace original recordset. New recordset is left join with old recordset
      ProjectionExpression originalResultExpression = context.Bindings[parameter];
      var originalQuery = originalResultExpression.ItemProjector.DataSource;
      ColNum offset = originalQuery.Header.Columns.Count;

      // Join primary index of target type
      IndexInfo indexToJoin = targetTypeInfo.Indexes.PrimaryIndex;
      var queryToJoin = indexToJoin.GetQuery().Alias(context.GetNextAlias());
      var keySegment = entityExpression.Key.Mapping.GetItems();
      var keyPairs = keySegment
        .Select((leftIndex, rightIndex) => (leftIndex, (ColNum)rightIndex))
        .ToArray();

      // Replace recordset.
      var joinedRecordQuery = originalQuery.LeftJoin(queryToJoin, keyPairs);
      var itemProjectorExpression = new ItemProjectorExpression(
        originalResultExpression.ItemProjector.Item, joinedRecordQuery, context);
      var projectionExpression = new ProjectionExpression(
        originalResultExpression.Type, itemProjectorExpression, originalResultExpression.TupleParameterBindings);
      context.Bindings.ReplaceBound(parameter, projectionExpression);

      // return new EntityExpression
      var result = EntityExpression.Create(context.Model.Types[targetType], offset, false);
      result.IsNullable = true;
      if (parameter != currentParameter)
        result = (EntityExpression) result.BindParameter(parameter, new Dictionary<Expression, Expression>());
      return result;
    }

    private void EnsureEntityFieldsAreJoined(EntityExpression entityExpression)
    {
      ItemProjectorExpression itemProjector = entityExpression.OuterParameter == null
        ? context.Bindings[State.Parameters[0]].ItemProjector
        : context.Bindings[entityExpression.OuterParameter].ItemProjector;
      EnsureEntityFieldsAreJoined(entityExpression, itemProjector);
    }

    public void EnsureEntityFieldsAreJoined(EntityExpression entityExpression, ItemProjectorExpression itemProjector)
    {
      TypeInfo typeInfo = entityExpression.PersistentType;
      if (
        typeInfo.Fields.All(fieldInfo => entityExpression.Fields.Any(entityField => entityField.Name == fieldInfo.Name)))
        return; // All fields are already joined
      IndexInfo joinedIndex = typeInfo.Indexes.PrimaryIndex;
      var joinedRs = joinedIndex.GetQuery().Alias(itemProjector.Context.GetNextAlias());
      Segment<ColNum> keySegment = entityExpression.Key.Mapping;
      var keyPairs = keySegment.GetItems()
        .Select((leftIndex, rightIndex) => (leftIndex, (ColNum)rightIndex))
        .ToArray();
      ColNum offset = itemProjector.DataSource.Header.Length;
      var oldDataSource = itemProjector.DataSource;
      var newDataSource = entityExpression.IsNullable
        ? itemProjector.DataSource.LeftJoin(joinedRs, keyPairs)
        : itemProjector.DataSource.Join(joinedRs, keyPairs);
      itemProjector.DataSource = newDataSource;
      EntityExpression.Fill(entityExpression, offset);
      context.RebindApplyParameter(oldDataSource, newDataSource);
    }

    private void EnsureEntityReferenceIsJoined(EntityFieldExpression entityFieldExpression)
    {
      if (entityFieldExpression.Entity != null)
        return;
      TypeInfo typeInfo = entityFieldExpression.PersistentType;
      IndexInfo joinedIndex = typeInfo.Indexes.PrimaryIndex;
      var joinedRs = joinedIndex.GetQuery().Alias(context.GetNextAlias());
      Segment<ColNum> keySegment = entityFieldExpression.Mapping;
      (ColNum Left, ColNum Right)[] keyPairs = keySegment.GetItems()
        .Select((leftIndex, rightIndex) => (leftIndex, (ColNum)rightIndex))
        .ToArray();
      ItemProjectorExpression originalItemProjector = entityFieldExpression.OuterParameter == null
        ? context.Bindings[State.Parameters[0]].ItemProjector
        : context.Bindings[entityFieldExpression.OuterParameter].ItemProjector;
      ColNum offset = originalItemProjector.DataSource.Header.Length;
      var oldDataSource = originalItemProjector.DataSource;
      bool shouldUseLeftJoin = false;
      var filterProvider = oldDataSource as FilterProvider;
      if (filterProvider != null) {
        var applyProvider = filterProvider.Source as ApplyProvider;
        if (applyProvider != null)
          shouldUseLeftJoin = applyProvider.ApplyType == JoinType.LeftOuter;
        else {
          var joinProvider = filterProvider.Source as JoinProvider;
          if (joinProvider != null)
            shouldUseLeftJoin = joinProvider.JoinType == JoinType.LeftOuter;
        }
      }
      else {
        var joinProvider = oldDataSource as JoinProvider;
        if (joinProvider != null)
          shouldUseLeftJoin = joinProvider.JoinType == JoinType.LeftOuter;
      }
      var newDataSource = entityFieldExpression.IsNullable || shouldUseLeftJoin
        ? originalItemProjector.DataSource.LeftJoin(joinedRs, keyPairs)
        : originalItemProjector.DataSource.Join(joinedRs, keyPairs);
      originalItemProjector.DataSource = newDataSource;
      entityFieldExpression.RegisterEntityExpression(offset);
      context.RebindApplyParameter(oldDataSource, newDataSource);
    }

    private static Expression MakeBooleanExpression(Expression previous, Expression left, Expression right,
      ExpressionType operationType, ExpressionType concatenationExpression)
    {
      var binaryExpression = operationType switch {
        ExpressionType.Equal => Expression.Equal(left, right),
        ExpressionType.NotEqual => Expression.NotEqual(left, right),
        ExpressionType.OrElse => Expression.OrElse(left, right),
        ExpressionType.AndAlso => Expression.AndAlso(left, right),
        _ => throw new ArgumentOutOfRangeException("operationType")
      };

      if (previous == null) {
        return binaryExpression;
      }

      return concatenationExpression switch {
        ExpressionType.AndAlso => Expression.AndAlso(previous, binaryExpression),
        ExpressionType.OrElse => Expression.OrElse(previous, binaryExpression),
        _ => throw new ArgumentOutOfRangeException("concatenationExpression")
      };
    }

    private static ProjectionExpression CreateLocalCollectionProjectionExpression(Type itemType, object value, Translator translator, Expression sourceExpression)
    {
      var storedEntityType = translator.State.TypeOfEntityStoredInKey;
      var translatorContext = translator.context;
      var itemToTupleConverter = ItemToTupleConverter.BuildConverter(itemType, storedEntityType, value, translatorContext.Model, sourceExpression);
      var tupleDescriptor = itemToTupleConverter.TupleDescriptor;
      var columns = tupleDescriptor
        .Select(x => new SystemColumn(translatorContext.GetNextColumnAlias(), 0, x))
        .Cast<Column>()
        .ToArray(tupleDescriptor.Count);
      var rsHeader = new RecordSetHeader(tupleDescriptor, columns);
      var rawProvider = new RawProvider(rsHeader, itemToTupleConverter.GetEnumerable());
      var recordset = new StoreProvider(rawProvider);
      var itemProjector = new ItemProjectorExpression(itemToTupleConverter.Expression, recordset, translatorContext);
      if (translator.State.JoinLocalCollectionEntity)
        itemProjector = EntityExpressionJoiner.JoinEntities(translator, itemProjector);
      return new ProjectionExpression(itemType, itemProjector, TranslatedQuery.EmptyTupleParameterBindings);
    }

    private Expression BuildInterfaceExpression(MemberExpression ma)
    {
      var @interface = ma.Expression.Type;
      var property = (PropertyInfo) ma.Member;
      var implementors = context.Model.Types[@interface].AllImplementors;
      var fields = implementors
        .Select(im => im.UnderlyingType.GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public))
        .Concat(implementors
          .Select(im => im.UnderlyingType.GetProperty($"{@interface.Name}.{property.Name}", BindingFlags.Instance | BindingFlags.NonPublic)))
        .Where(f => f != null);

      return BuildExpression(ma, fields);
    }

    private Expression BuildHierarchyExpression(MemberExpression ma)
    {
      var ancestor = ma.Expression.Type;
      var property = (PropertyInfo) ma.Member;
      var descendants = context.Model.Types[ancestor].AllDescendants;
      var fields = descendants
        .Select(im => im.UnderlyingType.GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        .Where(f => f != null);

      return BuildExpression(ma, fields);
    }

    private Expression BuildExpression(MemberExpression ma, IEnumerable<PropertyInfo> fields)
    {
      object defaultValue = null;
      var propertyType = ((PropertyInfo) ma.Member).PropertyType;
      if (propertyType.IsValueType && !propertyType.IsNullable())
        defaultValue = System.Activator.CreateInstance(propertyType);

      Expression current = Expression.Constant(defaultValue, propertyType);
      foreach (var field in fields) {
        var compiler = context.CustomCompilerProvider.GetCompiler(field);
        if (compiler == null)
          continue;
        var expression = compiler.Invoke(Expression.TypeAs(ma.Expression, field.ReflectedType), null);
        current = Expression.Condition(Expression.TypeIs(ma.Expression, field.ReflectedType), expression, current);
      }
      return current;
    }

    private MemberInfo TryGetActualPropertyInfo(PropertyInfo propertyInfo, Type initializingType)
    {
      //if property is an indexer
      if (propertyInfo.GetIndexParameters().Length != 0)
        return propertyInfo;

      // the name of property is unique within type hierarchy
      // so we can use it.
      var actualPropertyInfo = initializingType.GetProperty(propertyInfo.Name);
      return actualPropertyInfo ?? propertyInfo;
    }

    private IList<ColumnInfo> GetColumnsToSearch(ConstantExpression arrayOfColumns, Type elementType, TypeInfo domainType)
    {
      var columnAccessLambdas = (LambdaExpression[]) arrayOfColumns.Value;
      var fulltextFields = new List<ColumnInfo>();
      foreach (var lambda in columnAccessLambdas) {
        var field = FieldExtractor.Extract(lambda, elementType, domainType);
        if (field.Column == null)
          throw new InvalidOperationException(string.Format(Strings.FieldXIsComplexAndCannotBeUsedForSearch, lambda.Body));
        fulltextFields.Add(field.Column);
      }
      return fulltextFields;
    }

    private void ArrangeContainsTableParameters(System.Collections.ObjectModel.ReadOnlyCollection<Expression> parameters, out Expression searchCriteria, out Expression columns, out Expression topNByRank)
    {
      searchCriteria = parameters[0];
      columns = null;
      topNByRank = null;
      if (parameters.Count == 2) {
        if (parameters[1].Type.IsArray)
          columns = parameters[1];
        else
          topNByRank = parameters[1];
      }
      if (parameters.Count == 3) {
        columns = parameters[1];
        topNByRank = parameters[2];
      }
    }

    #endregion
  }
}
