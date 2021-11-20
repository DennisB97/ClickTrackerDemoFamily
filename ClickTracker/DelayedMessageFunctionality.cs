// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Threading;
using System.Windows;
using System.Collections.Concurrent;


namespace ClickTracker
{
    /// <summary>
    /// DelayedMessageFunctionality takes care of initating messageboxes for user
    /// If no mainwindow is currently open it will queue the messages up.
    /// And will then on next mainwindow open dequeue the messages.
    /// </summary>
    public class DelayedMessageFunctionality
    {
        private readonly App app = null;

        ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

        bool bProcessMessages = false;


        public DelayedMessageFunctionality(App inApp)
        {
            app = inApp;

            app.MainWindowCreatedEvent += new App.MainWindowEventHandler(OnWindowCreated);
            app.MainWindowDestroyedEvent += new App.MainWindowEventHandler(OnWindowDestroyed);

        }

        private void ProcessMessages()
        {
            while (bProcessMessages && messages != null && messages.Count > 0)
            {
                string foundMessage = "";
                if (messages.TryDequeue(out foundMessage))
                {
                    MessageBox.Show(foundMessage);
                }

            }

        }

        public void RequestMessage(string message)
        {
            if (app != null)
            {
                if (app.GetApplicationState() == App.EApplicationState.WindowClosed)
                {
                    messages.Enqueue(DateTime.Now.ToString() + "\n" + message);
                }
                else
                {
                    MessageBox.Show(message);
                }

            }

        }


        private void OnWindowCreated(object sender, MainWindow window)
        {
            bProcessMessages = true;
            ProcessMessages();
        }

        private void OnWindowDestroyed(object sender, MainWindow window)
        {
            bProcessMessages = false;
        }

    }
}
