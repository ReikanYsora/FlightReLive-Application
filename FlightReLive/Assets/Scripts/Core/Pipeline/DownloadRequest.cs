using System;

namespace FlightReLive.Core.Pipeline.Download
{
    internal class DownloadRequest
    {
        #region PROPERTIES
        public string Url { get; }

        public Action<byte[]> OnSuccess { get; }

        public Action<string> OnError { get; }

        public Action<long, long> OnProgress { get; }
        #endregion

        #region CONSTRUCTOR
        public DownloadRequest(string url, Action<byte[]> onSuccess, Action<string> onError, Action<long, long> onProgress = null)
        {
            Url = url;
            OnSuccess = onSuccess;
            OnError = onError;
            OnProgress = onProgress;
        }
        #endregion
    }
}
