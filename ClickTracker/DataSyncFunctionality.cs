// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Threading;


namespace ClickTracker
{
    /// <summary>
    /// DataSyncFunctionality handles auto syncing feature
    /// Manual syncing feature and also synchistory.
    /// </summary>
    class DataSyncFunctionality
    {
        private readonly App app = null;

        private readonly ILogFunctionality logFunctionality = null;

        private readonly ClickCountingFunctionality clickCountingFunctionality = null;

        private MainWindow mainWindow = null;

        private const string syncDateFileName = "SyncData.xml";

        // If no previous sync is found from file then it shows this instead of a date
        private const string syncDateNotFoundText = "No previous sync found";

        // The sync button texts, uses the int of syncstate for setting correct text.
        private readonly string[] syncButtonTexts = new string[] { "Sync now", "Manual Sync...", "Auto sync...", "Failed, try again", "No ID" };

        // The button default background when the button is active
        private readonly Brush buttonForegroundDefaultBrush = Brushes.Black;

        // The button background when button is in a disabled state
        private readonly Brush buttonForegroundDisabledBrush = Brushes.Gray;

        private Timer syncTimer = null;

        private const int maxSyncHistoryAmount = 20;

        private List<DateTime> syncHistory = null;

        private delegate void AutoSyncEventHandler(object state);

        public enum EDataSyncState
        {
            Idle,
            ManualSyncing,
            AutoSyncing,
            SyncFailed,
            NoID
        }

        private EDataSyncState currentState = EDataSyncState.NoID;


        /// <summary>
        /// In the .ctor the window created and destroyed events are bound
        /// Also the auto sync configured events are bound together with unique ID is received event
        /// </summary>
        /// <param name="inApp"></param>
        /// <param name="inClickCountingFunctionality"></param>
        /// <param name="inLogFunctionality"></param>
        public DataSyncFunctionality(App inApp, ClickCountingFunctionality inClickCountingFunctionality, ILogFunctionality inLogFunctionality)
        {
            if (inApp == null || inClickCountingFunctionality == null)
            {
                return;
            }

            app = inApp;
            clickCountingFunctionality = inClickCountingFunctionality;
            logFunctionality = inLogFunctionality;

            app.MainWindowCreatedEvent += new App.MainWindowEventHandler(OnWindowCreated);
            app.MainWindowDestroyedEvent += new App.MainWindowEventHandler(UnbindWindowEvents);

            LoadSyncHistory();
            
            app.SyncTimeChangedEvent += new App.SyncTimeChangedEventHandler(OnSyncTimeChanged);
            app.AutoSyncRunChangedEvent += new App.AutoSyncRunChangedEventHandler(OnAutoSyncRun);
            app.UniqueStringIDReadyEvent += new App.UniqueStringIDReadyEventHandler(OnUniqueStringReady);

        }

        /// <summary>
        /// OnWindowCreated is called when a mainwindow is created, depending on state it either enables or disables the button and sets correct button text and sync time text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="inMainWindow"></param>
        public void OnWindowCreated(object sender,MainWindow inMainWindow)
        {
            if (app == null || inMainWindow == null)
            {
                return;
            }
            mainWindow = inMainWindow;

            if (mainWindow.BTN_SyncNow != null)
            {
                mainWindow.BTN_SyncNow.Click += new RoutedEventHandler(OnButtonClick);
            }

            UpdateSyncTimeText();

            switch(currentState)
            {

                case EDataSyncState.ManualSyncing: //Fall through
                case EDataSyncState.AutoSyncing:
                case EDataSyncState.NoID:
                    {
                        DisableButton();
                        break;
                    }

                case EDataSyncState.Idle: // fall through
                case EDataSyncState.SyncFailed:
                    {
                        EnableButton(); 
                        break;
                    }

 
                default:
                    break;

            }

            ChangeSyncButtonText();

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Datasyncfunctionality initialized", EClickerLogVerbosity.Verbose);
        }

        private void ChangeSyncButtonText()
        {
            if (syncButtonTexts != null && syncButtonTexts.ElementAtOrDefault((int)currentState) != null)
            {
                ChangeButtonText(syncButtonTexts[(int)currentState]);
            }

        }

        public void UnbindWindowEvents(object sender, MainWindow inMainWindow)
        {
            if (inMainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("inMainWindow was null when trying to unbind events", EClickerLogVerbosity.Warning);
                return;
            }


            if (inMainWindow.BTN_SyncNow != null)
            {
                inMainWindow.BTN_SyncNow.Click -= OnButtonClick;
            }

            mainWindow = null;

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Window events unbound in datasyncfunctionality", EClickerLogVerbosity.Verbose);
        }

        ~DataSyncFunctionality()
        {
            if (app != null)
            {
                app.SyncTimeChangedEvent -= OnSyncTimeChanged;
                app.AutoSyncRunChangedEvent -= OnAutoSyncRun;
                app.UniqueStringIDReadyEvent -= OnUniqueStringReady;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Datasyncfunctionality destructed", EClickerLogVerbosity.Verbose);
            }

           
        }

       
        /// <summary>
        ///  For checking local savedata if a sync has been happened before
        ///  and if it has send it forward to updatesynctime to show the sync time
        /// </summary>
        private void LoadSyncHistory()
        {
           
            bool bContainsSyncData = false;

            syncHistory = DataSaveAndLoader.ReadDataFromXML<List<DateTime>>(typeof(List<DateTime>), syncDateFileName);

            if(syncHistory != null && syncHistory.Count > 0)
            {
                bContainsSyncData = true;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Previous sync history loaded", EClickerLogVerbosity.Verbose);
            }

            if (!bContainsSyncData)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("No previous sync history found", EClickerLogVerbosity.Verbose);
            }

            UpdateSyncTimeText();
        }

        /// <summary>
        /// Saves the sync history to file
        /// </summary>
        private void SaveSyncHistory()
        {
            DataSaveAndLoader.SaveDataAsXML<List<DateTime>>(syncHistory, typeof(List<DateTime>), syncDateFileName);
            if (logFunctionality != null)
                logFunctionality.ClickerLog("Sync history saved", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// Logs current datetime for synchistory
        /// </summary>
        private void LogSyncHistory()
        {
            if(syncHistory == null)
            {
                syncHistory = new List<DateTime>();
            }

            if(syncHistory.Count >= maxSyncHistoryAmount)
            {
                syncHistory.RemoveAt(0);
            }

            syncHistory.Add(DateTime.Now);
            syncHistory.Sort();


            SaveSyncHistory();
            UpdateSyncTimeText();
            if (logFunctionality != null)
                logFunctionality.ClickerLog("Sync history logged", EClickerLogVerbosity.Verbose);
        }

        private void UpdateSyncTimeText()
        {
            if (mainWindow != null && mainWindow.TB_LastSyncedDateTime != null)
            {
                if(syncHistory != null && syncHistory.Count > 0)
                {
                    mainWindow.TB_LastSyncedDateTime.Text = syncHistory.Last().ToString();
                }

                else
                {
                    mainWindow.TB_LastSyncedDateTime.Text = syncDateNotFoundText;
                }

               
            }

        }

        /// <summary>
        /// Disablebutton changes the buttonbackground and disables it
        /// </summary>
        private void DisableButton()
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when trying to disable sync button", EClickerLogVerbosity.Warning);
                return;
            }

            if (mainWindow.BTN_SyncNow != null)
            {
                mainWindow.BTN_SyncNow.IsEnabled = false;
                mainWindow.BTN_SyncNow.Foreground = buttonForegroundDisabledBrush;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Datasync button disabled", EClickerLogVerbosity.Verbose);
            }
        }

        /// <summary>
        /// Enablebutton changes back to default background and enables it
        /// </summary>
        private void EnableButton()
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when trying to enable sync button", EClickerLogVerbosity.Verbose);
                return;
            }

            if (mainWindow.BTN_SyncNow != null)
            {
                mainWindow.BTN_SyncNow.IsEnabled = true;
                mainWindow.BTN_SyncNow.Foreground = buttonForegroundDefaultBrush;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Datasync button enabled", EClickerLogVerbosity.Verbose);
            }
        }

        private void ChangeButtonText(string newText)
        {
            if(mainWindow == null)
            {
                return;
            }

            if(mainWindow.BTN_SyncNow != null)
            {
                mainWindow.BTN_SyncNow.Content = newText;
            }
        }


        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }

            if(currentState == EDataSyncState.Idle || currentState == EDataSyncState.SyncFailed)
            {
                currentState = EDataSyncState.ManualSyncing;
                SyncData();
            }
           
        }

        /// <summary>
        /// SyncData will disable the button and change the text while it will try to sync the clickdata to database
        /// and then re enables the button afterwards and updates the button text
        /// </summary>
        private async void SyncData()
        {

            if (app == null && currentState != EDataSyncState.ManualSyncing && currentState != EDataSyncState.AutoSyncing && currentState == EDataSyncState.NoID)
            {
                return;
            }

            ChangeSyncButtonText();
            DisableButton();
            

            bool bSyncResult = false;


            if (clickCountingFunctionality != null)
            {
                bSyncResult = await clickCountingFunctionality.RequestSync();
            }

            if (bSyncResult)
            {
                LogSyncHistory();
                currentState = EDataSyncState.Idle;
            }
            else
            {
                currentState = EDataSyncState.SyncFailed;
            }

            EnableButton();
            ChangeSyncButtonText();
        }

        /// <summary>
        /// InterruptSync is called from app when application is about to shutdown
        /// So that any inprogress sync can be stopped from modifying additional data.
        /// </summary>
        public void InterruptSync()
        {
            //If syncing then need to delete the first day clickdata in the dictionary to not duplicate the clickdata next time.
            if (currentState == DataSyncFunctionality.EDataSyncState.ManualSyncing || currentState == DataSyncFunctionality.EDataSyncState.AutoSyncing)
            {
                if (clickCountingFunctionality != null)
                {
                    clickCountingFunctionality.InterruptSync();
                }
            }
        }

        /// <summary>
        /// OnUniqueStringReady is bound to the ID readied event, this is called when an ID has been confirmed to be correct from the database
        /// This method takes care of calling method enabling the autosync if it is enabled per setting
        /// And if mainwindow is currently open it will enable the button and update button text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="inUniqueStringID"></param>
        private void OnUniqueStringReady(object sender, string inUniqueStringID)
        {
            currentState = EDataSyncState.Idle;
            if(app != null)
            {
                OnSyncTimeChanged(this, app.GetSettings().ComBCurrentIndex + 1);

                if(app.GetApplicationState() == App.EApplicationState.WindowOpen)
                {
                    EnableButton();
                    ChangeSyncButtonText();
                }


            }
        }

        /// <summary>
        /// OnAutoSyncRun is bound to the event that is sent when setting for enabling or disabling autosync has been changed
        /// It will dispose of any current running timer if it is set to disabled as per run parameter. 
        /// If it was set to be enabled it will call the method starting the timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="run">run indicates if the autosync is enabled or not</param>
        private void OnAutoSyncRun(object sender, bool run)
        {

            if(syncTimer != null && !run)
            {
                syncTimer.Dispose();
                syncTimer = null;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Auto sync disabled, disposing timer", EClickerLogVerbosity.Verbose);
            }
            else if(app != null && run)
            {
                // + 1 to get hour from index
                OnSyncTimeChanged(this, app.GetSettings().ComBCurrentIndex + 1);
            }

        }

        /// <summary>
        /// OnSyncTimeChanged is bound to event which is called when the setting for sync time interval is changed.
        /// It will dispose of any currently running timer and set a new one for the new specified time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newHour"></param>
        public void OnSyncTimeChanged(object sender, int newHour)
        {
            if(currentState == EDataSyncState.NoID)
            {
                return;
            }

            if (syncTimer != null)
            {
                syncTimer.Dispose();
                syncTimer = null;
            }

            if (app != null && !app.GetSettings().bUseAutomaticSync)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Auto sync time changed, but auto sync is not enabled", EClickerLogVerbosity.Verbose);
                return;
            }


            // Check last sync history 
            DateTime lastSync = new DateTime(1900,1,1); 
            if(syncHistory != null && syncHistory.Count > 0)
            {
                lastSync = syncHistory.LastOrDefault();
            }

            DateTime runHour = lastSync.AddHours(newHour);
            TimeSpan ts = runHour.Subtract(DateTime.Now);
            
            // If last sync + the new run hour is still older than current time then do a sync right now.
            if(ts.TotalSeconds <= 0)
            {
                if (currentState == EDataSyncState.Idle || currentState == EDataSyncState.SyncFailed)
                {
                    currentState = EDataSyncState.AutoSyncing;
                    SyncData();
                }
                runHour = DateTime.Now.AddHours(newHour);
                ts = runHour.Subtract(DateTime.Now);
            }
            
            // Set the timer to run
            syncTimer = new System.Threading.Timer(new System.Threading.TimerCallback(ScheduledSync), null, (int)ts.TotalMilliseconds, (int)ts.TotalMilliseconds);

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Auto sync timer started, next sync in: " + ts.Minutes.ToString(), EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// ScheduledSync is run from syncTimer, takes care of calling syncdata and setting state to autosyncing. 
        /// First checking if it has access that is it is the UI thread because if mainwindow is open the UI needs to be modified.
        /// </summary>
        /// <param name="state"></param>
        private void ScheduledSync(object state)
        {
            
            if (App.Current != null && App.Current.Dispatcher.CheckAccess())
            {
                if(currentState == EDataSyncState.Idle || currentState == EDataSyncState.SyncFailed)
                {
                    currentState = EDataSyncState.AutoSyncing;
                    SyncData();
                }
              
            }
            else if(App.Current != null)
            {
                App.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,new AutoSyncEventHandler(this.ScheduledSync),state);
            }

        }

    }

}
