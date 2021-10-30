﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace AquaLogic_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker _backgroundWorker;
        private SocketProcess _socketProcess;
        
        private readonly string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"AquaLogic.csv");
        private DateTime _lastLog = DateTime.Now;

        public MainWindow()
        {

            InitializeComponent();

            InitializeSocketProcess();

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _backgroundWorker.CancelAsync();
            Properties.Settings.Default.Save();
        }

        private void InitializeSocketProcess()
        {
            InitializeBackgroundWorker();

            _socketProcess = new(Properties.Settings.Default.ipAddr, Properties.Settings.Default.portNum);

            _backgroundWorker.RunWorkerAsync();
        }
        
 
        // UI Events

        private void Button_Click(object sender, RoutedEventArgs e)
        {
             Button button = (Button)sender;
            _socketProcess.QueueKey(button.Name);

        }
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            _backgroundWorker.CancelAsync();
            System.Threading.Thread.Sleep(200);
            _socketProcess.QueueKey("Reset");
            System.Threading.Thread.Sleep(200);
            InitializeSocketProcess();
        }

        private static void SetStatus(Button button, SocketProcess.States status, SocketProcess.States blink, SocketProcess.States state)
        {
            button.FontWeight = status.HasFlag(state) ? FontWeights.Bold : FontWeights.Regular;
            button.FontStyle = blink.HasFlag(state) ? FontStyles.Italic : FontStyles.Normal;
        }

        // Background Worker
        private void InitializeBackgroundWorker()
        {
            _backgroundWorker = new();
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
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_backgroundWorker.CancellationPending)
            {
                System.Threading.Thread.Sleep(50);
                SocketProcess.SocketData socketData = _socketProcess.Update();
                //DisplayText = "Connection Lost" + "\n" + "Restart Required";
                if (socketData.DisplayText != null)
                {
                    _backgroundWorker.ReportProgress(0, socketData);
                }
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(
           object sender, RunWorkerCompletedEventArgs e)
        {
        }

        // This event handler updates the progress bar.

        private void BackgroundWorker_ProgressChanged(object sender,
            ProgressChangedEventArgs e)
        {
            SocketProcess.SocketData socketData = (SocketProcess.SocketData)e.UserState;
            UpdateDisplay(socketData);
        }

        private void UpdateDisplay(SocketProcess.SocketData socketData)
        {
            if (socketData.DisplayText != null)
            {
                textDisplay.Text = socketData.DisplayText;
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
    }
}


