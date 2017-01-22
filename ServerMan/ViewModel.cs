using ServerMan.View.ViewModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace ServerMan
{
    public class ViewModel : INotifyPropertyChanged
    {
        public RelayCommand BrowseClickedCommand { get; private set; }
        public RelayCommand StartClickedCommand { get; private set; }
        public RelayCommand RestartClickedCommand { get; private set; }
        public RelayCommand StopClickedCommand { get; private set; }


        public string ServerOutput
        {
            get { return _serverOutput; }
            set { _serverOutput = value; NotifyPropertyChanged(); }
        }


        public event PropertyChangedEventHandler PropertyChanged;


        private bool _isRunning => _serverProcess != null && !_serverProcess.HasExited;

        private bool _validServerPathSelected => !string.IsNullOrWhiteSpace(_selectedServerExecutablepath) && File.Exists(_selectedServerExecutablepath);


        private string _serverOutput;
        private Process _serverProcess;
        private string _selectedServerExecutablepath;

        public ViewModel()
        {
            BrowseClickedCommand = new RelayCommand(BrowseClicked, (obj) => !_isRunning);
            StartClickedCommand = new RelayCommand(StartClicked, (obj) => !_isRunning && _validServerPathSelected);
            RestartClickedCommand = new RelayCommand(RestartClicked, (obj) => _isRunning);
            StopClickedCommand = new RelayCommand(StopClicked, (obj) => _isRunning);

            Application.Current.Exit += ApplicationExit;
        }

        private void BrowseClicked(object obj)
        {
            var openFileDialog = new OpenFileDialog()
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
            if (_isRunning) return;

            ServerOutput = "";

            var serverDirectory = Path.GetDirectoryName(_selectedServerExecutablepath);

            _serverProcess = new Process {
                StartInfo = new ProcessStartInfo(_selectedServerExecutablepath) {
                    WorkingDirectory = serverDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
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

        private void StopServer()
        {
            if (!_isRunning) return;
            
            _serverProcess.StandardInput.WriteLine("\x3");
            _serverProcess.StandardInput.Flush();
            _serverProcess.WaitForExit(1000);

            if (!_serverProcess.HasExited) _serverProcess.Kill();

            _serverProcess.Close();
            _serverProcess.Dispose();
            _serverProcess = null;

            ServerOutput += "Server stopped.";
        }

        private void StartClicked(object obj)
        {
            StartServer();
        }

        private void RestartClicked(object obj)
        {
            StopServer();
            StartServer();
        }

        private void StopClicked(object obj)
        {
            StopServer();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            StopServer();
        }
    }
}
