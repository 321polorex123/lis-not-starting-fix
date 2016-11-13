using OpenSupportEngine.Helpers.FileSystem;
using System.Windows;

namespace lis_not_starting_fix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RunnerAssistant runnerAssistant;

        public MainWindow()
        {
            InitializeComponent();

            TemporaryFolder.FolderPrefix = "lis_not_starting_fix";
            TemporaryFolder.CleanUp();

            runnerAssistant = new RunnerAssistant();

            PrgBar.Minimum = 0;
            PrgBar.Maximum = runnerAssistant.TaskCount;
            PrgBar.Value = 0;

            runnerAssistant.NextInstallationReached += (s, e) =>
            {
                PrgBar.Value = e.TaskCount;
                CurrentInstallationLbl.Content = e.UiString;
            };
            runnerAssistant.InstallationFinished += (s, e) =>
            {
                if (e.Successful)
                    ShowMsgBox("Successfully finished.");
                else
                    ShowMsgBox("Process failed, there was a problem.");
            };
        }

        private void ShowMsgBox(string msg)
        {
            MessageBox.Show(msg, Title);
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            StartBtn.IsEnabled = false;
            runnerAssistant.Start();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var msg =
                "The author(s) of this program is/are not responsible for any damage that my occur using this program.\n\n" +
                "This programs uses icons from the Icomoon Icon Pack Free Version licenesed under GPL / CC BY 3.0.\n" +
                "https://icomoon.io/\n" +
                "http://www.gnu.org/licenses/gpl.html\n";
            ShowMsgBox(msg);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (runnerAssistant.IsRunning)
            {
                ShowMsgBox("The process is running, please wait until it is finished.");
                e.Cancel = true;
            }
            else
            {
                runnerAssistant.RemoveTempFolder();
            }
        }
    }
}
