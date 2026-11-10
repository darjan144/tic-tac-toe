using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SoundAds.Internal
{
    /// <summary>One tracking beacon (impression, quartile, complete, mute, reward...).</summary>
    [Serializable]
    internal struct AdEvent
    {
        public string adId;
        public string placementId;
        public string type;
        public string timestampUtc;
        public string sessionId;
        public string moment;
    }

    [Serializable]
    internal struct AdEventBatch
    {
        public AdEvent[] events;
    }

    /// <summary>Where batches go. Swappable so tests can capture what would be sent.</summary>
    internal interface IEventTransport
    {
        Awaitable<bool> SendAsync(string json);
    }

    internal sealed class HttpEventTransport : IEventTransport
    {
        readonly string _url;
        readonly string _apiKey;
        readonly int _timeoutSec;

        public HttpEventTransport(string baseUrl, string apiKey, int timeoutSec)
        {
            _url = baseUrl.TrimEnd('/') + "/v1/events";
            _apiKey = apiKey;
            _timeoutSec = timeoutSec;
        }

        public async Awaitable<bool> SendAsync(string json)
        {
            using var req = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _timeoutSec
            };
            req.SetRequestHeader("X-Api-Key", _apiKey);

            var op = req.SendWebRequest();
            while (!op.isDone) await Awaitable.NextFrameAsync();
            return req.result == UnityWebRequest.Result.Success;
        }
    }

    /// <summary>Offline transport: events are only logged (test mode / server unreachable).</summary>
    internal sealed class LogEventTransport : IEventTransport
    {
        public Awaitable<bool> SendAsync(string json)
        {
            SoundAdsLog.Info($"Events (offline, not uploaded): {json}");
            var source = new AwaitableCompletionSource<bool>();
            source.SetResult(true);
            return source.Awaitable;
        }
    }

    /// <summary>
    /// Queues events and uploads them in batches. Failed uploads are re-queued and retried with
    /// exponential backoff, so a flaky connection never loses impressions (up to the queue cap).
    /// </summary>
    internal sealed class EventTracker
    {
        internal const int MaxQueueSize = 500;
        internal const int MaxBatchSize = 50;
        const float MaxBackoffSec = 60f;

        readonly IEventTransport _transport;
        readonly float _flushIntervalSec;
        readonly List<AdEvent> _queue = new();

        float _nextFlushTime;
        int _consecutiveFailures;
        bool _sending;

        public EventTracker(IEventTransport transport, float flushIntervalSec)
        {
            _transport = transport;
            _flushIntervalSec = flushIntervalSec;
        }

        public int PendingCount => _queue.Count;

        /// <summary>Session id attached to events; empty when the user has not consented to personalisation.</summary>
        public string SessionId { get; set; } = string.Empty;

        public void Track(string type, AdResponse ad, AdMoment moment)
        {
            if (_queue.Count >= MaxQueueSize)
                _queue.RemoveAt(0); // drop oldest rather than growing without bound

            _queue.Add(new AdEvent
            {
                adId = ad?.adId ?? string.Empty,
                placementId = ad?.placementId ?? string.Empty,
                type = type,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                sessionId = SessionId,
                moment = moment.ToWire()
            });

            SoundAdsLog.Info($"Event '{type}' queued for {ad?.adId} ({_queue.Count} pending)");
        }

        /// <summary>Call every frame; flushes when the interval (or backoff) has elapsed.</summary>
        public void Tick(float time)
        {
            if (_sending || _queue.Count == 0 || time < _nextFlushTime) return;
            _ = FlushAsync(time);
        }

        public async Awaitable FlushAsync(float time)
        {
            if (_sending || _queue.Count == 0) return;
            _sending = true;

            int count = Mathf.Min(_queue.Count, MaxBatchSize);
            var batch = _queue.GetRange(0, count);
            _queue.RemoveRange(0, count);

            bool ok;
            try
            {
                ok = await _transport.SendAsync(JsonUtility.ToJson(new AdEventBatch { events = batch.ToArray() }));
            }
            catch (Exception e)
            {
                SoundAdsLog.Warn($"Event upload threw: {e.Message}");
                ok = false;
            }

            if (ok)
            {
                _consecutiveFailures = 0;
                _nextFlushTime = time + _flushIntervalSec;
                SoundAdsLog.Info($"Uploaded {count} event(s)");
            }
            else
            {
                _queue.InsertRange(0, batch); // keep order; retry later
                if (_queue.Count > MaxQueueSize) _queue.RemoveRange(0, _queue.Count - MaxQueueSize);

                _consecutiveFailures++;
                float backoff = Mathf.Min(_flushIntervalSec * Mathf.Pow(2f, _consecutiveFailures), MaxBackoffSec);
                _nextFlushTime = time + backoff;
                SoundAdsLog.Warn($"Event upload failed; retrying in {backoff:0}s ({_queue.Count} pending)");
            }

            _sending = false;
        }
    }
}
