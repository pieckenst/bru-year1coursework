using Microsoft.Maui.Controls;
using TicketSalesAPP.Mobile.UI.MAUI.ViewModels;
using ZXing;
using ZXing.Net.Maui;

namespace TicketSalesAPP.Mobile.UI.MAUI.Views
{
    public partial class AuthPage : ContentPage
    {
        private readonly AuthViewModel _viewModel;
        private readonly IBarcodeReader _barcodeReader;

        public AuthPage()
        {
            InitializeComponent();
            _viewModel = new AuthViewModel();
            BindingContext = _viewModel;

            // Initialize barcode reader
            _barcodeReader = new BarcodeReader();
            _barcodeReader.Options.PossibleFormats = new[] { BarcodeFormat.QR_CODE };
            _barcodeReader.Options.TryHarder = true;

            // Start QR code scanning when QR login view becomes visible
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AuthViewModel.IsQRLoginVisible) && _viewModel.IsQRLoginVisible)
                {
                    StartQRScanning();
                }
            };
        }

        private async void StartQRScanning()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Error", "Camera permission is required for QR code scanning", "OK");
                    return;
                }

                var mediaFile = await MediaPicker.CapturePhotoAsync();
                if (mediaFile != null)
                {
                    using var stream = await mediaFile.OpenReadAsync();
                    var result = await _barcodeReader.DecodeAsync(stream);
                    if (result != null)
                    {
                        await _viewModel.HandleQRCodeResult(result.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back navigation during authentication
            return true;
        }
    }
}