using System;

namespace FlightReLive.Core.Version
{
    public class AppVersionDTO
    {
        #region PROPERTIES
        public string DisplayName { get; set; }

        public int Major { get; set; }

        public int Minor { get; set; }

        public int Build { get; set; }

        public bool IsStable { get; set; }

        public DateTime ReleaseDate { get; set; }

        public long FileSize { get; set; }

        public string FileSizeReadable { get; set; }

        public string Url { get; set; }

        public string OS { get; set; }

        public string Architecture { get; set; }
        #endregion

        #region METHODS
        public string GetFullVersion()
        {
            return IsStable ? $"{Major}.{Minor}.{Build}" : $"{Major}.{Minor}.{Build}b";
        }   

        #endregion
    }
}
