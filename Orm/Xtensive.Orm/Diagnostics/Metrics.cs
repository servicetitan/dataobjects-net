using System;
using System.Diagnostics.Metrics;

namespace Xtensive.Diagnostics;

public class Metrics
{
  public static bool IsEnabled { get; } = Environment.GetEnvironmentVariable("DO_Diagnostics") is "1" or "true";

  public static Meter Meter { get; } = new("DataObjects");

  public static Counter<long> BuffersReceived = Meter.CreateCounter<long>("SqlClient.SelectRows");
  public static Counter<long> ServerRoundtrips = Meter.CreateCounter<long>("SqlClient.ServerRoundtrips");
  public static Counter<long> SelectRows = Meter.CreateCounter<long>("SqlClient.SelectRows");
  public static Counter<long> Transactions = Meter.CreateCounter<long>("SqlClient.Transactions");

  public static Counter<int> SqlErrorCounter = Meter.CreateCounter<int>("dataobjects.sql_error");
}