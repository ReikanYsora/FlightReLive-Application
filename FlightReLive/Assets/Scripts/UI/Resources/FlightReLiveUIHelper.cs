using System;
using Fu;
using Fu.Framework;

namespace FlightReLive.UI
{
    internal static class FlightReLiveUIHelper
    {
        internal static void ShowYesNoMessageBox(string title, string message, Action OnYes, Action OnNo)
        {
            Fugui.ShowModal(title, (layout) =>
            {
                layout.CenterNextItemHV(message);
                layout.Text(message);
            }, FuModalSize.Large, new FuModalButton("No", OnNo, FuButtonStyle.Danger, FuKeysCode.Escape), new FuModalButton("Yes", OnYes, FuButtonStyle.Highlight, FuKeysCode.Enter));
        }

        internal static void ShowOKCancelMessageBox(string title, string message, Action OnOK, Action OnCancel)
        {
            Fugui.ShowModal(title, (layout) =>
            {
                layout.CenterNextItemHV(message);
                layout.Text(message);
            }, FuModalSize.Large, new FuModalButton("Cancel", OnCancel, FuButtonStyle.Danger, FuKeysCode.Escape), new FuModalButton("OK", OnOK, FuButtonStyle.Highlight, FuKeysCode.Enter));
        }
    }
}
