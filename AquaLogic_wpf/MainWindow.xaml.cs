using AquaLogic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Drawing;
using System.Net;

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
        protected void OnLostFocus_TextBox(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Name == "IPaddr")
            {
                if (IPAddress.TryParse(textBox.Text, out IPAddress ipAddress))
                {
                    _ipAddr = ipAddress.ToString();
                    Properties.Settings.Default.Save();
                }
                else { textBox.Text = _ipAddr; }
            }
            else if (textBox.Name == "PortNum")
            {
                if (int.TryParse(textBox.Text, out int num))
                {
                    _portNum = num;
                    Properties.Settings.Default.Save();
                }
                else { textBox.Text = _portNum.ToString(); }

            }
            else if (textBox.Name == "LogInt")
            {
                if (int.TryParse(textBox.Text, out int num))
                {
                    _logInt = num;
                    Properties.Settings.Default.Save();
                }
                else { textBox.Text = _logInt.ToString(); }

            }
            else
            {
                Properties.Settings.Default.Save();
            }
        }
        string _key = "";
        protected void Reset_Click(object sender, RoutedEventArgs e)
        {
            TabCon.SelectedIndex = 0;
            _key = "Reset";
        }
        protected void Button_Click(object sender, RoutedEventArgs e)
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

            _ipAddr = IPaddr.Text;
            _ = int.TryParse(PortNum.Text, out _portNum);

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
            SocketProcess socketProcess = new(_ipAddr, _portNum);
            Thread.Sleep(250);
            DateTime lTime = DateTime.Now;
            while (true)
            {
                if (!socketProcess.Connected || DateTime.Now.Subtract(lTime).Seconds > 5)
                {
                    socketProcess.Reset(_ipAddr, _portNum);
                    Thread.Sleep(250);
                    lTime = DateTime.Now;
                }
                else
                {
                    SocketProcess.SocketData socketData = socketProcess.Update();

                    if (socketData.HasData)
                    {
                        _backgroundWorker.ReportProgress(0, socketData);
                        lTime = DateTime.Now;
                    }

                    if (_key != "")
                    {
                        if (socketProcess.QueueKey(_key))
                        {
                            socketData.HasData = true;
                            socketData.DisplayText = "Please Wait...";
                            _backgroundWorker.ReportProgress(0, socketData);
                        }
                        else if (_key == "Reset")
                        {
                            socketData.HasData = true;
                            socketData.DisplayText = "Connection Reset...";
                            _backgroundWorker.ReportProgress(0, socketData);
                        }
                        _key = "";
                    }
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
                UpdateDisplay(socketData);
            }
        }
        private void BackgroundWorker_RunWorkerCompleted(
        object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}


