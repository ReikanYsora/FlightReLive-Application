using FlightReLive.Core.Workspace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Share
{
    //API DTOs (client-side)
    [Serializable]
    internal class FlightDataPointUpload
    {
        public DateTime Time { get; set; }
        public long TimeSpanTicks { get; set; }
        public float? Aperture { get; set; }
        public float? ShutterSpeed { get; set; }
        public int? ISO { get; set; }
        public float? Exposure { get; set; }
        public float? DigitalZoom { get; set; }
        public float? FocalLength { get; set; }
        public string ColorMode { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Distance { get; set; }
        public double RelativeAltitude { get; set; }
        public double AbsoluteAltitude { get; set; }
        public double HorizontalSpeed { get; set; }
        public double VerticalSpeed { get; set; }
    }

    [Serializable]
    internal class FlightFileUpload
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Frequency { get; set; }
        public TimeSpan Duration { get; set; } 
        public DateTime CreationDate { get; set; }
        public byte[] MapData { get; set; }
        public byte[] ThumbnailData { get; set; }
        public double? TakeOffLatitude { get; set; }
        public double? TakeOffLongitude { get; set; }
        public float? FlightGPSX { get; set; }
        public float? FlightGPSY { get; set; }
        public bool HasExtractionError { get; set; }
        public bool HasTakeOffPosition { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessagesJson { get; set; }
        public List<FlightDataPointUpload> DataPoints { get; set; } = new();
    }

    [Serializable]
    internal class FlightFileDownloadResponse : FlightFileUpload
    {
        public string ShareHash { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    public static class FlightShareService
    {
        #region CONSTANTS
        private const string BASE_API_URL = "https://flightrelive-api-cqb8dsgtb6c6ebaq.canadacentral-01.azurewebsites.net/api/flight";
        private const int DEFAULT_DAYS_TO_LIVE = 7;
        private const int MIN_DAYS = 1;
        private const int MAX_DAYS = 365;
        #endregion

        #region ATTRIBUTES
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        #endregion

        #region PUBLIC API
        public static Task<string> ShareFlightFileAsync(FlightFile flightFile)
            => ShareFlightFileAsync(flightFile, DEFAULT_DAYS_TO_LIVE);

        public static async Task<string> ShareFlightFileAsync(FlightFile flightFile, int daysToLive)
        {
            if (flightFile == null)
            {
                Debug.LogWarning("[FlightShareService] Cannot share null FlightFile.");
                return null;
            }

            try
            {
                flightFile.EncodeTextures();

                // Mapping Unity FlightFile -> DTO d’upload compatible API
                var upload = ToUploadRequest(flightFile);

                int days = Mathf.Clamp(daysToLive, MIN_DAYS, MAX_DAYS);
                string json = JsonConvert.SerializeObject(upload, _jsonSettings);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = $"{BASE_API_URL}/share?daysValid={days}";

                using HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string shareHash = await response.Content.ReadAsStringAsync();
                return shareHash.Trim('"');
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightShareService] ShareFlightFileAsync failed: {ex.GetBaseException().Message}");
                return null;
            }
        }

        public static async Task<FlightFile> GetFlightFileAsync(string shareHash)
        {
            if (string.IsNullOrWhiteSpace(shareHash))
            {
                Debug.LogWarning("[FlightShareService] Empty share hash.");
                return null;
            }

            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync($"{BASE_API_URL}/{Uri.EscapeDataString(shareHash)}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Debug.LogWarning($"[FlightShareService] FlightFile not found for hash: {shareHash}");
                    return null;
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                var dto = JsonConvert.DeserializeObject<FlightFileDownloadResponse>(json, _jsonSettings);
                if (dto == null) return null;

                var file = FromDownloadResponse(dto);
                file.DecodeTextures();

                return file;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightShareService] GetFlightFileAsync failed: {ex.GetBaseException().Message}");
                return null;
            }
        }
        #endregion

        #region MAPPING
        private static FlightFileUpload ToUploadRequest(FlightFile f)
        {
            return new FlightFileUpload
            {
                Name = f.Name,
                Width = f.Width,
                Height = f.Height,
                Frequency = f.Frequency,
                Duration = f.Duration, // ✅ envoie un TimeSpan directement
                CreationDate = f.CreationDate,
                MapData = f.MapData,
                ThumbnailData = f.ThumbnailData,
                TakeOffLatitude = f.EstimateTakeOffPosition?.Latitude,
                TakeOffLongitude = f.EstimateTakeOffPosition?.Longitude,
                FlightGPSX = f.FlightGPSCoordinates?.x,
                FlightGPSY = f.FlightGPSCoordinates?.y,
                HasExtractionError = f.HasExtractionError,
                HasTakeOffPosition = f.HasTakeOffPosition,
                IsValid = f.IsValid,
                ErrorMessagesJson = f.ErrorMessages != null ? JsonConvert.SerializeObject(f.ErrorMessages) : null, // ✅ serialize la liste
                DataPoints = f.DataPoints?.ConvertAll(p => new FlightDataPointUpload
                {
                    Time = p.Time,
                    TimeSpanTicks = p.TimeSpan.Ticks,
                    Aperture = p.CameraSettings?.Aperture,
                    ShutterSpeed = p.CameraSettings?.ShutterSpeed,
                    ISO = p.CameraSettings?.ISO,
                    Exposure = p.CameraSettings?.Exposure,
                    DigitalZoom = p.CameraSettings?.DigitalZoom,
                    FocalLength = p.CameraSettings?.FocalLength,
                    ColorMode = p.CameraSettings?.ColorMode,
                    Longitude = p.Longitude,
                    Latitude = p.Latitude,
                    Distance = p.Distance,
                    RelativeAltitude = p.RelativeAltitude,
                    AbsoluteAltitude = p.AbsoluteAltitude,
                    HorizontalSpeed = p.HorizontalSpeed,
                    VerticalSpeed = p.VerticalSpeed
                }) ?? new List<FlightDataPointUpload>()
            };
        }

        private static FlightFile FromDownloadResponse(FlightFileDownloadResponse dto)
        {
            var file = new FlightFile
            {
                Name = dto.Name,
                Width = dto.Width,
                Height = dto.Height,
                Frequency = dto.Frequency,
                Duration = dto.Duration,
                CreationDate = dto.CreationDate,
                MapData = dto.MapData,
                ThumbnailData = dto.ThumbnailData,
                EstimateTakeOffPosition = (dto.TakeOffLatitude.HasValue && dto.TakeOffLongitude.HasValue)
                    ? new FlightDefinition.FlightGPSData(dto.TakeOffLatitude.Value, dto.TakeOffLongitude.Value)
                    : null,
                FlightGPSCoordinates = (dto.FlightGPSX.HasValue && dto.FlightGPSY.HasValue)
                    ? new FFmpeg.SerializableVector2(new Vector2(dto.FlightGPSX.Value, dto.FlightGPSY.Value))
                    : null,
                HasExtractionError = dto.HasExtractionError,
                HasTakeOffPosition = dto.HasTakeOffPosition,
                IsValid = dto.IsValid,
                ErrorMessages = !string.IsNullOrEmpty(dto.ErrorMessagesJson)
                    ? JsonConvert.DeserializeObject<List<string>>(dto.ErrorMessagesJson)
                    : new List<string>(),
                DataPoints = new List<FlightDefinition.FlightDataPoint>()
            };

            if (dto.DataPoints != null)
            {
                foreach (var p in dto.DataPoints)
                {
                    file.DataPoints.Add(new FlightDefinition.FlightDataPoint
                    {
                        Time = p.Time,
                        TimeSpan = TimeSpan.FromTicks(p.TimeSpanTicks),
                        CameraSettings = new FlightDefinition.FlightDataPointCameraSettings
                        {
                            Aperture = p.Aperture ?? 0f,
                            ShutterSpeed = p.ShutterSpeed ?? 0f,
                            ISO = p.ISO ?? 0,
                            Exposure = p.Exposure ?? 0f,
                            DigitalZoom = p.DigitalZoom ?? 0f,
                            FocalLength = p.FocalLength ?? 0f,
                            ColorMode = p.ColorMode
                        },
                        Longitude = p.Longitude,
                        Latitude = p.Latitude,
                        Distance = p.Distance,
                        RelativeAltitude = p.RelativeAltitude,
                        AbsoluteAltitude = p.AbsoluteAltitude,
                        HorizontalSpeed = p.HorizontalSpeed,
                        VerticalSpeed = p.VerticalSpeed
                    });
                }
            }

            return file;
        }
        #endregion
    }
}
