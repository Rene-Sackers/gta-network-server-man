using ServerMan.View.ViewModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using System.Linq;
using ServerMan.Unsafe;

namespace ServerMan
{
    public class ViewModel : INotifyPropertyChanged
    {
        public RelayCommand BrowseClickedCommand { get; private set; }
        public RelayCommand StartClickedCommand { get; private set; }
        public RelayCommand RestartClickedCommand { get; private set; }
        public RelayCommand StopClickedCommand { get; private set; }

        public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited && !_isShuttingDownServer;

        public string ServerOutput
        {
            get { return _serverOutput; }
            set { _serverOutput = value; NotifyPropertyChanged(); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool ValidServerPathSelected => !string.IsNullOrWhiteSpace(_selectedServerExecutablepath) && File.Exists(_selectedServerExecutablepath);


        private string _serverOutput;
        private Process _serverProcess;
        private string _selectedServerExecutablepath;
        private bool _isShuttingDownServer;

        public ViewModel()
        {
            var serverFileInDirectory = Directory.GetFiles(Directory.GetCurrentDirectory(), "GTANetworkServer.exe");

            _selectedServerExecutablepath = serverFileInDirectory.FirstOrDefault();

            BrowseClickedCommand = new RelayCommand(BrowseClicked, obj => !IsRunning);
            StartClickedCommand = new RelayCommand(StartClicked, obj => !IsRunning && ValidServerPathSelected);
            RestartClickedCommand = new RelayCommand(RestartClicked, obj => IsRunning);
            StopClickedCommand = new RelayCommand(StopClicked, obj => IsRunning);

            Application.Current.Exit += ApplicationExit;
        }

        private void BrowseClicked(object obj)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "GTANetworkServer.exe|GTANetworkServer.exe"
            };

            if (openFileDialog.ShowDialog() != true) return;

            var selectedFile = openFileDialog.FileName;

            if (!File.Exists(selectedFile))
            {
                MessageBox.Show("File invalid.");
                return;
            }

            _selectedServerExecutablepath = openFileDialog.FileName;
        }

        private void StartServer()
        {
            if (IsRunning) return;

            ServerOutput = "";

            var serverDirectory = Path.GetDirectoryName(_selectedServerExecutablepath);

            if (serverDirectory == null) return;

            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo(_selectedServerExecutablepath)
                {
                    WorkingDirectory = serverDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _serverProcess.Start();

            Task.Factory.StartNew(ServerOutputStreamReader);
        }

        private async void ServerOutputStreamReader()
        {
            while (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    var output = await _serverProcess.StandardOutput.ReadLineAsync();
                    ServerOutput += output + Environment.NewLine;
                }
                catch
                {
                    return;
                }
            }
        }

        private async Task StopServer()
        {
            if (!IsRunning || _isShuttingDownServer) return;

            _isShuttingDownServer = true;

            ServerOutput += "Sending shutdown event." + Environment.NewLine;

            await ShutDownConsoleProcess(_serverProcess);

            if (_serverProcess == null) return;
            if (!_serverProcess.HasExited) _serverProcess.Kill();

            _serverProcess.Close();
            _serverProcess.Dispose();
            _serverProcess = null;

            NotifyPropertyChanged(nameof(IsRunning));

            ServerOutput += "Server stopped." + Environment.NewLine;

            _isShuttingDownServer = false;
        }

        private static async Task ShutDownConsoleProcess(Process process)
        {
            if (ConsoleHandlingImports.AttachConsole((uint)process.Id))
            {
                ConsoleHandlingImports.SetConsoleCtrlHandler(null, true);
                try
                {
                    ConsoleHandlingImports.GenerateConsoleCtrlEvent(ConsoleHandlingImports.CTRL_C_EVENT, 0);
                    await Task.Factory.StartNew(process.WaitForExit);
                }
                finally
                {
                    ConsoleHandlingImports.FreeConsole();
                    ConsoleHandlingImports.SetConsoleCtrlHandler(null, false);
                }
            }
        }

        private void StartClicked(object obj)
        {
            StartServer();
        }

        private async void RestartClicked(object obj)
        {
            await StopServer();
            StartServer();
        }

        private async void StopClicked(object obj)
        {
            await StopServer();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void ApplicationExit(object sender, ExitEventArgs e)
        {
            await StopServer();
        }
    }
}
