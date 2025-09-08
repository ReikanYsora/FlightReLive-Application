using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Version
{
    public static class VersionService
    {
        #region CONSTANTS
        private const string BASE_API_URL = "https://flightrelive-api-cqb8dsgtb6c6ebaq.canadacentral-01.azurewebsites.net/api/version/latest";
        #endregion

        #region ATTRIBUTES
        private static readonly HttpClient _httpClient = new HttpClient();
        #endregion

        #region METHODS
        internal static async Task<AppVersionDTO> GetLatestVersionAsync()
        {
            string os = DetectOS();
            string arch = DetectArchitecture();
            os = NormalizeOS(os, arch);
            string query = $"?os={Uri.EscapeDataString(os)}&arch={Uri.EscapeDataString(arch)}";

            string fullUrl = BASE_API_URL + query;

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                AppVersionDTO version = JsonConvert.DeserializeObject<AppVersionDTO>(json);

                return version;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.GetBaseException().Message);
                return null;
            }
        }

        private static string NormalizeOS(string rawOS, string arch)
        {
            if (rawOS == "MacOS" && arch == "Apple Silicon")
            {
                return "MacOS (Apple Silicon)";
            }

            return rawOS;
        }

        private static string DetectOS()
        {
            string osString = SystemInfo.operatingSystem.ToLower();

            if (osString.Contains("windows"))
            {
                return "Windows";
            }

            if (osString.Contains("mac os") || osString.Contains("macos"))
            {
                return "MacOS";
            }

            if (osString.Contains("linux"))
            {
                return "Linux";
            }

            return "Unknown";
        }

        private static string DetectArchitecture()
        {
            string arch = SystemInfo.processorType.ToLower();

            if (arch.Contains("apple"))
            {
                return "Apple Silicon";
            }

            if (arch.Contains("arm") || arch.Contains("aarch64"))
            {
                return "ARM64";
            }

            if (arch.Contains("intel") || arch.Contains("amd") || arch.Contains("x86") || arch.Contains("x64"))
            {
                return "x64";
            }

            return "Unknown";
        }

        #endregion
    }
}
