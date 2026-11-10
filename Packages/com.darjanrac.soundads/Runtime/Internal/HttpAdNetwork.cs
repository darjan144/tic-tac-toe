using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace SoundAds.Internal
{
    /// <summary>
    /// Fetches ad metadata from the SoundAds server (<c>GET /v1/ads</c>) and then downloads the
    /// audio creative, so the ad is fully buffered before <see cref="SoundAdsSdk.OnAdLoaded"/> fires.
    /// </summary>
    internal sealed class HttpAdNetwork : IAdNetwork
    {
        readonly string _baseUrl;
        readonly string _apiKey;
        readonly int _timeoutSec;

        public HttpAdNetwork(string baseUrl, string apiKey, int timeoutSec)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _timeoutSec = timeoutSec;
        }

        public string Name => "Http";

        public async Awaitable<AdLoadResult> RequestAdAsync(AdRequest request, CancellationToken cancellationToken)
        {
            string url = $"{_baseUrl}/v1/ads" +
                         $"?placement={UnityWebRequest.EscapeURL(request.PlacementId)}" +
                         $"&format={request.Format.ToWire()}" +
                         $"&moment={request.Moment.ToWire()}" +
                         $"&npa={(request.Personalized ? 0 : 1)}" +
                         $"&apiKey={UnityWebRequest.EscapeURL(_apiKey)}";

            AdResponse metadata;
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = _timeoutSec;
                req.SetRequestHeader("X-Api-Key", _apiKey);
                req.SetRequestHeader("X-SoundAds-Sdk", "unity/" + SoundAdsSdk.Version);

                await SendAsync(req, cancellationToken);

                if (req.result == UnityWebRequest.Result.ConnectionError)
                    return AdLoadResult.Fail(AdFailureReason.NetworkError, transport: true);

                if (req.responseCode == 204)
                    return AdLoadResult.Fail(AdFailureReason.NoFill);

                if (req.result != UnityWebRequest.Result.Success)
                    return AdLoadResult.Fail($"Http{req.responseCode}");

                try
                {
                    metadata = JsonUtility.FromJson<AdResponse>(req.downloadHandler.text);
                }
                catch (Exception e)
                {
                    SoundAdsLog.Warn($"Malformed ad response: {e.Message}");
                    return AdLoadResult.Fail("MalformedResponse");
                }
            }

            if (metadata == null || string.IsNullOrEmpty(metadata.audioUrl))
                return AdLoadResult.Fail("MalformedResponse");

            var clip = await DownloadClipAsync(metadata.audioUrl, cancellationToken);
            if (clip == null)
                return AdLoadResult.Fail(AdFailureReason.NetworkError, transport: true);

            if (metadata.durationSec <= 0f) metadata.durationSec = clip.length;
            if (string.IsNullOrEmpty(metadata.placementId)) metadata.placementId = request.PlacementId;

            return AdLoadResult.Ok(new LoadedAd { Metadata = metadata, Clip = clip });
        }

        /// <summary>Lightweight reachability probe used during initialisation.</summary>
        public async Awaitable<bool> PingAsync(CancellationToken cancellationToken)
        {
            using var req = UnityWebRequest.Get(_baseUrl + "/health");
            req.timeout = Mathf.Min(_timeoutSec, 3);
            await SendAsync(req, cancellationToken);
            return req.result == UnityWebRequest.Result.Success;
        }

        async Awaitable<AudioClip> DownloadClipAsync(string audioUrl, CancellationToken cancellationToken)
        {
            using var req = UnityWebRequestMultimedia.GetAudioClip(audioUrl, GuessAudioType(audioUrl));
            req.timeout = _timeoutSec;
            await SendAsync(req, cancellationToken);

            if (req.result != UnityWebRequest.Result.Success)
            {
                SoundAdsLog.Warn($"Audio download failed ({req.error}) for {audioUrl}");
                return null;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip != null) clip.name = "SoundAds_" + Path.GetFileNameWithoutExtension(audioUrl);
            return clip;
        }

        static async Awaitable SendAsync(UnityWebRequest req, CancellationToken cancellationToken)
        {
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    req.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                await Awaitable.NextFrameAsync();
            }
        }

        static AudioType GuessAudioType(string url)
        {
            string path = url;
            int query = path.IndexOf('?');
            if (query >= 0) path = path.Substring(0, query);

            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                ".mp3" => AudioType.MPEG,
                _ => AudioType.UNKNOWN
            };
        }
    }
}
