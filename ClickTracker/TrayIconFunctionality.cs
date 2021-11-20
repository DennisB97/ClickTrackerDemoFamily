// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Drawing;

namespace ClickTracker
{
    /// <summary>
    /// TrayIconFunctionality class handles the trayicon for clicktracker
    /// </summary>
    class TrayIconFunctionality : IDisposable
    {

        private readonly App app = null;
        private readonly ILogFunctionality logFunctionality = null;
        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private readonly Icon trayIcon = null;

        public TrayIconFunctionality(App inApp, ILogFunctionality inLogFunctionality)
        {
            // Create the windows.forms notifyicon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
           
            app = inApp;
            logFunctionality = inLogFunctionality;

            notifyIcon.Visible = true;
            notifyIcon.Text = "ClickTracker";

            trayIcon = ClickTracker.Properties.Resources.TrayIcon;
            notifyIcon.Icon = trayIcon;
          
            notifyIcon.DoubleClick += new EventHandler(OnNotifyIconDoubleClick);
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, OnIconExit);

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Tray icon was constructed", EClickerLogVerbosity.Verbose);
        }

        ~TrayIconFunctionality()
        {
            Dispose(false);    
        }

        /// <summary>
        /// OnNotifyIconDoubleClick is bound to the notifyicons double click event
        /// Tries to request a mainwindow if currently no mainwindow is open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            if (app == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("app was null when double clicking notifyicon", EClickerLogVerbosity.Warning);
                return;
            }

            if(app.GetApplicationState() == App.EApplicationState.WindowClosed)
            {
                app.CreateNewMainWindow();
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("notifyicon double clicked opening window", EClickerLogVerbosity.Verbose);
            }
        }


        /// <summary>
        /// OnIconExit is an item added to the context menu of the notifyicon
        /// Will request a shutdown of the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnIconExit(object sender, EventArgs e)
        {
            if (logFunctionality != null)
                logFunctionality.ClickerLog("App exit requested from notifyicon", EClickerLogVerbosity.Verbose);
            app.Shutdown();
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose takes care of disposing the notifyicon properly together with the icon itself
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Icon.Dispose();
                    notifyIcon.Icon = null;
                    notifyIcon.DoubleClick -= OnNotifyIconDoubleClick;
                    notifyIcon.Dispose();
                    notifyIcon = null;
                    if (logFunctionality != null)
                        logFunctionality.ClickerLog("Notifyicon disposed off", EClickerLogVerbosity.Verbose);
                }


            }

        }

    }
}
