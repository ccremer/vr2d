using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
                
            try
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(fileName));
                DisplayImage.Source = bitmapImage;
                DisplayImage.Width = bitmapImage.Width;
                DisplayImage.Height = bitmapImage.Height;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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