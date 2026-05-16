using System;
using System.IO;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using QRCoder;

namespace Frisson.App.ViewModels;

public partial class QrCodeWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _qrCodeImage;

    [ObservableProperty]
    private string _qrCodeContent = string.Empty;

    [ObservableProperty]
    private bool _isUrlVisible;

    public string DisplayUrl => IsUrlVisible ? QrCodeContent : "***";
    public string EyeIconValue => IsUrlVisible ? "fa-solid fa-eye-slash" : "fa-solid fa-eye";

    public QrCodeWindowViewModel(string content)
    {
        QrCodeContent = content;
        GenerateQrCode(content);
    }

    partial void OnIsUrlVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayUrl));
        OnPropertyChanged(nameof(EyeIconValue));
    }

    [RelayCommand]
    private void ToggleUrlVisibility()
    {
        IsUrlVisible = !IsUrlVisible;
    }

    private void GenerateQrCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = pngQrCode.GetGraphic(20);

        using var stream = new MemoryStream(pngBytes);
        QrCodeImage = new Bitmap(stream);
    }
}
