// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.08.20

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Core;

namespace Xtensive.Orm.Providers
{
  internal sealed class BatchingCommandProcessor : CommandProcessor, ISqlTaskProcessor
  {
    private readonly int batchSize;
    private Queue<SqlTask> tasks;

    void ISqlTaskProcessor.ProcessTask(SqlLoadTask task, CommandProcessorContext context)
    {
      var part = Factory.CreateQueryPart(task, GetParameterPrefix(context));
      if (PendCommandPart(context.ActiveCommand, part)) {
        return;
      }
      context.ActiveCommand.AddPart(part);
      context.ActiveTasks.Add(task);
      context.CurrentTask = null;
    }

    void ISqlTaskProcessor.ProcessTask(SqlPersistTask task, CommandProcessorContext context)
    {
      if (task.ValidateRowCount) {
        ProcessUnbatchedTask(task, context);
        context.CurrentTask = null;
        return;
      }
      var sequence = Factory.CreatePersistParts(task, GetParameterPrefix(context)).ToList();
      if (PendCommandParts(context.ActiveCommand, sequence)) {
        // higly recommended to no tear apart persist actions if they are batchable
        return;
      }
      foreach (var part in sequence) {
        context.ActiveCommand.AddPart(part);
      }

      context.CurrentTask = null;
    }

    public override void RegisterTask(SqlTask task) => tasks.Enqueue(task);

    public override void ClearTasks() => tasks.Clear();

    public override void ExecuteTasks(CommandProcessorContext context)
    {
      PutTasksForExecution(context);

      var processingTasks = context.ProcessingTasks;
      while (processingTasks.Count >= batchSize) {
        _ = ExecuteBatch(batchSize, null, context);
      }

      if (!context.AllowPartialExecution) {
        while (processingTasks.Count > 0) {
          _ = processingTasks.Count > batchSize
            ? ExecuteBatch(batchSize, null, context)
            : ExecuteBatch(processingTasks.Count, null, context);
        }
      }
      else {
        //re-register task
        for (int i = 0, count = processingTasks.Count; i < count; i++) {
          tasks.Enqueue(processingTasks.Dequeue());
        }
      }
    }

    public override async Task ExecuteTasksAsync(CommandProcessorContext context, CancellationToken token)
    {
      PutTasksForExecution(context);

      var processingTasks = context.ProcessingTasks;
      while (processingTasks.Count >= batchSize) {
        _ = await ExecuteBatchAsync(batchSize, null, context, token).ConfigureAwait(false);
      }

      if (!context.AllowPartialExecution) {
        while (processingTasks.Count > 0) {
          _ = await ((context.ProcessingTasks.Count > batchSize)
            ? ExecuteBatchAsync(batchSize, null, context, token)
            : ExecuteBatchAsync(context.ProcessingTasks.Count, null, context, token));
        }
      }
      else {
        for(int i = 0, count = processingTasks.Count; i < count; i++) {
          tasks.Enqueue(processingTasks.Dequeue());
        }
      }
    }

    public override DataReader ExecuteTasksWithReader(QueryRequest request, CommandProcessorContext context)
    {
      context.AllowPartialExecution = false;
      PutTasksForExecution(context);

      while (context.ProcessingTasks.Count >= batchSize) {
        _ = ExecuteBatch(batchSize, null, context);
      }

      for (; ; ) {
        var currentBatchSize = (context.ProcessingTasks.Count > batchSize) ? batchSize : context.ProcessingTasks.Count;
        var result = ExecuteBatch(currentBatchSize, request, context);
        if (result != null && context.ProcessingTasks.Count == 0) {
          return result.CreateReader(request.GetAccessor());
        }
      }
    }

    public override async Task<DataReader> ExecuteTasksWithReaderAsync(QueryRequest request,
      CommandProcessorContext context, CancellationToken token)
    {
      context.AllowPartialExecution = false;
      PutTasksForExecution(context);

      while (context.ProcessingTasks.Count >= batchSize) {
        _ = await ExecuteBatchAsync(batchSize, null, context, token).ConfigureAwaitFalse();
      }

      for (; ; ) {
        var currentBatchSize = (context.ProcessingTasks.Count > batchSize) ? batchSize : context.ProcessingTasks.Count;
        var result = await ExecuteBatchAsync(currentBatchSize, request, context, token).ConfigureAwait(false);
        if (result != null && context.ProcessingTasks.Count == 0) {
          return result.CreateReader(request.GetAccessor());
        }
      }
    }

    #region Private / internal methods

    private Command ExecuteBatch(int numberOfTasks, QueryRequest lastRequest, CommandProcessorContext context)
    {
      if (numberOfTasks == 0 && lastRequest == null) {
        return null;
      }

      AllocateCommand(context);

      var shouldReturnReader = false;
      var tasksToProcess = context.ProcessingTasks;
      try {
        while (numberOfTasks > 0 && tasksToProcess.Count > 0) {
          var task = tasksToProcess.Peek();
          context.CurrentTask = task;
          task.ProcessWith(this, context);
          if (context.CurrentTask == null) {
            numberOfTasks--;
            _ = tasksToProcess.Dequeue();
          }
          else {
            break;
          }
        }

        var command = context.ActiveCommand;
        if (lastRequest != null && tasksToProcess.Count == 0) {
          var part = Factory.CreateQueryPart(lastRequest, context.ParameterContext);
          if (!PendCommandPart(command, part)) {
            shouldReturnReader = true;
            command.AddPart(part);
          }
        }

        if (command.Count==0) {
          return null;
        }

        var hasQueryTasks = context.ActiveTasks.Count > 0;

        if (!hasQueryTasks && !shouldReturnReader) {
          _ = command.ExecuteNonQuery();
          return null;
        }

        command.ExecuteReader();
        if (hasQueryTasks) {
          var currentQueryTask = 0;
          while (currentQueryTask < context.ActiveTasks.Count) {
            var queryTask = context.ActiveTasks[currentQueryTask];
            var accessor = queryTask.Request.GetAccessor();
            var result = queryTask.Output;
            while (command.NextRow()) {
              result.Add(command.ReadTupleWith(accessor));
            }
            _ = command.NextResult();
            currentQueryTask++;
          }
        }

        return shouldReturnReader ? command : null;
      }
      finally {
        if (!shouldReturnReader) {
          context.ActiveCommand.DisposeSafely();
        }
        ReleaseCommand(context);
      }
    }

    private async Task<Command> ExecuteBatchAsync(int numberOfTasks, QueryRequest lastRequest,
      CommandProcessorContext context, CancellationToken token)
    {
      if (numberOfTasks == 0 && lastRequest == null) {
        return null;
      }

      AllocateCommand(context);

      var shouldReturnReader = false;
      var tasksToProcess = context.ProcessingTasks;
      try {
        while (numberOfTasks > 0 && tasksToProcess.Count > 0) {
          var task = tasksToProcess.Peek();
          context.CurrentTask = task;
          task.ProcessWith(this, context);
          if (context.CurrentTask == null) {
            numberOfTasks--;
            _ = tasksToProcess.Dequeue();
          }
          else {
            break;
          }
        }

        var command = context.ActiveCommand;
        if (lastRequest != null && tasksToProcess.Count == 0) {
          var part = Factory.CreateQueryPart(lastRequest, context.ParameterContext);
          if (!PendCommandPart(command, part)) {
            shouldReturnReader = true;
            command.AddPart(part);
          }
        }

        if (command.Count==0) {
          return null;
        }

        var hasQueryTasks = context.ActiveTasks.Count > 0;
        if (!hasQueryTasks && !shouldReturnReader) {
          _ = await command.ExecuteNonQueryAsync(token).ConfigureAwaitFalse();
          return null;
        }

        await command.ExecuteReaderAsync(token).ConfigureAwaitFalse();
        if (hasQueryTasks) {
          var currentQueryTask = 0;
          while (currentQueryTask < context.ActiveTasks.Count) {
            var queryTask = context.ActiveTasks[currentQueryTask];
            var accessor = queryTask.Request.GetAccessor();
            var result = queryTask.Output;
            while (await command.NextRowAsync(token).ConfigureAwaitFalse()) {
              result.Add(command.ReadTupleWith(accessor));
            }
            _ = await command.NextResultAsync(token).ConfigureAwaitFalse();
            currentQueryTask++;
          }
        }

        return shouldReturnReader ? command : null;
      }
      finally {
        if (!shouldReturnReader) {
          await context.ActiveCommand.DisposeSafelyAsync().ConfigureAwaitFalse();
        }

        ReleaseCommand(context);
      }
    }

    private void ProcessUnbatchedTask(SqlPersistTask task, CommandProcessorContext context)
    {
      if (context.ActiveCommand.Count > 0) {
        _ = context.ActiveCommand.ExecuteNonQuery();
        ReleaseCommand(context);
        AllocateCommand(context);
      }

      ExecuteUnbatchedTask(task);
    }

    private void ExecuteUnbatchedTask(SqlPersistTask task)
    {
      var sequence = Factory.CreatePersistParts(task);
      foreach (var part in sequence) {
        using (var command = Factory.CreateCommand()) {
          ValidateCommandPartParameters(part);
          command.AddPart(part);
          var affectedRowsCount = command.ExecuteNonQuery();
          if (affectedRowsCount == 0) {
            throw new VersionConflictException(string.Format(
              Strings.ExVersionOfEntityWithKeyXDiffersFromTheExpectedOne, task.EntityKey));
          }
        }
      }
    }

    private void PutTasksForExecution(CommandProcessorContext context)
    {
      if (context.AllowPartialExecution) {
        var processingTasksCount = tasks.Count / batchSize * batchSize;
        context.ProcessingTasks = new Queue<SqlTask>(processingTasksCount);
        while (context.ProcessingTasks.Count < processingTasksCount) {
          context.ProcessingTasks.Enqueue(tasks.Dequeue());
        }
      }
      else {
        context.ProcessingTasks = tasks;
        tasks = new Queue<SqlTask>(batchSize);
      }
    }

    private bool PendCommandPart(Command currentCommand, CommandPart partToAdd) =>
      PendCommandParts(currentCommand, new[] { partToAdd });

    private bool PendCommandParts(Command currentCommand, ICollection<CommandPart> partsToAdd)
    {
      var currentCount = (currentCommand != null) ? currentCommand.ParametersCount : 0;
      var behavior = GetCommandExecutionBehavior(partsToAdd, currentCount);
      if (behavior == ExecutionBehavior.AsOneCommand) {
        return false;
      }
      return behavior == ExecutionBehavior.TooLargeForAnyCommand
        ? throw new ParametersLimitExceededException(currentCount + partsToAdd.Sum(x => x.Parameters.Count), MaxQueryParameterCount)
        : true;
    }

    private static string GetParameterPrefix(CommandProcessorContext context) =>
      $"p{context.ActiveCommand.Count + 1}_";

    #endregion

    // Constructors

    public BatchingCommandProcessor(CommandFactory factory, int batchSize, int maxQueryParameterCount)
      : base(factory, maxQueryParameterCount)
    {
      ArgumentValidator.EnsureArgumentIsGreaterThan(batchSize, 1, nameof(batchSize));
      this.batchSize = batchSize;
      this.tasks = new Queue<SqlTask>(batchSize);
    }
  }
}