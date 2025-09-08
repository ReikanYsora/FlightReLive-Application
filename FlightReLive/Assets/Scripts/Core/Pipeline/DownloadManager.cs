using FlightReLive.Core.Pipeline.Download;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FlightReLive.Core.Pipeline.Download
{
    internal static class DownloadManager
    {
        #region ATTRIBUTES
        private static readonly Queue<DownloadRequest> _downloadQueue = new Queue<DownloadRequest>();
        private static readonly HashSet<string> _activeUrls = new HashSet<string>();
        private static bool _isProcessing = false;
        private static float _progress = 0f;
        #endregion

        #region PROPERTIES
        internal static float Progress
        {
            get { return _progress; }
        }
        #endregion

        #region METHODS
        internal static void EnqueueDownload(string url, Action<byte[]> onSuccess, Action<string> onError)
        {
            if (_activeUrls.Contains(url))
            {
                return;
            }

            _activeUrls.Add(url);
            _downloadQueue.Enqueue(new DownloadRequest(url, onSuccess, onError));

            if (!_isProcessing)
            {
                ProcessQueue();
            }
        }


        private static async void ProcessQueue()
        {
            _isProcessing = true;

            while (_downloadQueue.Count > 0)
            {
                var request = _downloadQueue.Dequeue();
                await DownloadAsync(request);
            }

            _progress = 1f;
            _isProcessing = false;
        }

        private static async Task DownloadAsync(DownloadRequest request)
        {
            using var uwr = UnityWebRequest.Get(request.Url);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            var operation = uwr.SendWebRequest();

            while (!operation.isDone)
            {
                _progress = operation.progress;
                await Task.Yield();
            }

            _activeUrls.Remove(request.Url);

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                byte[] rawData = uwr.downloadHandler.data;
                var safeCopy = new MemoryStream(rawData.Length);
                safeCopy.Write(rawData, 0, rawData.Length);
                safeCopy.Position = 0;

                UnityMainThreadDispatcher.AddActionInMainThread(() => request.OnSuccess?.Invoke(safeCopy.ToArray()));
            }
            else
            {
                UnityMainThreadDispatcher.AddActionInMainThread(() => request.OnError?.Invoke(uwr.error));
            }
        }

        #endregion
    }
}
