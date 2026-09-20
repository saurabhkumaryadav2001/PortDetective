using PortDetective.Models;

namespace PortDetective.Interfaces
{
    /// <summary>
    /// Loads and persists application settings.
    /// </summary>
    public interface ISettingsService
    {
        AppSettings Current { get; }
        void Save(AppSettings settings);
        void Load();

        /// <summary>Adds or removes the app from the Windows startup registry key.</summary>
        void SetStartWithWindows(bool enabled);
    }
}
