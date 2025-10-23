using System;
using System.Runtime.InteropServices;

namespace FlightReLive.Core.Platform
{
    public static class MacOsFolderAccess
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        [DllImport("Unity_MacOsFolderAccess")]
        private static extern IntPtr ShowFolderPickerAndBookmark(string message);

        [DllImport("Unity_MacOsFolderAccess")]
        private static extern bool StartAccessWithBookmark(string bookmark);

        [DllImport("Unity_MacOsFolderAccess")]
        private static extern void StopAccess();
#endif

        internal static (string path, string bookmark) PickFolderWithBookmark(string message = "Select a folder")
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            IntPtr ptr = ShowFolderPickerAndBookmark(message);
            if (ptr == IntPtr.Zero)
            {
                return (null, null);
            }

            string combined = Marshal.PtrToStringAnsi(ptr);
            if (string.IsNullOrEmpty(combined))
            {
                return (null, null);
            }

            string[] parts = combined.Split(new[] { "||" }, StringSplitOptions.None);
            return parts.Length == 2 ? (parts[0], parts[1]) : (combined, null);
#else
            return (null, null);
#endif
        }

        internal static bool BeginAccess(string bookmark)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (string.IsNullOrEmpty(bookmark))
            {
                return false;
            }

            return StartAccessWithBookmark(bookmark);
#else
    return true;
#endif
        }

        internal static void EndAccess()
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            StopAccess();
#endif
        }
    }
}

