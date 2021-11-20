// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Threading;

namespace ClickTracker
{
    /// <summary>
    /// EClickerLogVerbosity is enum for the verbosity of a log message. 
    /// If the log verbositylevel is below the value of given verbosity message it will be shown else it wont be logged.
    /// </summary>
    public enum EClickerLogVerbosity
    {
        Verbose,
        Warning,
        Error,
        Fatal
    }

    public interface ILogFunctionality
    {
        /// <summary>
        /// ClickerLog handles the construction of the string message and then proceeds to save it through the DataSaveAndLoader method which appends to a txt file.
        /// </summary>
        /// <param name="logMessage">The message that should be logged</param>
        /// <param name="verbosity">The message verbosity</param>
        /// <param name="memberName">The membername is from which method name the call came from. It is automatically received by the [CallerMemberName] from the caller method</param>
        public void ClickerLog(string logMessage, EClickerLogVerbosity verbosity, [CallerMemberName] string memberName = "");
    }


    class LogFunctionality : ILogFunctionality
    {

        public const string logFileName = "ClickTrackerLog.txt";

        public readonly EClickerLogVerbosity verbosityLevel = EClickerLogVerbosity.Verbose;

        public LogFunctionality()
        {
            //Clear previous log file
            DataSaveAndLoader.DeleteFile(logFileName);

#if !DEBUG
            verbosityLevel = EClickerLogVerbosity.Warning;
#endif
        }

   
        /// <summary>
        /// ClickerLog handles the construction of the string message and then proceeds to save it through the DataSaveAndLoader method which appends to a txt file.
        /// </summary>
        /// <param name="logMessage">The message that should be logged</param>
        /// <param name="verbosity">The message verbosity</param>
        /// <param name="memberName">The membername is from which method name the call came from. It is automatically received by the [CallerMemberName] from the caller method</param>
        public void ClickerLog(string logMessage,EClickerLogVerbosity verbosity, [CallerMemberName] string memberName = "")
        {
            string preFix = "Log " + verbosity.ToString() + ": " + DateTime.Now.ToString() + " Place: " + memberName + " Message: ";
            string message = preFix + logMessage;


            switch(verbosity)
            {
                case EClickerLogVerbosity.Verbose:
                    {
                        if((int)verbosityLevel == (int)EClickerLogVerbosity.Verbose)
                        {
                            DataSaveAndLoader.SaveText(message, logFileName);
                        }
                        break;
                    }
                   
                case EClickerLogVerbosity.Warning:
                    {
                        if ((int)verbosityLevel >= (int)EClickerLogVerbosity.Warning)
                        {
                             DataSaveAndLoader.SaveText(message, logFileName);
                        }
                        break;
                    }
                  
                case EClickerLogVerbosity.Error:
                    {
                        if ((int)verbosityLevel >= (int)EClickerLogVerbosity.Error)
                        {
                            DataSaveAndLoader.SaveText(message, logFileName);
                        }
                        break;
                    }
                    
                case EClickerLogVerbosity.Fatal:
                    {
                        if ((int)verbosityLevel >= (int)EClickerLogVerbosity.Fatal)
                        {
                            DataSaveAndLoader.SaveText(message, logFileName);
                            MessageBox.Show(logMessage);

                          
                            if (Application.MessageLoop)
                            {
                                Application.Exit();
                            }
                                
                            else
                            {
                                Environment.Exit(1);
                            }
                                
                        }
                        break;
                    }
                   
                default:
                    break;

            }

            
        }






    }
}
