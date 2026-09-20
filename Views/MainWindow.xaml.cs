using System;
using System.Windows;
using System.Windows.Input;
using PortDetective.ViewModels;

namespace PortDetective.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // Wire up ViewModel events
            _viewModel.ShowMessage      += OnShowMessage;
            _viewModel.ConfirmRequested += OnConfirmRequested;
            _viewModel.OpenSettingsRequested += OnOpenSettings;

            Loaded += async (_, _) => await _viewModel.InitializeAsync();
        }

        private void PortInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _viewModel.FindProcessCommand.Execute(null);
        }

        private void OnShowMessage(object? sender, string message)
        {
            MessageBox.Show(this, message, "Port Detective", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnConfirmRequested(object? sender, (string Title, string Message, Action OnConfirm) args)
        {
            var result = MessageBox.Show(this, args.Message, args.Title,
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
                args.OnConfirm();
        }

        private void OnOpenSettings(object? sender, EventArgs e)
        {
            var settingsWindow = App.GetService<SettingsWindow>();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.ShowMessage      -= OnShowMessage;
            _viewModel.ConfirmRequested -= OnConfirmRequested;
            _viewModel.OpenSettingsRequested -= OnOpenSettings;
            base.OnClosed(e);
        }
    }
}
