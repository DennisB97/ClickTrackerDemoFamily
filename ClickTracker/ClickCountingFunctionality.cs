// Copyright 2021 Dennis Baeckstroem 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ClickTracker
{

    /// <summary>
    /// ClickCountingFunctionality provides mouse hooking and classes for storing the clickdata.
    /// </summary>
    [DataContract(Name = "CCF", Namespace = "")]
    public class ClickCountingFunctionality : IDisposable
    {
        //------------------------------UNMANAGED DEFINITIONS RELATED-----------------------------------//

        // defines the delegate function format that the mouse hook will send mouse events to
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate IntPtr HookProc(int nCode, UIntPtr wParam, IntPtr lParam);

        // Type of hook for windows, 14 equals a lowelevelmouse hook
        enum EHookType
        {
            LowLevelMouse = 14
        }

        // class for accessing the external windows methods for hooking the mouse
        class ExternalMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(EHookType idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

            [DllImport("user32.dll")]
            public static extern int UnhookWindowsHookEx(IntPtr hHook);

            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr _, int nCode, UIntPtr wParam, IntPtr lParam);
        }

        // Different messages the delegate will give when mouse does something
        public enum EMouseMessage
        {
            MouseMove = 0x200,
            LButtonDown = 0x201,
            LButtonUp = 0x202,
            RButtonDown = 0x204,
            RButtonUp = 0x205,
            MouseWheel = 0x20a,
            MouseHWheel = 0x20e
        }

        // Point is the screen coordinates which mouse delegate gives. But is not used in this application.
        [StructLayout(LayoutKind.Sequential)]
        public class Point
        {
            public int x;
            public int y;
        }

        // Struct defining the whole struct message given to mouse delegate
        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public Point pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        // Used for the eventhandler given the customized eventargs
        public class NewMouseMessageEventArgs : EventArgs
        {
            public Point Position { get; private set; }
            public EMouseMessage MessageType { get; private set; }

            public NewMouseMessageEventArgs(Point position, EMouseMessage msg)
            {
                Position = position;
                MessageType = msg;
            }
        }

        //  defines the event used to connect the functions listening to the mouse events
        public event EventHandler<NewMouseMessageEventArgs> NewMouseMessage;

        // Holds the hook received from the external method
        public IntPtr MouseHook { get; private set; } = IntPtr.Zero;

        //------------------------------UNMANAGED DEFINITIONS RELATED END-----------------------------------//

        /// <summary>
        /// FDayClickCount is for containing one day of clickdata
        /// </summary>
        [DataContract(Name = "DCC", Namespace = "")]
        public class FDayClickCount
        {
            public FDayClickCount() { }

            // dayclickdata consists of a key for the hours 0-23 which then value itself contains the amount of clicks
            [DataMember]
            public SortedDictionary<ushort, FHourClickCount> dayClickData = new SortedDictionary<ushort, FHourClickCount>();
        }

        /// <summary>
        /// FHourClickCount is for containing an hour of clickdata which means it is the most inner class of clickdata type, 
        /// i.e. contains the integer value for left and right clicks.
        /// </summary>
        [DataContract(Name = "HCC", Namespace = "")]
        public class FHourClickCount
        {
            public FHourClickCount(uint inLeftMouseClicks = 0, uint inRightMouseClicks = 0)
            {
                leftMouseClicks = inLeftMouseClicks;
                rightMouseClicks = inRightMouseClicks;
            }

            public static FHourClickCount operator +(FHourClickCount a, FHourClickCount b)
            {
                return new FHourClickCount(a.leftMouseClicks + b.leftMouseClicks, a.rightMouseClicks + b.rightMouseClicks);
            }

            [DataMember]
            public uint leftMouseClicks = 0;

            [DataMember]
            public uint rightMouseClicks = 0;

        }

        // Contains the current accumulated click data where each click with mouse will be added into this variable
        private FHourClickCount currentClickData = new FHourClickCount();

        //The outer most clickdata variable which contains a dictionary for days with the clickdata as value
        [DataMember]
        SortedDictionary<DateTime, FDayClickCount> clickData = null;

        private const string unSyncedClickDataFileName = "UnSyncedClickedData.xml";

        // Timer which will be set to run each hour a second before hour changes for it to stash the currently accumulated clicks for current hour
        // So that it can start accumulating for next hour 
        private Timer beforeHourChangeTimer = null;

        // This is the time that the timer was started at, so that the current click count is added to the correct hour time
        private DateTime beforeHourChangeTimerStartTime;

        // bSyncInterrupted is used if application is closed while syncing data to database,
        // it will stop the normal procedure continuing if it happens while app is closing.
        private bool bSyncInterrupted = false;

        // Mutex used for keeping the current click data safe from multiple threads
        private Mutex mut = new Mutex();

        /// <summary>
        /// AddClickCount is used when mouse hook sees a left or right mouse message which it then provides to this function so that it can ++ correct integer
        /// </summary>
        /// <param name="message">Is the mouse message that occured, either left or right</param>
        private void AddClickCount(EMouseMessage message)
        {
            if (currentClickData == null)
            {
                return;
            }

            mut.WaitOne();
            if (message == EMouseMessage.LButtonUp)
            {
                ++currentClickData.leftMouseClicks;
            }
            else if (message == EMouseMessage.RButtonUp)
            {
                ++currentClickData.rightMouseClicks;
            }
            mut.ReleaseMutex();


        }

        /// <summary>
        /// GetClickCount is used to get the current accumulated click count for current hour
        /// </summary>
        /// <returns>Returns the current clickdata as FHourClickCount</returns>
        private FHourClickCount GetClickCount()
        {
            if (currentClickData == null)
            {
                return new FHourClickCount();
            }

            FHourClickCount clickDataCopy = null;
            mut.WaitOne();
            clickDataCopy = new FHourClickCount(currentClickData.leftMouseClicks, currentClickData.rightMouseClicks);
            mut.ReleaseMutex();
            return clickDataCopy;
        }

        /// <summary>
        /// ClearClickCount is used to clear the current accumulated click count, used after the current click count has been received and stashed
        /// </summary>
        private void ClearClickCount()
        {
            if (currentClickData == null)
            {
                return;
            }

            mut.WaitOne();
            currentClickData.leftMouseClicks = 0;
            currentClickData.rightMouseClicks = 0;
            mut.ReleaseMutex();
            if (logFunctionality != null)
                logFunctionality.ClickerLog("Click count cleared", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// RequestSync is called from datasyncfunctionality class when a sync to database is requested
        /// This method will stash the current hour accumulated data and then proceed to try syncing to database
        /// </summary>
        /// <returns>Returns a Task<bool> and depending on if the sync was successfull or not returns the bool value</bool></returns>
        public async Task<bool> RequestSync()
        {
            StashSyncData(beforeHourChangeTimerStartTime);

            if(clickData == null)
            {
                return false;
            }


            // Sync one day per time, in case of an interruption , then will lose less data.
            while (clickData.Count != 0)
            {
                var dayClickData = clickData.FirstOrDefault();

                var result = await app.GetServiceProvider().GetRequiredService<IConnectivityFunctionality>().SyncData(app.GetUniqueString(),dayClickData.Key,dayClickData.Value);

                if (result.status == EConnectionReturnStatus.PreConnectionFailed)
                {
                    if(delayedMessageFunctionality != null)
                    {
                        delayedMessageFunctionality.RequestMessage("Failed to connect to server while trying to sync: \n" + result.message);
                    }

                    return false;
                }
                else if (result.status == EConnectionReturnStatus.Success && !bSyncInterrupted)
                {
                    clickData.Remove(dayClickData.Key);
                    CheckAndSaveUnSyncedClickData(false);
                    await Task.Delay(500);
                }
                else
                {
                    if(delayedMessageFunctionality != null)
                    {
                        delayedMessageFunctionality.RequestMessage("Something went wrong in server side when trying to sync: \n" + result.message);
                    }
                   
                    return false;
                }



            }

            return true;
          
        }

        /// <summary>
        /// InterruptSync is called from a shutdown event method in case application is shutting down while syncing
        /// This will set bSyncInterrupted as true and then proceed to delete the first clickdata found which is currently being synced to the database.
        /// </summary>
        public void InterruptSync()
        {
            bSyncInterrupted = true;
            if(clickData != null && clickData.Count > 0)
            {
                var dayClickData = clickData.FirstOrDefault();
                clickData.Remove(dayClickData.Key);
            }
           
        }


        // Cancellationtokens used for stopping the thread
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token;

        // holds the thread that runs the hooking and event
        private Thread thread = null;

        private readonly App app = null;

        private readonly DelayedMessageFunctionality delayedMessageFunctionality = null;

        private readonly ILogFunctionality logFunctionality = null;

        // ctor taking care of starting the thread and timers and loading any unsynced data that is stored locally
        public ClickCountingFunctionality(App inApp, DelayedMessageFunctionality inDelayedMessageFunctionality, ILogFunctionality inLogFunctionality)
        {
            app = inApp;
            delayedMessageFunctionality = inDelayedMessageFunctionality;
            logFunctionality = inLogFunctionality;

            LoadUnSyncedClickData();

            StartHourEndTimer();

            thread = new Thread(Start);
            thread.IsBackground = true;
            thread.Start();

  
            if(logFunctionality != null)
            logFunctionality.ClickerLog("ClickCountingFunctionality constructed", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// StartHourEndTimer is called each time again after timer has run once to correctly set it to next hours x:59:99 moment
        /// </summary>
        private void StartHourEndTimer()
        {
            if (beforeHourChangeTimer != null)
            {
                beforeHourChangeTimer.Dispose();
                beforeHourChangeTimer = null;
            }

            
            DateTime currentTime = DateTime.Now;

            // In the event of it still being the same hour after stashing for last hour was done, then needs to add another hour for it to set timer correctly 
            if (beforeHourChangeTimerStartTime.Hour == currentTime.Hour)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Adding an hour to beforehourchangeendtimer because execution was fast enough to stay within same hour", EClickerLogVerbosity.Verbose);
                currentTime = currentTime.AddHours(1);
            }
            beforeHourChangeTimerStartTime = currentTime;

            DateTime runTime = new DateTime(beforeHourChangeTimerStartTime.Year, beforeHourChangeTimerStartTime.Month, beforeHourChangeTimerStartTime.Day, beforeHourChangeTimerStartTime.Hour, 59, 59);

            TimeSpan ts = runTime.Subtract(DateTime.Now);

            beforeHourChangeTimer = new Timer(new TimerCallback(ScheduledHourEndTimer), null, ((int)ts.TotalMilliseconds), ((int)ts.TotalMilliseconds));

            if (logFunctionality != null)
                logFunctionality.ClickerLog("StartHourEnd timer started, next call in: " + ts.Minutes.ToString(), EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// This is the method that runs for hourendtimer 
        /// Disposes the current timer and stashes the data and proceeds to call method to set the timer again.
        /// </summary>
        /// <param name="state"></param>
        private void ScheduledHourEndTimer(object state)
        {
            if (beforeHourChangeTimer != null)
            {
                beforeHourChangeTimer.Dispose();
                beforeHourChangeTimer = null;
            }

            if (logFunctionality != null)
                logFunctionality.ClickerLog("HourEndTimer run", EClickerLogVerbosity.Verbose);

            StashSyncData(beforeHourChangeTimerStartTime);

            StartHourEndTimer();
        }


        ~ClickCountingFunctionality()
        {
            Dispose(false);
        }

        /// <summary>
        /// LoadUnSyncedClickData is used to load from file any unsynced clickdata which would be stored as xml locally if no database sync has been done in awhile.
        /// </summary>
        private void LoadUnSyncedClickData()
        {
            clickData = DataSaveAndLoader.DataContractReadDataFromXML<SortedDictionary<DateTime, FDayClickCount>>(typeof(SortedDictionary<DateTime, FDayClickCount>), unSyncedClickDataFileName);

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Unsynced click data loaded", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// CheckAndSaveUnSyncedClickData will optionally check current hour clicks and stash that too
        /// Or else it will just save the currently stashed data to local file as xml
        /// </summary>
        /// <param name="check"> check parameter is used for indicating if current hours accumulated data should be checked and processed together with already gathered clickdata </param>
        public void CheckAndSaveUnSyncedClickData(bool check = true)
        {
            if(check)
            {
                DateTime now = DateTime.Now;
                StashSyncData(now);
            }
           
            DataSaveAndLoader.DataContractSaveAsXML<SortedDictionary<DateTime, FDayClickCount>>(clickData, typeof(SortedDictionary<DateTime, FDayClickCount>), unSyncedClickDataFileName);

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Unsynced data saved to file", EClickerLogVerbosity.Verbose);
        }


        /// <summary>
        /// StashSyncData takes the current accumulated clickdata and will stash it to a new hour if current hour is not found to have data yet
        /// </summary>
        /// <param name="now">now is the hour time which for the current clickdata belongs to</param>
        private void StashSyncData(DateTime now)
        {
            DateTime nowCopy = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            FHourClickCount inProgressClickCount = GetClickCount();


            if (inProgressClickCount.leftMouseClicks <= 0 && inProgressClickCount.rightMouseClicks <= 0)
            {
                return;
            }

            if (clickData == null)
            {
                clickData = new SortedDictionary<DateTime, FDayClickCount>();
            }

            if (!clickData.ContainsKey(nowCopy))
            {
                FDayClickCount newDayClickCount = new FDayClickCount();
                clickData.Add(nowCopy, newDayClickCount);
            }


            if (!clickData[nowCopy].dayClickData.ContainsKey(((ushort)now.Hour)))
            {
                FHourClickCount newHourClickCount = new FHourClickCount();
                clickData[nowCopy].dayClickData.Add((ushort)now.Hour, newHourClickCount);
            }

            FHourClickCount stashedClickCount = null;
            if (clickData[nowCopy].dayClickData.TryGetValue((ushort)now.Hour, out stashedClickCount))
            {
                stashedClickCount = inProgressClickCount + stashedClickCount;
                clickData[nowCopy].dayClickData[(ushort)now.Hour] = stashedClickCount;
            }
            else
            {
                clickData[nowCopy].dayClickData.Add((ushort)now.Hour, inProgressClickCount);
            }

            ClearClickCount();

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Sync data stashed", EClickerLogVerbosity.Verbose);
        }

        // Start is the function the thread runs at first
        private void Start()
        {
            // get cancellation token from source
            token = source.Token;

            // Hook here the mouse
            MouseHook = ExternalMethods.SetWindowsHookEx(EHookType.LowLevelMouse, LowLevelMouseProc, IntPtr.Zero, 0);

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Mouse hooked", EClickerLogVerbosity.Verbose);

            // Bind the mouse event function to the event
            NewMouseMessage += new EventHandler<NewMouseMessageEventArgs>(OnMouseEvent);

            // Loop thread with a message loop
            System.Windows.Forms.Application.Run();
        }

        // Gets the mouse events and forwards if message exists
        private IntPtr LowLevelMouseProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var st = Marshal.PtrToStructure<MouseHookStruct>(lParam);
                NewMouseMessage?.Invoke(this, new NewMouseMessageEventArgs(st.pt, (EMouseMessage)wParam));

            }
            return ExternalMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        // This function is invoken by the function listening to mouse events
        private void OnMouseEvent(object sender, NewMouseMessageEventArgs e)
        {
            // Check the token if thread should stop
            if (token.IsCancellationRequested)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Thread exit requested", EClickerLogVerbosity.Verbose);
                System.Windows.Forms.Application.Exit();
            }

            // If the button this application tracks, then proceed with saving value.
            if (e.MessageType == EMouseMessage.LButtonUp)
            {
                AddClickCount(EMouseMessage.LButtonUp);
            }
            else if (e.MessageType == EMouseMessage.RButtonUp)
            {
                AddClickCount(EMouseMessage.RButtonUp);
            }


        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose is used to clean up this class, where the thread is joined until it stops because a token has been requested for cancel
        /// Then it disposes the thread and timers and unhooks the mouse and saves locally the syncdata
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Cancel();
                thread.Join();
                source.Dispose();
                thread = null;

                if (beforeHourChangeTimer != null)
                {
                    beforeHourChangeTimer.Dispose();
                    beforeHourChangeTimer = null;
                }

                if (logFunctionality != null)
                    logFunctionality.ClickerLog("members disposed in clickcountingfunctionality", EClickerLogVerbosity.Verbose);

                CheckAndSaveUnSyncedClickData();
            }

            ExternalMethods.UnhookWindowsHookEx(MouseHook);
            if (logFunctionality != null)
                logFunctionality.ClickerLog("Mouse unhooked", EClickerLogVerbosity.Verbose);

        }

    }
}
