using System;
using System.Windows;

namespace PortDetective.Utilities
{
    /// <summary>
    /// Safe clipboard wrapper — swallows OpenClipboard errors common on Windows.
    /// </summary>
    public static class ClipboardHelper
    {
        public static void SetText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                Clipboard.SetDataObject(text, copy: true);
            }
            catch (Exception)
            {
                // Clipboard can fail if another application holds it; retry once
                try { Clipboard.SetText(text); }
                catch { /* Best effort */ }
            }
        }
    }
}
