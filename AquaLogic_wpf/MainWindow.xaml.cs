using AquaLogic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Drawing;

namespace AquaLogic_wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            InitializeBackgroundWorker();

            App_Version.Content = Assembly.GetExecutingAssembly().GetName().Version.ToString();
         }

        // UI Events

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        string _key = "";
         private void Button_Click(object sender, RoutedEventArgs e)
        {
             Button button = (Button)sender;
            _key = button.Name;
        }

        string _ipAddr;
        int _portNum;
        bool _resetSocket;
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            _ipAddr = ipAddr.Text;
            _portNum = Int32.Parse(portNum.Text);
            _resetSocket = true;

            TabCon.SelectedIndex--;
        }

        // UI Updates

        private readonly string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AquaLogic.csv");
        private DateTime _lastLog = DateTime.Now;
        private void UpdateDisplay(SocketProcess.SocketData socketData)
        {
            try
            {
                if (socketData.DisplayText != null)
                {
                    TextDisplay.Text = socketData.DisplayText;
                }

                if (socketData.Status != 0)
                {
                    SetStatus(Pool, socketData.Status, socketData.Blink, SocketProcess.States.POOL);
                    SetStatus(Spa, socketData.Status, socketData.Blink, SocketProcess.States.SPA);
                    SetStatus(Spillover, socketData.Status, socketData.Blink, SocketProcess.States.SPILLOVER);
                    SetStatus(Filter, socketData.Status, socketData.Blink, SocketProcess.States.FILTER);
                    SetStatus(Lights, socketData.Status, socketData.Blink, SocketProcess.States.LIGHTS);
                    SetStatus(Heater1, socketData.Status, socketData.Blink, SocketProcess.States.HEATER_1);
                    SetStatus(Valve3, socketData.Status, socketData.Blink, SocketProcess.States.VALVE_3);
                    SetStatus(Valve4, socketData.Status, socketData.Blink, SocketProcess.States.VALVE_4);
                    SetStatus(Aux1, socketData.Status, socketData.Blink, SocketProcess.States.AUX_1);
                    SetStatus(Aux2, socketData.Status, socketData.Blink, SocketProcess.States.AUX_2);
                    SetStatus(Aux3, socketData.Status, socketData.Blink, SocketProcess.States.AUX_3);
                    SetStatus(Aux4, socketData.Status, socketData.Blink, SocketProcess.States.AUX_4);
                    SetStatus(Aux5, socketData.Status, socketData.Blink, SocketProcess.States.AUX_5);
                    SetStatus(Aux6, socketData.Status, socketData.Blink, SocketProcess.States.AUX_6);
                }

                if (socketData.LogText != null && Properties.Settings.Default.LogInt > 0 && DateTime.Now >= _lastLog.AddMinutes(Properties.Settings.Default.LogInt))
                {
                    _lastLog = DateTime.Now;
                    SocketProcess.WriteTextFile(_logPath, socketData.LogText);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }
        private static void SetStatus(Button button, SocketProcess.States status, SocketProcess.States blink, SocketProcess.States state)
        {
            button.FontWeight = status.HasFlag(state) ? FontWeights.Bold : FontWeights.Regular;
            button.FontStyle = blink.HasFlag(state) ? FontStyles.Italic : FontStyles.Normal;
        }

        // BackgroundWorker

        readonly BackgroundWorker _backgroundWorker = new();
        private void InitializeBackgroundWorker()
        {
            TextDisplay.Text = "Connecting...";

            _ipAddr = ipAddr.Text;
            _portNum = Int32.Parse(portNum.Text);

            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork +=
                new DoWorkEventHandler(BackgroundWorker_DoWork);
            _backgroundWorker.RunWorkerCompleted +=
                    new RunWorkerCompletedEventHandler(
                BackgroundWorker_RunWorkerCompleted);
            _backgroundWorker.ProgressChanged +=
                    new ProgressChangedEventHandler(
                BackgroundWorker_ProgressChanged);
            _backgroundWorker.RunWorkerAsync();
         }

       private void BackgroundWorker_DoWork(object sender,
            DoWorkEventArgs e)
        {
            int vCnt = 0;
            SocketProcess socketProcess = new(_ipAddr, _portNum);
            Thread.Sleep(200);
            while (true)
            {
                if (_key != "")
                {
                    socketProcess.QueueKey(_key);
                    _key = "";
                }
                else
                {
                    Thread.Sleep(100);
                    SocketProcess.SocketData socketData = socketProcess.Update();

                    if (socketData.Valid)
                    {
                        vCnt = 0;
                        _backgroundWorker.ReportProgress(vCnt, socketData);
                    }
                    else if (vCnt == 50)
                    {
                        _backgroundWorker.ReportProgress(vCnt, socketData);
                    }
                    else if (vCnt == 300 || !socketProcess.Connected)
                    {
                        vCnt = 0;
                        socketProcess.Reset(_ipAddr, _portNum);
                        Thread.Sleep(200);
                    }
                    else if (_resetSocket)
                    {
                        _resetSocket = false;
                        socketProcess.QueueKey("Reset");
                        Thread.Sleep(200);
                        socketProcess.Reset(_ipAddr, _portNum);
                        Thread.Sleep(200);
                    }
                    vCnt++;
                }
            }
        }
        private void BackgroundWorker_ProgressChanged(object sender,
           ProgressChangedEventArgs e)
        {
            SocketProcess.SocketData socketData = (SocketProcess.SocketData)e.UserState;
            if (socketData.Valid)
            {
                TextDisplay.FontStyle = FontStyles.Normal;
                UpdateDisplay(socketData);
            }
            else
            {
                TextDisplay.FontStyle = FontStyles.Italic;
            }
        }
        private void BackgroundWorker_RunWorkerCompleted(
        object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}


