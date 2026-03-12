// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.08.19

using System.Collections.Frozen;
using System.Globalization;

namespace Xtensive.Orm.Weaver;

public sealed class MessageLogger
{
  private static readonly FrozenDictionary<MessageCode, string> Messages = new Dictionary<MessageCode, string>() {
    [MessageCode.ErrorInternal] = "Internal error",
    [MessageCode.ErrorInputFileIsNotFound] = "Input file is not found",
    [MessageCode.ErrorStrongNameKeyIsNotFound] = "Strong name key file is not found",
    [MessageCode.ErrorUnableToLocateOrmAssembly] = "Unable to locate Xtensive.Orm assembly",
    [MessageCode.ErrorUnableToFindReferencedAssembly] = "Unable to find referenced assembly",
    [MessageCode.ErrorUnableToRemoveBackingField] = "Unable to remove backing field",
    [MessageCode.ErrorEntityLimitIsExceeded] = "Number of persistent types in assembly exceeds the maximal available types per assembly for Community Edition. " +
                                               "Consider upgrading to any commercial edition of DataObjects.Net.",
    [MessageCode.ErrorLicenseIsInvalid] = "DataObjects.Net license is invalid.",
    [MessageCode.ErrorSubscriptionExpired] = "Your subscription expired and is not valid for this release of DataObjects.Net.",
    [MessageCode.ErrorPersistentPropertiesWereNotProcessed] = "Some of perstistent properties were not processed. Check warnings for details.",
    [MessageCode.WarningDebugSymbolsFileIsNotFound] = "Debug symbols file is not found",
    [MessageCode.WarningReferencedAssemblyFileIsNotFound] = "Referenced assembly file is not found",
    [MessageCode.WarningPersistentPropertyHasNoSetterOrGetter] = "Persistent property was skipped due to it has no setter or getter"
  }.ToFrozenDictionary();

  private readonly MessageWriter writer;

  public void Write(MessageCode code)
  {
    Write(code, null, null, null);
  }

  public void Write(MessageCode code, MessageLocation location)
  {
    Write(code, null, location, null);
  }

  public void Write(MessageCode code, string extraInformation)
  {
    Write(code, extraInformation, null, null);
  }

  public void Write(MessageCode code, string extraInformation, MessageLocation location)
  {
    Write(code, extraInformation, location, null);
  }

  public void Write(MessageCode code, Exception exception)
  {
    Write(code, exception.Message, null, exception);
  }

  public void Write(MessageCode code, string extraInformation, MessageLocation location, Exception exception)
  {
    string description;
    if (!Messages.TryGetValue(code, out description))
      throw new ArgumentOutOfRangeException("code");

    var messageCode = string.Format(CultureInfo.InvariantCulture, "XW{0:0000}", (int) code);
    var messageText = string.IsNullOrEmpty(extraInformation)
      ? description
      : $"{description}: {extraInformation}";

    var message = new ProcessorMessage {
      MessageCode = messageCode,
      MessageText = messageText,
      Location = location,
      Type = GetMessageType(code),
      Exception = exception
    };

    writer.Write(message);
  }

  private static MessageType GetMessageType(MessageCode code)
  {
    var value = (int) code;
    return value >= 1000 ? MessageType.Warning : MessageType.Error;
  }

  public MessageLogger(MessageWriter writer)
  {
    ArgumentNullException.ThrowIfNull(writer);
    this.writer = writer;
  }
}
