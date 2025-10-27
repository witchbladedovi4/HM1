using System.Diagnostics;
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

namespace HM1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process notepadProcess;
        private CancellationTokenSource cancellationTokenSource;
        private int updateCounter = 0;
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                startButton.IsEnabled = false;
                txtStatus.Text = "Запуск статуса";

                notepadProcess = Process.Start("Notepad.exe");

                if (notepadProcess != null && !notepadProcess.HasExited)
                {
                    txtStatus.Text = "Запущен Noteppd";
                    await Task.Delay(3000);
                    if (!notepadProcess.HasExited)
                    {
                        notepadProcess.CloseMainWindow();
                        notepadProcess.Close();
                        txtStatus.Text = "Закрыт Notepad";
                    }
                }

                else
                {
                    txtStatus.Text = "Не удалось запустить NotePad";
                }    
            }
            catch (Exception ex) 
            {
                txtStatus.Text = ex.Message;
            }
            finally
            {
                startButton.IsEnabled = true;
            }
        }

        private async void UpdatetButton_Click (object sender, RoutedEventArgs e)
        {
            updateButton.IsEnabled = false;
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                txtStatus.Text = "Запуск фонового потока";

                await Task.Run(() => UpdateText(cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = "Обновление отменено";
            }
            catch (Exception ex)
            {
                txtStatus.Text = ex.Message;
            }
            finally
            {
                updateButton.IsEnabled = true;
            }
        }

        private void UpdateText(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 6; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                Thread.Sleep(600);

                Dispatcher.Invoke(() =>
                {
                    updateCounter += 1;
                    string newText = $"Обновление {updateCounter} из фонового потока\n" +
                        $"Время {DateTime.Now:T} " +
                        $"Поток ID {Thread.CurrentThread.ManagedThreadId} ";
                    txtContent.Text = newText + txtContent.Text;
                    txtStatus.Text = $"Обновлено {updateCounter} раз";
                });
                if (cancellationToken.IsCancellationRequested)
                    return;
            }

            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = "Обновление статуса заверщшено";
            });
        }

        private protected void OnClosed(EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();

            if (notepadProcess != null && !notepadProcess.HasExited)
            {
                notepadProcess.CloseMainWindow();
                notepadProcess.Close();
            }

            base.OnClosed(e);
        }

        private void cmdApp_Click(object sender, RoutedEventArgs e)
        {
            var cmdWindow = new CmdWindow();
            cmdWindow.Show();
            this.Close();
        }
    }
}