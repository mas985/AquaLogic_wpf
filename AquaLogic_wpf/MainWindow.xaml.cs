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
            
            App_Version.Content = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            
            InitializeBackgroundWorker();
        }

        // UI Events

        string _ipAddr;
        int _portNum;
        int _logInt;
        bool _resetSocket;
        private void GetParms()
        {
            _ipAddr = ipAddr.Text;
            _ = int.TryParse(portNum.Text, out int pNum);
            if (pNum > 0) { _portNum = pNum; }
            portNum.Text = _portNum.ToString();
            _ = int.TryParse(LogInt.Text, out _logInt);
            LogInt.Text = _logInt.ToString();
        }
        protected void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (TabCon.SelectedIndex == 0)
            {
                GetParms();
                Properties.Settings.Default.Save();
            }
        }
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            TabCon.SelectedIndex--;
            _resetSocket = true;
        }
        string _key = "";
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            _key = button.Name;
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

                if (socketData.LogText != null && _logInt > 0 && DateTime.Now >= _lastLog.AddMinutes(_logInt))
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

            GetParms();

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
            bool holdKey = false;
            SocketProcess socketProcess = new(_ipAddr, _portNum);
            Thread.Sleep(250);
            while (true)
            {
                if (_key != "")
                {
                    holdKey = socketProcess.QueueKey(_key);
                    _key = "";
                }
                else
                {
                    SocketProcess.SocketData socketData = socketProcess.Update();

                    if (socketData.HasData)
                    {
                        vCnt = 0;
                        _backgroundWorker.ReportProgress(vCnt, socketData);
                    }
                    else if (vCnt == 100)
                    {
                        _backgroundWorker.ReportProgress(vCnt, socketData);
                    }
                     else if (_resetSocket || !socketProcess.Connected)
                    {
                        _resetSocket = false;
                        if (socketProcess.Connected)
                        {
                            socketProcess.QueueKey("Reset");
                            Thread.Sleep(250);
                        }
                        socketProcess.Reset(_ipAddr, _portNum);
                        Thread.Sleep(250);
                    }
                    else if (holdKey)
                    {
                        socketData.HasData = true;
                        socketData.DisplayText = "Please Wait...";
                        holdKey = false;
                        _backgroundWorker.ReportProgress(vCnt, socketData);
                    }
                    vCnt++;
                    Thread.Sleep(100);
                }
            }
        }
        private void BackgroundWorker_ProgressChanged(object sender,
           ProgressChangedEventArgs e)
        {
            SocketProcess.SocketData socketData = (SocketProcess.SocketData)e.UserState;
            if (socketData.HasData)
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


