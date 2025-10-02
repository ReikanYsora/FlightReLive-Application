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

                var upload = ToUploadRequest(flightFile);   // ⚠️ assure-toi que c’est bien la version avec DurationTicks
                int days = Mathf.Clamp(daysToLive, MIN_DAYS, MAX_DAYS);
                string json = JsonConvert.SerializeObject(upload, _jsonSettings);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = $"{BASE_API_URL}/share?daysValid={days}";

                using HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[FlightShareService] Share failed: {(int)response.StatusCode} {response.ReasonPhrase}\nBody: {body}");
                    return null;
                }

                var share = JsonConvert.DeserializeObject<FlightFileShareResponse>(body, _jsonSettings);
                return share?.ShareHash;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightShareService] ShareFlightFileAsync failed: {ex.GetBaseException().Message}");
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
                DurationTicks = f.Duration.Ticks,
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
                ErrorMessagesJson = f.ErrorMessages != null
                    ? JsonConvert.SerializeObject(f.ErrorMessages)
                    : null,
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
        #endregion
    }
}
