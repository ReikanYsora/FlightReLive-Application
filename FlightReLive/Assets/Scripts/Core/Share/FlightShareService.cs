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
    internal static class FlightShareService
    {
        #region CONSTANTS
        private const string BASE_API_URL = "https://flightrelive-api-cqb8dsgtb6c6ebaq.canadacentral-01.azurewebsites.net/api/flight";
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

        #region METHODS
        internal static async Task<FlightFileShareResponse> ShareFlightFileExAsync(FlightFile flightFile, int daysToLive)
        {
            if (flightFile == null)
            {
                Debug.LogWarning("[FlightShareService] Cannot share null FlightFile.");
                return null;
            }

            try
            {
                flightFile.EncodeTextures();

                FlightFileUpload upload = ToUploadRequest(flightFile);
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

                FlightFileShareResponse share = JsonConvert.DeserializeObject<FlightFileShareResponse>(body, _jsonSettings);
                return share;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightShareService] ShareFlightFileExAsync failed: {ex.GetBaseException().Message}");
                return null;
            }
        }

        public static async Task<FlightFile> GetFlightFileAsync(string shareHashOrDisplay)
        {
            string hash = NormalizeHash(shareHashOrDisplay);
            if (!IsValidShareHash(hash))
            {
                Debug.LogWarning($"[FlightShareService] Invalid share hash: '{shareHashOrDisplay}'");
                return null;
            }

            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync($"{BASE_API_URL}/{Uri.EscapeDataString(hash)}");
                string body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Debug.LogWarning($"[FlightShareService] FlightFile not found for hash: {hash}");
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[FlightShareService] Get failed: {(int)response.StatusCode} {response.ReasonPhrase}\nBody: {body}");
                    return null;
                }

                FlightFileDownloadResponse dto = JsonConvert.DeserializeObject<FlightFileDownloadResponse>(body, _jsonSettings);
                if (dto == null)
                {
                    Debug.LogError("[FlightShareService] Deserialization returned null DTO.");
                    return null;
                }

                FlightFile file = FromDownloadResponse(dto);
                file.DecodeTextures();
                return file;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightShareService] GetFlightFileAsync failed: {ex.GetBaseException().Message}");
                return null;
            }
        }

        private static FlightFileUpload ToUploadRequest(FlightFile f)
        {
            return new FlightFileUpload
            {
                Name = f.Name,
                Width = f.Width,
                Height = f.Height,
                Frequency = f.Frequency,
                DurationTicks = f.Duration.Ticks,
                CreationDate = ToUtcSafe(f.CreationDate),  // ✅ assure l’UTC

                MapData = f.MapData,
                ThumbnailData = f.ThumbnailData,

                TakeOffLatitude = f.EstimateTakeOffPosition?.Latitude,
                TakeOffLongitude = f.EstimateTakeOffPosition?.Longitude,
                FlightGPSX = f.FlightGPSCoordinates?.x,
                FlightGPSY = f.FlightGPSCoordinates?.y,
                HasExtractionError = f.HasExtractionError,
                HasTakeOffPosition = f.HasTakeOffPosition,
                IsValid = f.IsValid,
                ErrorMessagesJson = f.ErrorMessages != null ? JsonConvert.SerializeObject(f.ErrorMessages) : null,

                DataPoints = f.DataPoints?.ConvertAll(p => new FlightDataPointUpload
                {
                    Time = ToUtcSafe(p.Time),                 // ✅ on envoie en UTC
                    TimeSpanTicks = p.TimeSpan.Ticks,         // ✅ ticks

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
            FlightFile file = new FlightFile
            {
                Name = dto.Name,
                Width = dto.Width,
                Height = dto.Height,
                Frequency = dto.Frequency,

                Duration = TimeSpan.FromTicks(dto.DurationTicks),
                CreationDate = ToUtcSafe(dto.CreationDateUtc),

                MapData = dto.MapData,
                ThumbnailData = dto.ThumbnailData,

                EstimateTakeOffPosition = (dto.TakeOffLatitude.HasValue && dto.TakeOffLongitude.HasValue)
                    ? new FlightDefinition.FlightGPSData(dto.TakeOffLatitude.Value, dto.TakeOffLongitude.Value)
                    : null,

                FlightGPSCoordinates = (dto.FlightGPSX.HasValue && dto.FlightGPSY.HasValue)
                    ? new FFmpeg.SerializableVector2(new UnityEngine.Vector2(dto.FlightGPSX.Value, dto.FlightGPSY.Value))
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
                        Time = ToUtcSafe(p.TimeUtc),
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

        private static string NormalizeHash(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            input = input.Trim();

            if (input.StartsWith("#"))
            {
                input = input.Substring(1);
            }

            return input;
        }

        private static bool IsValidShareHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return false;
            }

            return System.Text.RegularExpressions.Regex.IsMatch(hash, @"^[A-Za-z0-9_-]{16}$");
        }

        private static DateTime ToUtcSafe(DateTime dt)
        {
            return dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
        }
        #endregion
    }
}
