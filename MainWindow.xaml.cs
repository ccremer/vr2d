using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace VR2D;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Video? Video { get; set; }
    private TimeSpan Position { get; set; } = TimeSpan.Zero;

    private bool _isProcessing;

    private TextBox? _lastUsedTextBox;

    private bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            UpdateControlsState();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        StopPreviewButton.IsEnabled = false;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Video files (*.mp4;*.wmv;*.mkv)|*.mp4;*.wmv;*.mkv|All files (*.*)|*.*",
            Title = "Select a video file"
        };

        if (openFileDialog.ShowDialog() != true) return;
        var fileName = openFileDialog.FileName;
        FileNameTextBox.Text = fileName;
        Video = new Video(fileName);
        HorizontalFovTextBox.Text = ToShortenedString(Video.HorizontalFieldOfView);
        VerticalFovTextBox.Text = ToShortenedString(Video.VerticalFieldOfView);
        CombinedFovTextBox.Text = ToShortenedString(Video.HorizontalFieldOfView);
        UpdateArgs();
        CreateScreenShot();
    }
    
    private String ToShortenedString(decimal value) => value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");

    private void CreateScreenShot()
    {
        if (Video == null) return;
        if (IsProcessing) return;
        PreviewButton.IsEnabled = true;
        Task.Run(async () =>
            {
                IsProcessing = true;
                try
                {
                    var image = await Video.CreateScreenshot(Position);

                    Dispatcher.Invoke(() =>
                    {
                        DisplayMedia.Source = null;
                        DisplayMedia.Source = new Uri(image, UriKind.Relative);
                        DisplayMedia.Play();
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        );
    }

    private void CreatePreview()
    {
        if (Video == null) return;
        if (IsProcessing) return;
        if (!int.TryParse(PreviewValueTextBox.Text, out var seconds)) return;
        if (seconds < 1)
        {
            seconds = 1;
            PreviewValueTextBox.Text = seconds.ToString();
        }

        Task.Run(async () =>
        {
            IsProcessing = true;
            try
            {
                var file = await Video.CreatePreview(Position, TimeSpan.FromSeconds(seconds));
                await Dispatcher.InvokeAsync(() =>
                {
                    DisplayMedia.Source = null;
                    DisplayMedia.Source = new Uri(file, UriKind.Relative);
                    PreviewButton.IsEnabled = false;
                    StopPreviewButton.IsEnabled = true;
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                IsProcessing = false;
            }
        });
    }

    private void Decrease10sButton_Click(object sender, RoutedEventArgs e) => AdjustTimestamp(-10);

    private void Decrease1sButton_Click(object sender, RoutedEventArgs e) => AdjustTimestamp(-1);

    private void Decrease01sButton_Click(object sender, RoutedEventArgs e) => AdjustTimestamp(-0.1);

    private void Increase01sButton_Click(object sender, RoutedEventArgs e) => AdjustTimestamp(0.1);

    private void Increase1sButton_Click(object sender, RoutedEventArgs e) => AdjustTimestamp(1);

    private void Increase10sButton_Click(object sender, RoutedEventArgs e) => AdjustTimestamp(10);

    private void EnterTimestamp()
    {
        if (!TimeSpan.TryParseExact(TimestampTextBox.Text, @"hh\:mm\:ss\.f", null, out var timestamp))
        {
            TimestampTextBox.Background = new SolidColorBrush(Colors.Red);
            return;
        }

        TimestampTextBox.Background = null;
        Position = timestamp;
        _lastUsedTextBox = TimestampTextBox;
        AdjustTimestamp(0);
    }

    private void TimestampTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        EnterTimestamp();
    }

    private void AdjustTimestamp(double secondsDelta)
    {
        Position += TimeSpan.FromSeconds(secondsDelta);
        if (Position.TotalSeconds < 0) Position = TimeSpan.Zero;
        ShowPosition();
        UpdateArgs();
        CreateScreenShot();
    }

    private void DisplayMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        // Restart the media when it ends (to achieve looping)
        DisplayMedia.Position = new TimeSpan(0, 0, 0, 0, 1);
        DisplayMedia.Play();
    }

    private void PitchUpButton_Click(object sender, RoutedEventArgs e) => AdjustPitch(1);

    private void YawRightButton_Click(object sender, RoutedEventArgs e) => AdjustYaw(1);

    private void YawLeftButton_Click(object sender, RoutedEventArgs e) => AdjustYaw(-1);
    private void PitchDownButton_Click(object sender, RoutedEventArgs e) => AdjustPitch(-1);

    private void AdjustPitch(int delta)
    {
        if (Video == null) return;
        Video.Pitch += delta;
        PitchTextBox.Text = Video.Pitch.ToString("N2");
        UpdateArgs();
        CreateScreenShot();
    }

    private void AdjustYaw(int delta)
    {
        if (Video == null) return;
        Video.Yaw += delta;
        YawTextBox.Text = Video.Yaw.ToString("N2");
        UpdateArgs();
        CreateScreenShot();
    }

    private void SetPitch(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (Video == null) return;
        Video.Pitch = decimal.TryParse(PitchTextBox.Text, out var value) ? value : Video.Pitch;
        _lastUsedTextBox = PitchTextBox;
        UpdateArgs();
        CreateScreenShot();
    }

    private void SetYaw(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (Video == null) return;
        Video.Yaw = decimal.TryParse(YawTextBox.Text, out var value) ? value : Video.Yaw;
        _lastUsedTextBox = YawTextBox;
        UpdateArgs();
        CreateScreenShot();
    }

    private void ShowPosition() => TimestampTextBox.Text = Position.ToString(@"hh\:mm\:ss\.f");

    private void UpdateControlsState()
    {
        Dispatcher.Invoke(() =>
        {
            var shouldEnable = !_isProcessing;

            Cursor = _isProcessing ? Cursors.Wait : Cursors.Arrow;
            Controls.IsEnabled = shouldEnable;
            _lastUsedTextBox?.Focus();
        });
    }

    // Validates that only numbers and decimal point are entered
    private void PositiveNumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex("[^0-9.]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void SetHorizontalFoV(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _lastUsedTextBox = HorizontalFovTextBox;
        SetFoV();
    }

    private void SetVerticalFoV(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _lastUsedTextBox = VerticalFovTextBox;
        SetFoV();
    }
    

    private void SetCombinedFoV(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _lastUsedTextBox = CombinedFovTextBox;
        HorizontalFovTextBox.Text = CombinedFovTextBox.Text;
        VerticalFovTextBox.Text = CombinedFovTextBox.Text;
        SetFoV();
    }

    private void SetFoV()
    {
        if (Video == null) return;
        Video.VerticalFieldOfView =
            decimal.TryParse(VerticalFovTextBox.Text, out var v) ? v : Video.VerticalFieldOfView;
        Video.HorizontalFieldOfView = decimal.TryParse(HorizontalFovTextBox.Text, out var h)
            ? h
            : Video.HorizontalFieldOfView;
        UpdateArgs();
        CreateScreenShot();
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        CreatePreview();
    }

    private void StopPreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        DisplayMedia.Stop();
        DisplayMedia.Source = null;
        PreviewButton.IsEnabled = true;
        StopPreviewButton.IsEnabled = false;
    }

    private void UpdateArgs()
    {
        CliArgsPreview.Text = Video?.GetArguments(Position) ?? "";
    }

    private void DisplayMedia_OnMediaOpened(object sender, RoutedEventArgs e) => DisplayMedia.Play();

    private void DisplayMedia_OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        Console.WriteLine($"Failed playback: {e.ErrorException.Message}: {e.ErrorException.StackTrace}");
    }

    private void CreateTransition(object sender, RoutedEventArgs e)
    {
        if (Video == null) return;
        if (IsProcessing) return;

        var pitch = decimal.TryParse(TransitionPitchTextBox.Text, out var p) ? p : Video.Pitch;
        var yaw = decimal.TryParse(TransitionYawTextBox.Text, out var y) ? y : Video.Yaw;
        var hFoV = decimal.TryParse(TransitionHFoVTextBox.Text, out var h) ? h : Video.HorizontalFieldOfView;
        var vFoV = decimal.TryParse(TransitionVFoVTextBox.Text, out var v) ? v : Video.VerticalFieldOfView;
        Task.Run(async () =>
        {
            IsProcessing = true;
            try
            {
                var fileName = await Video.CreateTransitionFrom(new Video(Video.Input)
                {
                    Pitch = pitch,
                    Yaw = yaw,
                    HorizontalFieldOfView = hFoV,
                    VerticalFieldOfView = vFoV,
                }, Position);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                IsProcessing = false;
            }
        });
    }
}