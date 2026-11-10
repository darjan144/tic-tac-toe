using System.Collections.Concurrent;

namespace SoundAds.Server;

/// <summary>Thread-safe in-memory event store with simple aggregate reporting.</summary>
public sealed class EventStore
{
    const int MaxEvents = 100_000;

    readonly ConcurrentQueue<AdEvent> _events = new();

    public int Count => _events.Count;

    public int Add(IEnumerable<AdEvent> events)
    {
        int accepted = 0;
        foreach (var e in events)
        {
            if (string.IsNullOrWhiteSpace(e.Type)) continue;
            _events.Enqueue(e);
            accepted++;
        }

        while (_events.Count > MaxEvents && _events.TryDequeue(out _)) { }
        return accepted;
    }

    public AdReport BuildReport()
    {
        var snapshot = _events.ToArray();

        return new AdReport(
            TotalEvents: snapshot.Length,
            UniqueSessions: snapshot.Select(e => e.SessionId).Where(s => !string.IsNullOrEmpty(s)).Distinct().Count(),
            ByAd: Aggregate(snapshot, e => e.AdId),
            ByMoment: Aggregate(snapshot, e => e.Moment),
            ByPlacement: Aggregate(snapshot, e => e.PlacementId));
    }

    static AdReportRow[] Aggregate(AdEvent[] events, Func<AdEvent, string> key) =>
        events
            .GroupBy(e => string.IsNullOrEmpty(key(e)) ? "(none)" : key(e))
            .Select(g =>
            {
                int impressions = g.Count(e => e.Type == "impression");
                int completes = g.Count(e => e.Type == "complete");
                return new AdReportRow(
                    g.Key,
                    impressions,
                    completes,
                    g.Count(e => e.Type == "mute"),
                    g.Count(e => e.Type == "reward"),
                    impressions == 0 ? 0 : Math.Round(completes / (double)impressions, 3));
            })
            .OrderByDescending(r => r.Impressions)
            .ToArray();
}
