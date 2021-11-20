// Copyright 2021 Dennis Baeckstroem 

using System.Windows.Controls;
using System.Windows;
using Microsoft.Win32;


namespace ClickTracker
{
    /// <summary>
    /// SettingsFunctionality class handles 
    /// </summary>
    public class SettingsFunctionality
    {
        private MainWindow mainWindow = null;

        private readonly App app = null;

        private readonly ILogFunctionality logFunctionality = null;

        private const string settingsSaveName = "Configs.xml";

        public class FSettings
        {
            public bool bLaunchOnStartup;

            public bool bLaunchHidden;

            public bool bUseAutomaticSync;

            public bool bExitOnClose;

            public int ComBCurrentIndex;


        }

        private FSettings currentSettings = new FSettings();

        public FSettings CurrentSettings
        {
            get { return currentSettings; }
        }

        private FSettings templatedSettings = new FSettings();



        readonly string[] comboBoxItems = new string[] { "Every 1 Hour", "Every 2 Hours", "Every 3 Hours", "Every 4 Hours", "Every 5 hours", "Every 6 Hours", "Every 7 Hours", "Every 8 Hours" };

        /// <summary>
        /// .ctor takes care of binding mainwindow created and destroyed events and also initializing templated settings
        /// </summary>
        /// <param name="inApp"></param>
        /// <param name="inLogFunctionality"></param>
        public SettingsFunctionality(App inApp, ILogFunctionality inLogFunctionality)
        {

            app = inApp;

            logFunctionality = inLogFunctionality;
            // Bind events for mainwindow creation and destroy
            app.MainWindowCreatedEvent += new App.MainWindowEventHandler(OnWindowCreated);
            app.MainWindowDestroyedEvent += new App.MainWindowEventHandler(UnbindWindowEvents);

            // Init templated settings if needed later
            templatedSettings.bLaunchOnStartup = true;
            templatedSettings.bLaunchHidden = false;
            templatedSettings.bUseAutomaticSync = true;
            templatedSettings.bExitOnClose = false;
            templatedSettings.ComBCurrentIndex = 0;

            // Proceed to try load settings
            LoadSettings();
        }

        /// <summary>
        /// OnWindowCreated when window gets created this is called where the visual settings for each setting UIElement,
        /// and then the UIElements events are bound.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="inMainWindow"></param>
        public void OnWindowCreated(object sender, MainWindow inMainWindow)
        {
            if (inMainWindow == null)
            {
                return;
            }

            mainWindow = inMainWindow;

            ApplyVisualSettings();

            if (mainWindow.CHK_bAutomaticSync != null)
            {
                mainWindow.CHK_bAutomaticSync.Click += new RoutedEventHandler(OnAutomaticSyncChecked);
            }

            if (mainWindow.CHK_bLaunchHidden != null)
            {
                mainWindow.CHK_bLaunchHidden.Click += new RoutedEventHandler(OnLaunchHiddenChecked);
            }

            if (mainWindow.CHK_bLaunchOnStartup != null)
            {
                mainWindow.CHK_bLaunchOnStartup.Click += new RoutedEventHandler(OnLaunchOnStartupChecked);
            }

            if (mainWindow.CHK_bExitOnClose != null)
            {
                mainWindow.CHK_bExitOnClose.Click += new RoutedEventHandler(OnExitOnCloseChecked);
            }

            if (mainWindow.ComB_SyncSchedule != null)
            {
                foreach (var item in comboBoxItems)
                {
                    mainWindow.ComB_SyncSchedule.Items.Add(item);
                }

                mainWindow.ComB_SyncSchedule.SelectionChanged += new SelectionChangedEventHandler(OnComBItemChanged);

            }

            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Settingsfunctionality is initialized", EClickerLogVerbosity.Verbose);
            }
        }

        /// <summary>
        /// UnbindWindowEvents is called when mainwindow is destroyed,
        /// it will unbind all UIElement related events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="inMainWindow"></param>
        public void UnbindWindowEvents(object sender, MainWindow inMainWindow)
        {
            if (inMainWindow == null)
            {
                if (logFunctionality != null)
                {
                    logFunctionality.ClickerLog("inMainWindow was null when trying to unbind events", EClickerLogVerbosity.Warning);
                }

                return;
            }

            if (inMainWindow.CHK_bAutomaticSync != null)
            {
                inMainWindow.CHK_bAutomaticSync.Click -= OnAutomaticSyncChecked;
            }

            if (inMainWindow.CHK_bLaunchHidden != null)
            {
                inMainWindow.CHK_bLaunchHidden.Click -= OnLaunchHiddenChecked;
            }

            if (inMainWindow.CHK_bLaunchOnStartup != null)
            {
                inMainWindow.CHK_bLaunchOnStartup.Click -= OnLaunchOnStartupChecked;
            }

            if (inMainWindow.CHK_bExitOnClose != null)
            {
                inMainWindow.CHK_bExitOnClose.Click -= OnExitOnCloseChecked;
            }

            if (inMainWindow.ComB_SyncSchedule != null)
            {
                inMainWindow.ComB_SyncSchedule.SelectionChanged -= OnComBItemChanged;
            }

            mainWindow = null;

            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Window events unbound in settingsfunctionality", EClickerLogVerbosity.Verbose);
            }

        }

        private void OnAutomaticSyncChecked(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }


            if (mainWindow.CHK_bAutomaticSync != null && mainWindow.CHK_bAutomaticSync.IsChecked.HasValue)
            {

                currentSettings.bUseAutomaticSync = mainWindow.CHK_bAutomaticSync.IsChecked.Value;

                ConfigureAutomaticSyncEnabled();
            }

        }

        private void OnLaunchHiddenChecked(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }


            if (mainWindow.CHK_bLaunchHidden != null && mainWindow.CHK_bLaunchHidden.IsChecked.HasValue)
            {
                currentSettings.bLaunchHidden = mainWindow.CHK_bLaunchHidden.IsChecked.Value;
                ConfigureHiddenLaunch();
            }


        }

        private void OnLaunchOnStartupChecked(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }

            if (mainWindow.CHK_bLaunchOnStartup != null && mainWindow.CHK_bLaunchOnStartup.IsChecked.HasValue)
            {
                currentSettings.bLaunchOnStartup = mainWindow.CHK_bLaunchOnStartup.IsChecked.Value;
                ConfigureLaunchOnStartup();
            }

        }

        private void OnExitOnCloseChecked(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }

            if (mainWindow.CHK_bExitOnClose != null && mainWindow.CHK_bExitOnClose.IsChecked.HasValue)
            {
                currentSettings.bExitOnClose = mainWindow.CHK_bExitOnClose.IsChecked.Value;
                ConfigureExitOnClose();
            }

        }

        private void OnComBItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }

            if (mainWindow.ComB_SyncSchedule != null && mainWindow.ComB_SyncSchedule.SelectedIndex > -1)
            {
                currentSettings.ComBCurrentIndex = mainWindow.ComB_SyncSchedule.SelectedIndex;
                ConfigureAutomaticSyncTime();
            }

        }


        /// <summary>
        /// LoadSettings tries to load from local file, if not found will set currentsettings to templatedsettings and proceed to apply these settings
        /// </summary>
        private void LoadSettings()
        {
            bool bSettingsFound = false;

            FSettings saveData = DataSaveAndLoader.ReadDataFromXML<FSettings>(typeof(FSettings), settingsSaveName);
            if (saveData != null)
            {
                bSettingsFound = true;
                currentSettings = saveData;

                if (logFunctionality != null)
                {
                    logFunctionality.ClickerLog("Settings found from file loading", EClickerLogVerbosity.Verbose);
                }

            }

            if (!bSettingsFound)
            {
                currentSettings = templatedSettings;

                if (logFunctionality != null)
                {
                    logFunctionality.ClickerLog("Settings not found using template", EClickerLogVerbosity.Verbose);
                }
            }

            ApplyRequiredSettings();
        }

        /// <summary>
        /// SaveSettings saves the current settings to file
        /// </summary>
        private void SaveSettings()
        {
            DataSaveAndLoader.SaveDataAsXML<FSettings>(currentSettings, typeof(FSettings), settingsSaveName);

            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Settings saved", EClickerLogVerbosity.Verbose);
            }
        }


        private void ApplyVisualSettings()
        {
            if (mainWindow.CHK_bAutomaticSync != null)
            {
                mainWindow.CHK_bAutomaticSync.IsChecked = currentSettings.bUseAutomaticSync;
            }

            if (mainWindow.CHK_bLaunchHidden != null)
            {
                mainWindow.CHK_bLaunchHidden.IsChecked = currentSettings.bLaunchHidden;
            }

            if (mainWindow.CHK_bLaunchOnStartup != null)
            {
                mainWindow.CHK_bLaunchOnStartup.IsChecked = currentSettings.bLaunchOnStartup;
            }

            if (mainWindow.CHK_bExitOnClose != null)
            {
                mainWindow.CHK_bExitOnClose.IsChecked = currentSettings.bExitOnClose;
            }

            if (mainWindow.ComB_SyncSchedule != null)
            {
                mainWindow.ComB_SyncSchedule.SelectedIndex = currentSettings.ComBCurrentIndex;
            }

        }

        /// <summary>
        /// ApplyRequiredSettings applies the necessary settings that can be setup when the application starts
        /// </summary>
        private void ApplyRequiredSettings()
        {

            ConfigureHiddenLaunch();

            ConfigureLaunchOnStartup();

            ConfigureExitOnClose();


            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Applied all required startup settings", EClickerLogVerbosity.Verbose);
            }

        }

        private void ConfigureAutomaticSyncEnabled()
        {
            if (app == null)
            {
                return;
            }

            app.AutoSyncRunChanged(CurrentSettings.bUseAutomaticSync);

            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Automatic sync run status changed", EClickerLogVerbosity.Verbose);
            }


            SaveSettings();
        }

        private void ConfigureAutomaticSyncTime()
        {
            if (app == null)
            {
                return;
            }

            // Add + 1 to the index because array starts from 0 , so when 1 added to it, it will correspond to the combobox's hour settings.
            app.SyncTimeChanged(CurrentSettings.ComBCurrentIndex + 1);

            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Automatic sync configured", EClickerLogVerbosity.Verbose);
            }


            SaveSettings();
        }


        private void ConfigureHiddenLaunch()
        {
            SaveSettings();
        }

        private void ConfigureLaunchOnStartup()
        {

#if !DEBUG // Don't startup write to registry in debug builds


            // also at least when debugger attached don't write to registry 
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return;
            }


            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (currentSettings.bLaunchOnStartup)
                {
                    rk.SetValue(Application.ResourceAssembly.GetName().Name, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                }
                else
                {
                    rk.DeleteValue(Application.ResourceAssembly.GetName().Name, false);
                }
            }


            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("Launch on startup configured", EClickerLogVerbosity.Verbose);
            }

            SaveSettings();
#endif
        }

        private void ConfigureExitOnClose()
        {

            logFunctionality.ClickerLog("Exit on close configured", EClickerLogVerbosity.Verbose);

            SaveSettings();
        }



    }
}
