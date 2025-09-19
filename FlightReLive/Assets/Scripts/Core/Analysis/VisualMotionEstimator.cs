using FlightReLive.Core.FlightDefinition;
using FlightReLive.UI.VideoPlayer;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

namespace FlightReLive.Core.Analysis
{
    public class VisualMotionEstimator : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] public Material _frameAnalyserMaterial;
        private RenderTexture _differenceRenderTexture;
        private int downscale = 32;
        private Texture2D _readBackTexture;
        private RenderTexture _previousFrame;
        private bool _isInitialized;
        private int _frameCounter = 0;
        public bool enable;
        #endregion

        #region PROPERTIES
        public static VisualMotionEstimator Instance { get; private set; }

        public Vector2 EstimatedMotion { get; private set; }
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

        private void Start()
        {
            VideoPlayerManager.Instance.OnVideoLoaded += OnVideoLoaded;
            VideoPlayerManager.Instance.OnVideoUnloaded += OnVideoUnloaded;
        }

        private void Update()
        {
            if (!enable)
            {
                return;
            }

            _frameCounter++;
            if (_frameCounter % 5 != 0)
            {
                return;
            }

            Texture tempTexture = VideoPlayerManager.Instance.Texture;

            if (VideoPlayerManager.Instance.Player == null || tempTexture == null)
            {
                return;
            }

            if (!_isInitialized)
            {
                Initialize(tempTexture);
            }
           
            //Blit current and previous frame into shader
            _frameAnalyserMaterial.SetTexture("_MainTexA", _previousFrame);
            _frameAnalyserMaterial.SetTexture("_MainTexB", tempTexture);
            Graphics.Blit(null, _differenceRenderTexture, _frameAnalyserMaterial);

            //Read back diffRT
            RenderTexture.active = _differenceRenderTexture;
            AsyncGPUReadback.Request(_differenceRenderTexture, 0, TextureFormat.RGB24, OnReadbackComplete);

            //Store current frame as previous
            Graphics.Blit(tempTexture, _previousFrame);
        }

        private void OnDestroy()
        {
            VideoPlayerManager.Instance.OnVideoLoaded -= OnVideoLoaded;
        }
        #endregion

        #region METHODS
        private void Initialize(Texture texture)
        {
            _previousFrame = new RenderTexture(texture.width, texture.height, 0);
            _differenceRenderTexture = new RenderTexture(texture.width / downscale, texture.height / downscale, 0, RenderTextureFormat.ARGB32);
            _differenceRenderTexture.Create();
            _readBackTexture = new Texture2D(_differenceRenderTexture.width, _differenceRenderTexture.height, TextureFormat.RGB24, false);
            _isInitialized = true;
        }

        private void Uninitialize()
        {
            if (_differenceRenderTexture != null)
            {
                _differenceRenderTexture.Release();
                _differenceRenderTexture = null;
            }

            _previousFrame = null;
            _readBackTexture = null;
            _isInitialized = false;
        }
        #endregion

        #region JOBS
        [BurstCompile]
        public struct MotionEstimationJob : IJob
        {
            [ReadOnly] public NativeArray<Color32> pixels;
            public int width;
            public int height;
            public NativeArray<Vector2> result;

            public void Execute()
            {
                Vector2 sum = Vector2.zero;

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 c = pixels[i];
                    float intensity = c.r / 255f * 0.299f + c.g / 255f * 0.587f + c.b / 255f * 0.114f;
                    Vector2 pos = new Vector2(i % width, i / width);
                    sum += pos * intensity;
                }

                Vector2 center = new Vector2(width, height) * 0.5f;
                Vector2 avg = sum / pixels.Length;
                result[0] = (avg - center).normalized;
            }
        }
        #endregion

        #region CALLBACKS
        void OnReadbackComplete(AsyncGPUReadbackRequest req)
        {
            if (req.hasError)
            {
                return;
            }

            NativeArray<Color32> data = req.GetData<Color32>();
            NativeArray<Vector2> result = new NativeArray<Vector2>(1, Allocator.TempJob);

            MotionEstimationJob job = new MotionEstimationJob
            {
                pixels = data,
                width = _differenceRenderTexture.width,
                height = _differenceRenderTexture.height,
                result = result
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            EstimatedMotion = result[0];
            result.Dispose();

            Debug.Log($"Motion (job): {EstimatedMotion}");
        }

        private void OnVideoUnloaded()
        {

        }

        private void OnVideoLoaded(FlightData flightData)
        {

        }
        #endregion
    }
}
