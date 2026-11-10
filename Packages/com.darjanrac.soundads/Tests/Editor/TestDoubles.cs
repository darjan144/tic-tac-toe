using System.Collections.Generic;
using System.Threading;
using SoundAds.Internal;
using UnityEngine;

namespace SoundAds.Tests
{
    /// <summary>Records every batch instead of sending it; can be told to fail.</summary>
    internal sealed class CapturingTransport : IEventTransport
    {
        public readonly List<string> Sent = new();
        public bool Succeed = true;

        public Awaitable<bool> SendAsync(string json)
        {
            if (Succeed) Sent.Add(json);
            var source = new AwaitableCompletionSource<bool>();
            source.SetResult(Succeed);
            return source.Awaitable;
        }
    }

    /// <summary>Network that always returns the same canned result synchronously.</summary>
    internal sealed class StubNetwork : IAdNetwork
    {
        readonly AdLoadResult _result;
        public int Calls;

        public StubNetwork(AdLoadResult result) => _result = result;

        public string Name => "Stub";

        public Awaitable<AdLoadResult> RequestAdAsync(AdRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            var source = new AwaitableCompletionSource<AdLoadResult>();
            source.SetResult(_result);
            return source.Awaitable;
        }
    }
}
