using System.Diagnostics.Metrics;

namespace Xtensive.Diagnostics;

public class Metrics
{
  public static bool IsEnabled { get; } = Environment.GetEnvironmentVariable("DO_Diagnostics") is "1" or "true";

  public static Meter Meter { get; } = new("DataObjects");

  public static readonly Counter<long> BuffersReceived = Meter.CreateCounter<long>("SqlClient.BuffersReceived");
  public static readonly Counter<long> ServerRoundtrips = Meter.CreateCounter<long>("SqlClient.ServerRoundtrips");
  public static readonly Counter<long> SelectRows = Meter.CreateCounter<long>("SqlClient.SelectRows");
  public static readonly Counter<long> Transactions = Meter.CreateCounter<long>("SqlClient.Transactions");

  public static readonly Counter<int> SqlErrorCounter = Meter.CreateCounter<int>("dataobjects.sql_error");

  public static readonly Histogram<int> SqlLength = Meter.CreateHistogram<int>("dataobjects.sql_length");
}
