using PortDetective.Helpers;
using PortDetective.Models;

namespace PortDetective.ViewModels
{
    /// <summary>
    /// Wraps a QuickPort for display in the quick-access port buttons.
    /// </summary>
    public class QuickPortViewModel : ViewModelBase
    {
        private bool _isInUse;
        private bool _isChecking;

        public int Port        => Model.Port;
        public string Label    => Model.Label;
        public string Tooltip  => $"{Model.Port} — {Model.Description}";

        public bool IsInUse
        {
            get => _isInUse;
            set => SetProperty(ref _isInUse, value);
        }

        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }

        public QuickPort Model { get; }

        public QuickPortViewModel(QuickPort model)
        {
            Model = model;
        }
    }
}
