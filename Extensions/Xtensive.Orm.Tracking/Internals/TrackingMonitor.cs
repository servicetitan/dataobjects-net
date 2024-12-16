namespace Xtensive.Orm.Tracking
{
  internal abstract class TrackingMonitor : ITrackingMonitor
  {
    private int disableCount;

    public event EventHandler<TrackingCompletedEventArgs> TrackingCompleted;

    public void Disable()
    {
      disableCount++;
    }

    public void Enable()
    {
      if (disableCount==0)
        throw new InvalidOperationException("Tracking monitor is not disabled");
      disableCount--;
    }

    protected void RaiseTrackingCompleted(TrackingCompletedEventArgs e)
    {
      if (disableCount > 0)
        return;
      TrackingCompleted?.Invoke(this, e);
    }
  }
}
