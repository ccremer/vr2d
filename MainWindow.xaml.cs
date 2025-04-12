using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace VR2D;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

        Task.Run(async () =>
            {
                Console.WriteLine($"Creating snapshot: {fileName}");
                var f = new Video(fileName);
                var image = await f.CreateScreenshot(TimeSpan.FromMinutes(2));

                Dispatcher.Invoke(() =>
                {
                    // Use MediaElement for GIF
                    DisplayMedia.Visibility = Visibility.Visible;
                    DisplayMedia.LoadedBehavior = MediaState.Manual;
                    DisplayMedia.Source = new Uri(image, UriKind.Relative);
                    DisplayMedia.Play();
                    //new FileInfo(task.Result).Delete();
                });
            }
        );
    }

// Handle MediaElement looping for GIFs
    private void DisplayMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        // Restart the media when it ends (to achieve looping)
        DisplayMedia.Position = new TimeSpan(0, 0, 0, 0, 1);
        DisplayMedia.Play();
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle up arrow button click
    }

    private void RightButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle right arrow button click
    }

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle down arrow button click
    }

    private void LeftButton_Click(object sender, RoutedEventArgs e)
    {
        // Handle left arrow button click
    }

    private void AngleTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null)
        {
            // Validate that the input is a numeric value
            if (!string.IsNullOrEmpty(textBox.Text) && !double.TryParse(textBox.Text, out _))
            {
                // If not numeric, revert to the previous value or clear
                textBox.Text = "";
            }
        }
    }
}