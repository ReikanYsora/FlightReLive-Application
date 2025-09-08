using System;

namespace FlightReLive.Core.Pipeline.Download
{
    internal class DownloadRequest
    {
        #region ATTRIBUTES
        internal string Url;
        internal Action<byte[]> OnSuccess;
        internal Action<string> OnError;
        #endregion

        #region CONSTRUCTOR
        public DownloadRequest(string url, Action<byte[]> onSuccess, Action<string> onError)
        {
            Url = url;
            OnSuccess = onSuccess;
            OnError = onError;
        }
        #endregion
    }
}
