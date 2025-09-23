using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FlightReLive.Core.Pipeline.Download
{
    internal static class DownloadManager
    {
        #region ATTRIBUTES
        private const int MAX_CONCURRENT = 64;
        private static readonly Queue<DownloadRequest> _downloadQueue = new Queue<DownloadRequest>();
        private static readonly HashSet<string> _activeUrls = new HashSet<string>();
        private static int _inflight = 0;
        private static bool _isProcessing = false;
        private static float _progress = 0f;
        #endregion

        #region PROPERTIES
        internal static float Progress => _progress;
        #endregion

        #region METHODS
        internal static void EnqueueDownload(string url, Action<byte[]> onSuccess, Action<string> onError, Action<long, long> onProgress = null)
        {
            if (_activeUrls.Contains(url))
                return;

            _activeUrls.Add(url);
            _downloadQueue.Enqueue(new DownloadRequest(url, onSuccess, onError, onProgress));

            if (!_isProcessing)
            {
                _isProcessing = true;
                TryPumpQueue();
            }
        }

        private static void TryPumpQueue()
        {
            while (_inflight < MAX_CONCURRENT && _downloadQueue.Count > 0)
            {
                var req = _downloadQueue.Dequeue();
                _ = RunOneAsync(req);
            }

            if (_downloadQueue.Count == 0 && _inflight == 0)
            {
                _progress = 1f;
                _isProcessing = false;
            }
        }

        private static async Task RunOneAsync(DownloadRequest request)
        {
            Interlocked.Increment(ref _inflight);

            try
            {
                await DownloadAsync(request);
            }
            finally
            {
                _activeUrls.Remove(request.Url);
                Interlocked.Decrement(ref _inflight);
                TryPumpQueue();
            }
        }

        private static async Task DownloadAsync(DownloadRequest request)
        {
            using var uwr = UnityWebRequest.Get(request.Url);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            UnityWebRequestAsyncOperation op = uwr.SendWebRequest();
            long totalBytes = 0;

            Dictionary<string, string> headers = uwr.GetResponseHeaders();
            if (headers != null && headers.TryGetValue("CONTENT-LENGTH", out var len) && long.TryParse(len, out var parsed))
            {
                totalBytes = parsed;
            }

            while (!op.isDone)
            {
                _progress = op.progress;

                long received = (long)uwr.downloadedBytes;
                request.OnProgress?.Invoke(received, totalBytes);

                await Task.Yield();
            }

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                byte[] data = uwr.downloadHandler.data;

                UnityMainThreadDispatcher.AddActionInMainThread(() => request.OnSuccess?.Invoke(data));
            }
            else
            {
                UnityMainThreadDispatcher.AddActionInMainThread(() => request.OnError?.Invoke(uwr.error));
            }
        }
        #endregion
    }
}
