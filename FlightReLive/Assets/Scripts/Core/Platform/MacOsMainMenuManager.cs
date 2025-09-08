using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlightReLive.Core.Platform
{
    public static class MacOsMainMenuManager
    {
        public delegate void MenuCallbackDelegate();

        private static readonly List<MenuCallbackDelegate> _callbackRefs = new();

        [DllImport("Unity-MacOsNativeMenu")]
        private static extern void AddMacMenuEntry(
            string menuTitle,
            string itemTitle,
            string keyEquivalent,
            uint modifierMask,
            bool isSeparator,
            MenuCallbackDelegate callback);

        [DllImport("Unity-MacOsNativeMenu")]
        private static extern void ResetMacMenu();

        [DllImport("Unity-MacOsNativeMenu")]
        private static extern void AddQuitMenuItem(string appName, MenuCallbackDelegate callback);

        public static void AddQuitMenuEntry(string appName, Action callback)
        {
            MenuCallbackDelegate del = () => callback?.Invoke();
            _callbackRefs.Add(del);
            AddQuitMenuItem(appName, del);
        }


#if UNITY_EDITOR
        static MacOsMainMenuManager()
        {
#if UNITY_STANDALONE_OSX
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            InitializeOnReload();
#endif
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnReload()
        {
            if (EditorApplication.isPlaying)
            {
                ResetMenu();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ResetMenu();
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }
#endif

        public static void AddMenuEntry(string menuTitle, string itemTitle, Action callback,
                                        string keyEquivalent = "",
                                        uint modifierMask = 0)
        {
            MenuCallbackDelegate del = () => callback?.Invoke();
            _callbackRefs.Add(del);
            AddMacMenuEntry(menuTitle, itemTitle, keyEquivalent, modifierMask, false, del);
        }

        public static void AddSeparator(string menuTitle)
        {
            AddMacMenuEntry(menuTitle, "", "", 0, true, null);
        }

        public static void ResetMenu()
        {
            _callbackRefs.Clear();
            ResetMacMenu();
        }
    }

    public static class MacKeyModifiers
    {
        public const uint Command = 1u << 20;
        public const uint Option = 1u << 19;
        public const uint Control = 1u << 18;
        public const uint Shift = 1u << 17;
    }
}
