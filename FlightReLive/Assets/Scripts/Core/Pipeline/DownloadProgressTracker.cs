using System;
using UnityEngine;

namespace FlightReLive.Core.Pipeline
{
    public class DownloadProgressTracker
    {
        #region ATTRIBUTES
        private int _totalSteps;
        private int _successfulSteps;
        private int _failedSteps;
        private bool _hasCompleted;
        #endregion

        #region PROPERTIES
        public int TotalTiles
        {
            get
            { 
                return _totalSteps;
            }
        }

        public int CompletedTiles
        {
            get
            { 
                return _successfulSteps;
            }
        }

        public int FailedSteps
        {
            get
            {
                return _failedSteps;
            }
        }

        public int Weight
        {
            get
            {
                return _totalSteps;
            }
        }

        public bool IsCancelled { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool HasCompleted
        {
            get 
            { 
                return _hasCompleted;
            }
        }

        public float CurrentProgress
        {
            get
            {
                return _totalSteps > 0 ? Mathf.Clamp01((float)(_successfulSteps + _failedSteps) / _totalSteps) : 0f;
            }
        }
        #endregion

        #region EVENTS
        public Action<float> OnProgressUpdated;
        public Action<string> OnError;
        public Action OnCompleted;
        #endregion

        #region METHODS

        public void Init(int total)
        {
            if (IsInitialized)
            {
                Debug.LogWarning("DownloadProgressTracker already initialized.");
                return;
            }

            _totalSteps = total;
            _successfulSteps = 0;
            _failedSteps = 0;
            IsCancelled = false;
            _hasCompleted = false;
            IsInitialized = true;

            OnProgressUpdated?.Invoke(CurrentProgress);
        }

        public void ReportSuccess()
        {
            if (!IsInitialized)
            {
                return;
            }

            _successfulSteps++;
            UpdateProgress();
        }

        public void ReportFailure(string error)
        {
            if (!IsInitialized)
            {
                return;
            }

            _failedSteps++;
            OnError?.Invoke(error);
            UpdateProgress();
        }

        public void Cancel()
        {
            IsCancelled = true;
        }

        public void MarkCompleted()
        {
            if (!_hasCompleted)
            {
                _hasCompleted = true;
                OnCompleted?.Invoke();
            }
        }

        private void UpdateProgress()
        {
            if (!IsInitialized || _hasCompleted)
            {
                return;
            }

            OnProgressUpdated?.Invoke(CurrentProgress);

            if (_successfulSteps + _failedSteps >= _totalSteps)
            {
                MarkCompleted();
            }
        }

        #endregion
    }
}
