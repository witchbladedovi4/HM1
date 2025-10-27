using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HM1
{
    public partial class CmdWindow : Window
    {
        private Process cmdProcess;
        private StreamWriter inputWriter;
        private CancellationTokenSource cancellationTokenSource;
        private StringBuilder outputBuffer;

        public CmdWindow()
        {
            InitializeComponent();
            outputBuffer = new StringBuilder();
        }

        // Обработчик запуска CMD
        private async void BtnStartCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtStatus.Text = "Запуск командной строки...";
                btnStartCmd.IsEnabled = false;

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = txtWorkingDirectory.Text.Trim()
                };

                cmdProcess = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true
                };

                cmdProcess.Exited += CmdProcess_Exited;
                cmdProcess.OutputDataReceived += CmdProcess_OutputDataReceived;
                cmdProcess.ErrorDataReceived += CmdProcess_ErrorDataReceived;

                if (cmdProcess.Start())
                {
                    inputWriter = cmdProcess.StandardInput;
                    cmdProcess.BeginOutputReadLine();
                    cmdProcess.BeginErrorReadLine();

                    cancellationTokenSource = new CancellationTokenSource();

                    txtStatus.Text = "Командная строка запущена";
                    btnSendCommand.IsEnabled = true;
                    btnStopCmd.IsEnabled = true;

                    await ClearOutputAsync();
                    await SendCommandAsync("dir");
                }
                else
                {
                    throw new Exception("Не удалось запустить cmd.exe");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"Ошибка запуска: {ex.Message}");
                txtStatus.Text = "Ошибка запуска";
                btnStartCmd.IsEnabled = true;
            }
        }

        private async void BtnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = txtCommand.Text.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                await SendCommandAsync(command);
            }
        }

        private void BtnStopCmd_Click(object sender, RoutedEventArgs e)
        {
            StopCmdProcess();
        }

        private void TxtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnSendCommand.IsEnabled)
            {
                BtnSendCommand_Click(sender, e);
            }
        }



        private async Task SendCommandAsync(string command)
        {
            if (cmdProcess == null || cmdProcess.HasExited)
            {
                AppendOutput("CMD процесс не запущен или завершен");
                return;
            }

            try
            {
                AppendOutput($"> {command}");
                await inputWriter.WriteLineAsync(command);
                txtStatus.Text = $"Команда отправлена: {command}";
            }
            catch (Exception ex)
            {
                AppendOutput($"Ошибка отправки команды: {ex.Message}");
            }
        }

        private void CmdProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendOutput(e.Data);
            }
        }

        private void CmdProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendOutput($"[ОШИБКА] {e.Data}");
            }
        }

        private void CmdProcess_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AppendOutput("\n--- Процесс CMD завершен ---");
                txtStatus.Text = "Процесс CMD завершен";
                btnStartCmd.IsEnabled = true;
                btnSendCommand.IsEnabled = false;
                btnStopCmd.IsEnabled = false;
            });
        }

        private void AppendOutput(string text)
        {
            Dispatcher.Invoke(() =>
            {
                outputBuffer.AppendLine(text);
                if (outputBuffer.Length > 10000)
                {
                    outputBuffer.Remove(0, outputBuffer.Length - 8000);
                }
                txtOutput.Text = outputBuffer.ToString();
                txtOutput.ScrollToEnd();
            });
        }

        private async Task ClearOutputAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                outputBuffer.Clear();
                txtOutput.Text = string.Empty;
            });
        }

        private void StopCmdProcess()
        {
            try
            {
                if (cmdProcess != null && !cmdProcess.HasExited)
                {
                    cancellationTokenSource?.Cancel();
                    inputWriter?.WriteLine("exit");
                    if (!cmdProcess.WaitForExit(2000))
                    {
                        cmdProcess.Kill();
                    }
                }

                inputWriter?.Close();
                cmdProcess?.Close();

                txtStatus.Text = "CMD остановлен";
                btnStartCmd.IsEnabled = true;
                btnSendCommand.IsEnabled = false;
                btnStopCmd.IsEnabled = false;
            }
            catch (Exception ex)
            {
                AppendOutput($"Ошибка остановки: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            StopCmdProcess();
            base.OnClosed(e);
        }

        private void backMain_Click(object sender, RoutedEventArgs e)
        {
            var mainMenu = new MainWindow();
            mainMenu.Show();
            this.Close();
        }
    }
}