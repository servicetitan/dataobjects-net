// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.02.10

using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Xtensive.Core;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Linq.Expressions;
using Xtensive.Orm.Linq.MemberCompilation;
using Xtensive.Orm.Linq.Rewriters;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using Xtensive.Reflection;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Linq
{
  internal sealed class TranslatorContext
  {
    private static readonly IReadOnlyList<string> ColumnAliasPrefixes = ["c01umn"];

    public static readonly CompositeFormat ResultAliasFormat = CompositeFormat.Parse("#{0}{1}");

    private AliasGenerator resultAliasGenerator = AliasGenerator.Create(ResultAliasFormat);
    private AliasGenerator columnAliasGenerator = AliasGenerator.Create(ColumnAliasPrefixes);
    private readonly Dictionary<ParameterExpression, Parameter<Tuple>> tupleParameters = new();
    private readonly Dictionary<CompilableProvider, ApplyParameter> applyParameters = new();
    private readonly Dictionary<ParameterExpression, ItemProjectorExpression> boundItemProjectors = new();
    private readonly Dictionary<MemberInfo, int> queryReuses = new();

    public readonly CompilerConfiguration RseCompilerConfiguration;

    public ProviderInfo ProviderInfo { get; }

    public Expression Query { get; }

    public Domain Domain { get; }

    public DomainModel Model { get; }

    public TypeIdRegistry TypeIdRegistry { get; }

    public IMemberCompilerProvider<Expression> CustomCompilerProvider { get; }

    public Translator Translator { get; }

    public ExpressionEvaluator Evaluator { get; }

    public ParameterExtractor ParameterExtractor { get; }

    public LinqBindingCollection Bindings { get; } = new();

    public IReadOnlyList<string> SessionTags { get; private set; }

    public bool IsRoot(Expression expression) => Query == expression;

    public string GetNextAlias() => resultAliasGenerator.Next();

    public string GetNextColumnAlias() => columnAliasGenerator.Next();

    public ApplyParameter GetApplyParameter(ProjectionExpression projection) => GetApplyParameter(projection.ItemProjector.DataSource);

    public ApplyParameter GetApplyParameter(CompilableProvider provider)
    {
      ref var parameter = ref CollectionsMarshal.GetValueRefOrAddDefault(applyParameters, provider, out var exists);
      if (!exists) {
        var providerType = provider.GetType();
        parameter = new ApplyParameter(providerType.IsGenericType ? providerType.GetShortName() : providerType.Name);
        // parameter = new ApplyParameter(provider.ToString()); 
        // ENABLE ONLY FOR DEBUGGING! 
        // May lead TO entity.ToString() calls, while ToString can be overridden.
      }
      return parameter;
    }

    public IReadOnlyList<string> GetMainQueryTags() =>
      Domain.TagsEnabled
        ? applyParameters.Keys.OfType<TagProvider>().Select(p => p.Tag).ToList()
        : Array.Empty<string>();

    internal readonly struct TagsRestorer : IDisposable
    {
      private readonly TranslatorContext context;
      private readonly IReadOnlyList<string> originalTags;

      public void Dispose() => context.SessionTags = originalTags;

      internal TagsRestorer(TranslatorContext context)
      {
        this.context = context;
        originalTags = context.SessionTags;
        context.SessionTags = null;
      }
    }

    public TagsRestorer DisableSessionTags() => new(this);

    public void RebindApplyParameter(CompilableProvider old, CompilableProvider @new)
    {
      if (applyParameters.TryGetValue(old, out var parameter)) {
        applyParameters[@new] = parameter;
      }
    }

    public Parameter<Tuple> GetTupleParameter(ParameterExpression expression)
    {
      ref var parameter = ref CollectionsMarshal.GetValueRefOrAddDefault(tupleParameters, expression, out var exists);
      return exists ? parameter : (parameter = new(expression.ToString()));
    }

    public ItemProjectorExpression GetBoundItemProjector(ParameterExpression parameter, ItemProjectorExpression itemProjector)
    {
      ref var result = ref CollectionsMarshal.GetValueRefOrAddDefault(boundItemProjectors, parameter, out var exists);
      return exists ? result : (result = itemProjector.BindOuterParameter(parameter));
    }

    public void RegisterPossibleQueryReuse(MemberInfo memberInfo)
    {
      _ = queryReuses.TryAdd(memberInfo, 0);
    }

    public bool CheckIfQueryReusePossible(MemberInfo memberInfo)
    {
      if (queryReuses.TryGetValue(memberInfo, out var uses)) {
        queryReuses[memberInfo] = uses + 1;
        return uses > 0;
      }
      return false;
    }

    private Expression ApplyPreprocessor(IQueryPreprocessor preprocessor, Session session, Expression query)
    {
      return preprocessor is IQueryPreprocessor2 preprocessor2
        ? preprocessor2.Apply(session, query)
        : preprocessor.Apply(query);
    }

    // Constructors

    public TranslatorContext(Session session, in CompilerConfiguration rseCompilerConfiguration, Expression query,
      CompiledQueryProcessingScope compiledQueryScope)
    {
      ArgumentNullException.ThrowIfNull(session);
      ArgumentNullException.ThrowIfNull(query);

      Domain = session.Domain;
      RseCompilerConfiguration = rseCompilerConfiguration;
      SessionTags = (Domain.TagsEnabled) ? session.Tags : null;

      // Applying query preprocessors
      query = Domain.Handler.QueryPreprocessors
        .Aggregate(query, (current, preprocessor) => ApplyPreprocessor(preprocessor, session, current));

      // Built-in preprocessors
      query = AggregateOptimizer.Rewrite(query);
      query = ClosureAccessRewriter.Rewrite(query, compiledQueryScope);
      query = EqualityRewriter.Rewrite(query);
      query = EntitySetAccessRewriter.Rewrite(query);
      query = SubqueryDefaultResultRewriter.Rewrite(query);
      Evaluator = new ExpressionEvaluator(query);
      query = PersistentIndexerRewriter.Rewrite(query, this);
      Query = query;

      CustomCompilerProvider = Domain.Handler.GetMemberCompilerProvider<Expression>();
      Model = Domain.Model;
      TypeIdRegistry = session.StorageNode.TypeIdRegistry;
      ProviderInfo = Domain.Handlers.ProviderInfo;
      Translator = new Translator(this, compiledQueryScope);
      ParameterExtractor = new ParameterExtractor(Evaluator);
    }
  }
}
