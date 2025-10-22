using FlightReLive.Core.Database;
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
        private const string BASE_API_URL = "https://api.flight-relive.org/api/flight";
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
        internal static async Task<FlightFileShareResponse> ShareFlightFileExAsync(SerializedFlightData flightFile)
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
                string json = JsonConvert.SerializeObject(upload, _jsonSettings);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = $"{BASE_API_URL}/share?daysValid={365}";

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

        public static async Task<SerializedFlightData> GetFlightFileAsync(string shareHashOrDisplay)
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

                FlightFileDownload dto = JsonConvert.DeserializeObject<FlightFileDownload>(body, _jsonSettings);
                if (dto == null)
                {
                    Debug.LogError("[FlightShareService] Deserialization returned null DTO.");
                    return null;
                }

                SerializedFlightData file = FromDownloadResponse(dto);
                file.DecodeTextures();
                return file;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightShareService] GetFlightFileAsync failed: {ex.GetBaseException().Message}");
                return null;
            }
        }

        private static FlightFileUpload ToUploadRequest(SerializedFlightData file)
        {
            List<FlightDataPointUpload> dataPoints = new List<FlightDataPointUpload>();

            foreach (SerializedFlightDataPoint point in file.DataPoints)
            {
                dataPoints.Add(new FlightDataPointUpload
                {
                    TimeUtc = ToUtcSafe(point.Time),
                    TimeSpanTicks = point.TimeSpan.Ticks,
                    Latitude = point.Coordinate.Latitude,
                    Longitude = point.Coordinate.Longitude,
                    Distance = point.Distance,
                    RelativeAltitude = point.RelativeAltitude,
                    AbsoluteAltitude = point.AbsoluteAltitude,
                    HorizontalSpeed = point.HorizontalSpeed,
                    VerticalSpeed = point.VerticalSpeed,
                    Aperture = point.Aperture,
                    ShutterSpeed = point.ShutterSpeed,
                    ISO = point.ISO,
                    Exposure = point.Exposure,
                    DigitalZoom = point.DigitalZoom,
                    FocalLength = point.FocalLength,
                    ColorMode = point.ColorMode
                });
            }

            return new FlightFileUpload
            {
                Name = file.Name,
                Width = file.Width,
                Height = file.Height,
                Frequency = file.Frequency,
                DurationTicks = file.Duration.Ticks,
                CreationDate = ToUtcSafe(file.CreationDate),
                ThumbnailData = file.ThumbnailData,
                TakeOffLatitude = file.EstimateTakeOffPosition?.Latitude,
                TakeOffLongitude = file.EstimateTakeOffPosition?.Longitude,
                TakeOffAltitude = file.TakeOffAltitude,
                FlightLatitude = file.FlightGPSCoordinates?.Latitude,
                FlightLongitude = file.FlightGPSCoordinates?.Longitude,
                HasTakeOffPosition = file.HasTakeOffPosition,
                DataPoints = dataPoints
            };
        }

        private static SerializedFlightData FromDownloadResponse(FlightFileDownload dto)
        {
            SerializedFlightData file = new SerializedFlightData
            {
                Origin = FlightDataOrigin.SharedHash,
                Name = dto.Name,
                Width = dto.Width,
                Height = dto.Height,
                Frequency = dto.Frequency,
                Duration = TimeSpan.FromTicks(dto.DurationTicks),
                CreationDate = dto.CreationDateUtc.ToUniversalTime(),
                ThumbnailData = dto.ThumbnailData,
                EstimateTakeOffPosition = (dto.TakeOffLatitude.HasValue && dto.TakeOffLongitude.HasValue)
                    ? new SerializedGPSCoordinate(dto.TakeOffLatitude.Value, dto.TakeOffLongitude.Value)
                    : null,
                TakeOffAltitude = dto.TakeOffAltitude,
                FlightGPSCoordinates = (dto.FlightLatitude.HasValue && dto.FlightLongitude.HasValue)
                    ? new SerializedGPSCoordinate(dto.FlightLatitude.Value, dto.FlightLongitude.Value)
                    : null,
                HasTakeOffPosition = dto.HasTakeOffPosition,
                ShareHash = dto.ShareHash
            };
            file.ComputeUniqueKey();

            foreach (var p in dto.DataPoints)
            {
                file.DataPoints.Add(new SerializedFlightDataPoint
                {
                    Time = p.TimeUtc,
                    TimeSpan = TimeSpan.FromTicks(p.TimeSpanTicks),
                    Coordinate = new SerializedGPSCoordinate(p.Latitude, p.Longitude),
                    Distance = p.Distance,
                    RelativeAltitude = p.RelativeAltitude,
                    AbsoluteAltitude = p.AbsoluteAltitude,
                    HorizontalSpeed = p.HorizontalSpeed,
                    VerticalSpeed = p.VerticalSpeed,
                    Aperture = p.Aperture,
                    ShutterSpeed = p.ShutterSpeed,
                    ISO = p.ISO,
                    Exposure = p.Exposure,
                    DigitalZoom = p.DigitalZoom,
                    FocalLength = p.FocalLength,
                    ColorMode = p.ColorMode
                });
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
