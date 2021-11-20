// Copyright 2021 Dennis Baeckstroem 

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;




namespace ClickTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        
        private  MainWindow mainWindow = null;

        //Service provider used for the httpclient class
        private readonly ServiceCollection serviceCollection = null;
        private readonly ServiceProvider serviceProvider = null;
       
        //--------- All the classes that extends this applications functionality
        private UniqueStringIDFunctionality uniqueStringFunctions = null;
        private DataSyncFunctionality dataSyncFunctionality = null;
        private SettingsFunctionality settingsFunctionality = null;
        private TrayIconFunctionality trayIconFunctionality = null;
        private ClickCountingFunctionality clickCountingFunctionality = null;
        private DelayedMessageFunctionality delayedMessageFunctionality = null;
        private ILogFunctionality logFunctionality = null;

        //---------- delegates and events that this main app.cs provides for cross class access
        public delegate void SyncTimeChangedEventHandler(object sender, int newHour);
        public event SyncTimeChangedEventHandler SyncTimeChangedEvent;

        public delegate void AutoSyncRunChangedEventHandler(object sender, bool run);
        public event AutoSyncRunChangedEventHandler AutoSyncRunChangedEvent;

        public delegate void UniqueStringIDReadyEventHandler(object sender, string inUniqueStringID);
        public event UniqueStringIDReadyEventHandler UniqueStringIDReadyEvent;

        public delegate void MainWindowEventHandler(object sender, MainWindow createdWindow);
        public event MainWindowEventHandler MainWindowCreatedEvent;
        public event MainWindowEventHandler MainWindowDestroyedEvent;

        // Mutex for stopping two of clicktrackers running at same time
        static System.Threading.Mutex appMutex = new System.Threading.Mutex(true, "ClickTracker_Application");


        public ServiceProvider GetServiceProvider()
        {
            return serviceProvider;
        }

        public string GetUniqueString()
        {
            if (uniqueStringFunctions != null)
            {
                return uniqueStringFunctions.GetUniqueString();
            }
            return "";
        }

        
        public void SyncTimeChanged(int hour)
        {
            if (SyncTimeChangedEvent != null)
            {
                SyncTimeChangedEvent(this,hour);
            }
        }
       
        public void AutoSyncRunChanged(bool run)
        {
            if (AutoSyncRunChangedEvent != null)
            {
                AutoSyncRunChangedEvent(this,run);
            }
        }
       
        public void UniqueStringIDReadied(string inUniqueString)
        {
            if(UniqueStringIDReadyEvent != null)
            {
                UniqueStringIDReadyEvent(this, inUniqueString);
            }
        }

        /// <summary>
        /// EApplicationState is a state for knowing if the current app has a window or not
        /// </summary>
        public enum EApplicationState
        {
            WindowClosed,
            WindowOpen
        }
        private EApplicationState applicationState = EApplicationState.WindowClosed;

        public EApplicationState GetApplicationState()
        {
            return applicationState;
        }
      
        /// <summary>
        /// Called to receive the settings from settingsfunctionality
        /// </summary>
        /// <returns>The FSettings class containing current settings</returns>
        public ClickTracker.SettingsFunctionality.FSettings GetSettings()
        {
            if(settingsFunctionality == null)
            {
                return null;
            }

            return settingsFunctionality.CurrentSettings;
        }

        /// <summary>
        /// OnAppExit is made to run the DoCleanup method which takes care of cleaning up before exiting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppExit(object sender, ExitEventArgs e)
        {
            DoCleanup();  
        }

        /// <summary>
        /// In the .ctor the servicecollection is configured and then provider built
        /// </summary>
        public App()
        {
            serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// OnAppStartup contains setting up all custom functionality classes and lastly creating a MainWindow if settings is not as hidden launch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            if (!appMutex.WaitOne(TimeSpan.Zero, true))
            {
                //there is already another instance
                MessageBox.Show("Another instance already running!");
                Application.Current.Shutdown();
            }

            //Prepare folder structure in local appdata for this application if not exists
            System.IO.Directory.CreateDirectory(DataSaveAndLoader.savePath);


            //Set already to not shutdown if mainwindow doesn't get launched, if hidden launch is enabled
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            //Initialize the classes with functionalities
            InitializeCustomComponents();

            //If not hidden launch then create a MainWindow on start
            if (settingsFunctionality != null && !settingsFunctionality.CurrentSettings.bLaunchHidden)
            {
                CreateNewMainWindow();         
            }
            else
            {
                applicationState = EApplicationState.WindowClosed;
            }

            if(logFunctionality != null)
            {
                logFunctionality.ClickerLog("App startup complete", EClickerLogVerbosity.Verbose);
            }
            
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddHttpClient<IConnectivityFunctionality, ConnectivityFunctionality>();
        }

        /// <summary>
        /// CreateNewMainWindow takes care of creating a MainWindow class and binding closing event from it. 
        /// Then showing the window.
        /// </summary>
        public void CreateNewMainWindow()
        {
            if (mainWindow != null)
            {
                mainWindow.Close();
            }

            applicationState = EApplicationState.WindowOpen;

            mainWindow = new MainWindow();
            mainWindow.Closing += new System.ComponentModel.CancelEventHandler(OnMainWindowClosing);
               
            mainWindow.Show();
            MainWindowCreatedEvent(this, mainWindow);

            if(logFunctionality != null)
            {
                logFunctionality.ClickerLog("New MainWindow created", EClickerLogVerbosity.Verbose);
            }
           
        }

        /// <summary>
        /// OnMainWindowClosing will unbind itself from the closing event and send a windowdestroyed event.
        /// Also checks if should exit on mainwindow close, if it does then it shutdowns the app.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.Closing -= OnMainWindowClosing;

                MainWindowDestroyedEvent(this, mainWindow);

                mainWindow = null;
            }
           

            applicationState = EApplicationState.WindowClosed;


            if(settingsFunctionality != null && settingsFunctionality.CurrentSettings != null)
            {
                if(settingsFunctionality.CurrentSettings.bExitOnClose)
                {
                    Shutdown();
                }
            }

            if (logFunctionality != null)
            {
                logFunctionality.ClickerLog("MainWindow closed", EClickerLogVerbosity.Verbose);
            }
        }


        private void InitializeCustomComponents()
        {
            delayedMessageFunctionality = new DelayedMessageFunctionality(this);
            logFunctionality = new LogFunctionality();
            settingsFunctionality = new SettingsFunctionality(this,logFunctionality);
            clickCountingFunctionality = new ClickCountingFunctionality(this, delayedMessageFunctionality, logFunctionality);
            dataSyncFunctionality = new DataSyncFunctionality(this,clickCountingFunctionality, logFunctionality);
            trayIconFunctionality = new TrayIconFunctionality(this, logFunctionality);
            uniqueStringFunctions = new UniqueStringIDFunctionality(this, delayedMessageFunctionality, logFunctionality);

            if(logFunctionality != null)
            {
                logFunctionality.ClickerLog("Custom components initialized", EClickerLogVerbosity.Verbose);
            }
           
        }

        /// <summary>
        /// OnBeforeWindowsShutdown is bound to the App.xaml event when application is forcefully closed ex. windows shutdown, 
        /// takes care of forwarding to cleanup method for trying to save any unsaved data locally before exiting.
        /// </summary>
        /// <param name="sender"> default sender argument of the object that sent this event </param>
        /// <param name="e">Cancelling event args, but not used, where you could request cancel of the shutdown and reason</param>
        private void OnBeforeWindowsShutdown(object sender, SessionEndingCancelEventArgs e)
        {
            DoCleanup();
        }

        /// <summary>
        /// DoCleanup does all the necessary stuff before exiting app.
        /// It takes care of interrupting a sync if inprogress, then disposes the classes that implement IDisposable
        /// </summary>
        private void DoCleanup()
        {
            if (dataSyncFunctionality != null)
            {
                dataSyncFunctionality.InterruptSync();
            }

            if (clickCountingFunctionality != null)
            {
                clickCountingFunctionality.Dispose();
            }

            if (trayIconFunctionality != null)
            {
                trayIconFunctionality.Dispose();
            }
 

            if(serviceProvider != null)
            {
                serviceProvider.Dispose();
            }
            
            if(logFunctionality != null)
            {
                logFunctionality.ClickerLog("App closing cleanup done", EClickerLogVerbosity.Verbose);
            }

        }
      



    }
}
