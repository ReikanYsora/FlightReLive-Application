using FlightReLive.Core.FlightDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.TimeBar
{
    /// <summary>
    /// Central manager that controls the playback timeline of a flight.
    /// Decoupled from video, modules can subscribe to events here instead of the VideoPlayer.
    /// </summary>
    public class TimeBarManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private long _totalFrameCount;
        private int _lastPointIndex = -1;
        private FlightData _currentFlightData;
        private TimeSpan _firstTimeSpan;
        private TimeSpan _lastTimeSpan;
        private PlaybackSpeed _playbackSpeed = PlaybackSpeed.Normal;
        #endregion

        #region PROPERTIES
        internal static TimeBarManager Instance { get; private set; }

        internal bool IsPlaying { get; private set; }

        internal double CurrentTime { get; private set; }

        internal double Duration { get; private set; }

        internal double Frequency { get; private set; }

        internal long TotalFrameCount { get; private set; }

        internal long CurrentFrame => (long)Math.Clamp(Math.Round(CurrentTime * Frequency), 0, TotalFrameCount > 0 ? TotalFrameCount - 1 : 0);

        internal double Time => (_currentFlightData == null || Duration <= 0) ? 0f : CurrentTime;

        internal double Length => (_currentFlightData == null || Duration <= 0) ? 0f : Duration;

        internal PlaybackSpeed Speed
        {
            get
            {
                return _playbackSpeed;
            }
            set
            {
                _playbackSpeed = value;
            }
        }
        internal double SpeedFactor
        {
            get
            {
                switch (_playbackSpeed)
                {
                    case PlaybackSpeed.UltraSlow:
                        return 0.25;
                    case PlaybackSpeed.Slow:
                        return 0.5;
                    default:
                    case PlaybackSpeed.Normal:
                        return 1.0;
                    case PlaybackSpeed.Fast:
                        return 2.0;
                    case PlaybackSpeed.UltraFast:
                        return 4.0;
                }
            }
        }
        #endregion

            #region EVENTS
        internal event Action<float, int, FlightDataPoint> OnProgressChanged;
        internal event Action<FlightData> OnFlightDataLoaded;
        internal event Action OnFlightDataUnloaded;
        internal event Action OnPlay;
        internal event Action OnPause;
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (_currentFlightData == null || _currentFlightData.Points == null || _currentFlightData.Points.Count == 0)
            {
                return;
            }

            if (IsPlaying)
            {
                CurrentTime += UnityEngine.Time.deltaTime * SpeedFactor;
                CurrentTime = Math.Min(CurrentTime, Duration);
            }

            // synchro avec les points
            TimeSpan target = _firstTimeSpan + TimeSpan.FromSeconds(CurrentTime);
            target = TimeSpan.FromTicks(Math.Clamp(target.Ticks, _firstTimeSpan.Ticks, _lastTimeSpan.Ticks));

            int index = FindClosestPointIndex(_currentFlightData.Points, target);

            if (index != _lastPointIndex)
            {
                _lastPointIndex = index;
                float progress = (Duration > 0) ? (float)(CurrentTime / Duration) : 0f;
                FlightDataPoint point = _currentFlightData.Points[index];
                OnProgressChanged?.Invoke(progress, index, point);
            }
        }
        #endregion

        #region METHODS : Loading
        internal void Load(FlightData flightData)
        {
            if (flightData == null || flightData.Points == null || flightData.Points.Count == 0)
            {
                return;
            }

            _currentFlightData = flightData;
            _lastPointIndex = -1;
            _firstTimeSpan = flightData.Points.First().TimeSpan;
            _lastTimeSpan = flightData.Points.Last().TimeSpan;
            Duration = Math.Max(0, (_lastTimeSpan - _firstTimeSpan).TotalSeconds);
            Frequency = flightData.Frequency;
            _totalFrameCount = (Duration > 0 && Frequency > 0) ? (long)Math.Round(Duration * Frequency) : 0;
            TotalFrameCount = _totalFrameCount;
            CurrentTime = 0;
            OnFlightDataLoaded?.Invoke(flightData);
            Play();
        }

        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                _currentFlightData = null;
                CurrentTime = 0;
                Duration = 0;
                Frequency = 0;
                _lastPointIndex = -1;
                _totalFrameCount = 0;
                TotalFrameCount = 0;
                IsPlaying = false;
                OnFlightDataUnloaded?.Invoke();
            });
        }
        #endregion

        #region METHODS
        internal void BackwardStep()
        {
            Seek(0);
        }

        internal void BackwardPoint()
        {
            if (_currentFlightData == null || _lastPointIndex <= 0)
            {
                return;
            }

            int targetIndex = Mathf.Max(0, _lastPointIndex - 1);
            FlightDataPoint targetPoint = _currentFlightData.Points[targetIndex];
            Seek(targetPoint.TimeSpan.Subtract(_firstTimeSpan).TotalSeconds);
        }

        internal void Play()
        {
            IsPlaying = true;
            OnPlay?.Invoke();
        }

        internal void Pause()
        {
            IsPlaying = false;
            OnPause?.Invoke();
        }

        internal void ForwardPoint()
        {
            if (_currentFlightData == null || _lastPointIndex < 0)
            {
                return;
            }

            int targetIndex = Mathf.Min(_currentFlightData.Points.Count - 1, _lastPointIndex + 1);
            FlightDataPoint targetPoint = _currentFlightData.Points[targetIndex];
            Seek(targetPoint.TimeSpan.Subtract(_firstTimeSpan).TotalSeconds);
        }

        internal void ForwardStep()
        {
            Seek(Duration);
        }

        internal void Seek(double timeInSeconds)
        {
            if (_currentFlightData == null)
            {
                return;
            }

            CurrentTime = Math.Clamp(timeInSeconds, 0, Duration);
        }

        internal void SeekRatio(float ratio01)
        {
            ratio01 = Mathf.Clamp01(ratio01);
            Seek(Duration * ratio01);
        }

        internal void SetFrame(long frame, bool pause)
        {
            if (_currentFlightData == null || Frequency <= 0)
            {
                return;
            }

            frame = Math.Clamp(frame, 0, TotalFrameCount > 0 ? TotalFrameCount - 1 : 0);
            double targetTime = frame / Frequency;
            Seek(targetTime);

            if (pause)
            {
                IsPlaying = false;
                OnPause?.Invoke();
            }
        }

        internal void SeekFrame(long frame)
        {
            if (Frequency <= 0 || Duration <= 0)
            {
                return;
            }

            double targetTime = frame / Frequency;
            Seek(targetTime);
        }

        internal void ChangeSpeed()
        {
            int next = ((int)_playbackSpeed + 1) % Enum.GetValues(typeof(PlaybackSpeed)).Length;
            _playbackSpeed = (PlaybackSpeed)next;
        }

        private int FindClosestPointIndex(List<FlightDataPoint> points, TimeSpan currentSpan)
        {
            int low = 0;
            int high = points.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                TimeSpan midSpan = points[mid].TimeSpan;

                if (midSpan < currentSpan)
                {
                    low = mid + 1;
                }
                else if (midSpan > currentSpan)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            int before = Mathf.Clamp(low - 1, 0, points.Count - 1);
            int after = Mathf.Clamp(low, 0, points.Count - 1);

            TimeSpan diffBefore = (points[before].TimeSpan - currentSpan).Duration();
            TimeSpan diffAfter = (points[after].TimeSpan - currentSpan).Duration();

            return diffBefore <= diffAfter ? before : after;
        }
        #endregion
    }

    public enum PlaybackSpeed
    {
        UltraSlow = 0,
        Slow = 1,
        Normal = 2,
        Fast = 3,
        UltraFast = 4
    }
}
