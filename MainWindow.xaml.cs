using System.Text.RegularExpressions;
using System.Windows;
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
        HorizontalFovTextBox.Text = Video.HorizontalFieldOfView.ToString();
        VerticalFovTextBox.Text = Video.VerticalFieldOfView.ToString();
        PitchTextBox.Text = Video.Pitch.ToString();
        UpdateArgs();
        CreateScreenShot();
    }

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
        PitchTextBox.Text = Video.Pitch.ToString();
        UpdateArgs();
        CreateScreenShot();
    }

    private void AdjustYaw(int delta)
    {
        if (Video == null) return;
        Video.Yaw += delta;
        YawTextBox.Text = Video.Yaw.ToString();
        UpdateArgs();
        CreateScreenShot();
    }

    private void SetPitch(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (Video == null) return;
        Video.Pitch = int.TryParse(PitchTextBox.Text, out var value) ? value : Video.Pitch;
        UpdateArgs();
        CreateScreenShot();
    }

    private void SetYaw(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (Video == null) return;
        Video.Yaw = int.TryParse(YawTextBox.Text, out var value) ? value : Video.Yaw;
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
        SetFoV();
    }

    private void SetVerticalFoV(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        SetFoV();
    }

    private void SetFoV()
    {
        if (Video == null) return;
        Video.VerticalFieldOfView =
            int.TryParse(VerticalFovTextBox.Text, out var v) ? v : Video.VerticalFieldOfView;
        Video.HorizontalFieldOfView = int.TryParse(HorizontalFovTextBox.Text, out var h)
            ? h
            : Video.HorizontalFieldOfView;
        HorizontalFovTextBox.Text = Video.HorizontalFieldOfView.ToString();
        VerticalFovTextBox.Text = Video.VerticalFieldOfView.ToString();
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

}