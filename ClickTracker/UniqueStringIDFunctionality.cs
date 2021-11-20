// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;

namespace ClickTracker
{
    /// <summary>
    /// UniqueStringIDFunctionality takes care of handling everything regarding the unique string ID
    /// It binds and unbinds the UIElement events for the UIElements associated with unique string ID
    /// It sends calls to the connectivityfunctionality class for checking and aquiring the unique ID 
    /// It also sends call to the connectivityfunctionality for unlocking the ID by request and handles locking it by loading too.
    /// </summary>
    class UniqueStringIDFunctionality
    {
        private readonly App app = null;
        private readonly DelayedMessageFunctionality delayedMessageFunctionality = null;
        private readonly ILogFunctionality logFunctionality = null;

        //Pointing to the main window class, where all application UI elements are.
        private MainWindow mainWindow = null;

        //Window handling the receiving of a previous unique string if no uniquestring was found in local files.
        private LoadUniqueStringWindow loadStringWindow = null;

        // The brush with an image for copying to clipboard. Used for the button.
        private readonly ImageBrush iconBrush = null;

        // The default buttons brush, default transparent.
        private readonly Brush defaultBrush = Brushes.Transparent;

        // File name of the binary file containing the unique string
        private const string uniqueStringFileName = "uniqueStringID.bin";

        // The unique string that database connects this application data to. Received from cloud when internet connection is found for application.
        private string uniqueStringID = "";

        // The unique string text error message instead if for some reason could not be generated or received.
        private const string uniqueStringError = "Unique string could not be received";

        // The unique string text message instead while acquiring the unique string.
        private const string gettingUniqueStringMessage = "Acquiring unique string ID...";

        // bool for indicating if currenlty has a unique string ID or not
        private bool bHasUniqueString = false;

        // bool for indicating if currently is trying to obtain the unique string ID
        private bool bGettinUniqueString = true;

        // Bool value for stating if the copy button was pressed. So that when leaving mouseover event can rename the tooltip.
        private bool bCopied = false;

        // Bool value for stating if the unlock button was pressed.
        private bool bUnlocked = false;

        // The string to be shown in tooltip when copy to clipboard button has been pressed.
        private const string copyBTNToolTipCopiedMessage = "Copied!";

        // The string to be shown in tooltip when copy to clipboard button has not yet been pressed.
        private const string copyBTNToolTipNormalMessage = "Copy to clipboard";

        // The string to be shown in tooltip when unlock unique string button is hovered.
        private const string unlockBTNToolTipNormalMessage = "Click to unlock ID for application removal";

        // The default background for the unlock ID button
        private readonly ImageBrush buttonBackgroundDefaultBrush = null;

        // The disabled button background for unlock ID button
        private readonly ImageBrush buttonBackgroundDisabledBrush = null;

        // This struct holds variables for few settings for tooltip that will be adjusted runtime
        private struct FCustomToolTipSettings
        {
            public System.Windows.Controls.Primitives.PlacementMode placementMode;
            public double horizontalOffset;
            public double clickedHorizontalOffset;
            public double verticalOffset;
            public HorizontalAlignment horizontalAlignment;
        }

        private FCustomToolTipSettings copyBTNCustomToolTipSettings;

        private FCustomToolTipSettings unlockBTNCustomToolTipSettings;

        public bool HasUniqueString()
        {
            return bHasUniqueString;
        }

        public string GetUniqueString()
        {
            return uniqueStringID;
        }

        /// <summary>
        /// In the .ctor the tooltip settings for both copy ID and unlock ID buttons are initialized
        /// And the ID is tried to be aquired by calling method AquireUniqueIDString.
        /// </summary>
        /// <param name="inApp"></param>
        /// <param name="inDelayedMessageFunctionality"></param>
        /// <param name="inLogFunctionality"></param>
        public UniqueStringIDFunctionality(App inApp, DelayedMessageFunctionality inDelayedMessageFunctionality, ILogFunctionality inLogFunctionality)
        {
            app = inApp;
            delayedMessageFunctionality = inDelayedMessageFunctionality;
            logFunctionality = inLogFunctionality;
            app.MainWindowCreatedEvent += new App.MainWindowEventHandler(OnWindowCreated);
            app.MainWindowDestroyedEvent += new App.MainWindowEventHandler(UnbindWindowEvents);


            // Try to aquire the unique ID
            AquireUniqueIDString();

            // Prepare an iconbrush for button by loading the png image 
            iconBrush = new ImageBrush(BitmapFrame.Create(new System.Uri("pack://application:,,,/" + "Resources/CopyIcon.png")));
            buttonBackgroundDefaultBrush = new ImageBrush(BitmapFrame.Create(new System.Uri("pack://application:,,,/" + "Resources/UnlockIDIcon.png")));
            buttonBackgroundDisabledBrush = new ImageBrush(BitmapFrame.Create(new System.Uri("pack://application:,,,/" + "Resources/UnlockedIDIcon.png")));

            // Initialize custom tooltip settings
            copyBTNCustomToolTipSettings.placementMode = System.Windows.Controls.Primitives.PlacementMode.Relative;
            copyBTNCustomToolTipSettings.horizontalOffset = -30.0f;
            copyBTNCustomToolTipSettings.clickedHorizontalOffset = -5.0f;
            copyBTNCustomToolTipSettings.verticalOffset = 40.0f;
            copyBTNCustomToolTipSettings.horizontalAlignment = HorizontalAlignment.Center;

            // Initialize custom tooltip settings
            unlockBTNCustomToolTipSettings.placementMode = System.Windows.Controls.Primitives.PlacementMode.Relative;
            unlockBTNCustomToolTipSettings.horizontalOffset = -100.0f;
            unlockBTNCustomToolTipSettings.clickedHorizontalOffset = -100.0f;
            unlockBTNCustomToolTipSettings.verticalOffset = 40.0f;
            unlockBTNCustomToolTipSettings.horizontalAlignment = HorizontalAlignment.Center;

        }

        /// <summary>
        /// OnWindowCreated bind the UIElement events
        /// If no unique ID still and not currently trying get one try to aquire one again on window launch
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

            // Bind the hover events for the textbox containing the uniquestringid
            if (mainWindow.TBo_UniqueStringID != null)
            {
                mainWindow.TBo_UniqueStringID.MouseEnter += new MouseEventHandler(OnUniqueStringMouseEnter);
                mainWindow.TBo_UniqueStringID.MouseLeave += new MouseEventHandler(OnUniqueStringMouseLeave);
            }

            // Bind the hover and click events for the button responsible of copying uniquestringid to clipboard.
            // And set tooltip text to normal message and button icon hidden by defaultbrush
            if (mainWindow.BTN_CopyUniqueStringID != null)
            {
                mainWindow.BTN_CopyUniqueStringID.MouseEnter += new MouseEventHandler(OnUniqueStringMouseEnter);
                mainWindow.BTN_CopyUniqueStringID.MouseLeave += new MouseEventHandler(OnUniqueStringMouseLeave);
                mainWindow.BTN_CopyUniqueStringID.Click += new RoutedEventHandler(OnCopyButtonClick);
                mainWindow.BTN_CopyUniqueStringID.Background = defaultBrush;

                if (mainWindow.BTN_CopyUniqueStringID.ToolTip != null && mainWindow.BTN_CopyUniqueStringID.ToolTip is ToolTip)
                {
                    var toolTip = (ToolTip)mainWindow.BTN_CopyUniqueStringID.ToolTip;
                    toolTip.Content = copyBTNToolTipNormalMessage;
                }

            }

            // Bind hover event and click event on unlock unique string button, and set tooltiptext
            if (mainWindow.BTN_UnlockUniqueStringID != null)
            {
                mainWindow.BTN_UnlockUniqueStringID.MouseEnter += new MouseEventHandler(OnUnlockButtonMouseEnter);
                mainWindow.BTN_UnlockUniqueStringID.MouseLeave += new MouseEventHandler(OnUnlockButtonMouseLeave);
                mainWindow.BTN_UnlockUniqueStringID.Click += new RoutedEventHandler(OnUnlockButtonClick);

                if (mainWindow.BTN_UnlockUniqueStringID.ToolTip != null && mainWindow.BTN_UnlockUniqueStringID.ToolTip is ToolTip)
                {
                    var toolTip = (ToolTip)mainWindow.BTN_UnlockUniqueStringID.ToolTip;
                    toolTip.Content = unlockBTNToolTipNormalMessage;
                }

            }

            if(bHasUniqueString == false && bGettinUniqueString == false)
            {
                AquireUniqueIDString();
            }



            ShowUniqueIDText();

            if (logFunctionality != null)
                logFunctionality.ClickerLog("UniqueStringIDFunctionality was initialized", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// Unbind all the UIelement events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="inMainWindow"></param>
        public void UnbindWindowEvents(object sender, MainWindow inMainWindow)
        {
            if (inMainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("inMainWindow was null when trying to unbind events", EClickerLogVerbosity.Warning);
                return;
            }

            if (inMainWindow.TBo_UniqueStringID != null)
            {
                inMainWindow.TBo_UniqueStringID.MouseEnter -= OnUniqueStringMouseEnter;
                inMainWindow.TBo_UniqueStringID.MouseLeave -= OnUniqueStringMouseLeave;
            }

            if (inMainWindow.BTN_CopyUniqueStringID != null)
            {
                inMainWindow.BTN_CopyUniqueStringID.MouseEnter -= OnUniqueStringMouseEnter;
                inMainWindow.BTN_CopyUniqueStringID.MouseLeave -= OnUniqueStringMouseLeave;
                inMainWindow.BTN_CopyUniqueStringID.Click -= OnCopyButtonClick;
            }

            if (inMainWindow.BTN_UnlockUniqueStringID != null)
            {
                inMainWindow.BTN_UnlockUniqueStringID.MouseEnter -= OnUnlockButtonMouseEnter;
                inMainWindow.BTN_UnlockUniqueStringID.MouseLeave -= OnUnlockButtonMouseLeave;
                inMainWindow.BTN_UnlockUniqueStringID.Click -= OnUnlockButtonClick;
            }

            mainWindow = null;

            if (logFunctionality != null)
                logFunctionality.ClickerLog("Window events for uniquestringidfunctionality were unbound", EClickerLogVerbosity.Verbose);
        }


        // In this function unique string is looked from local computer if not found then it asks user to input a previous one if had one else makes one to database.

        /// <summary>
        /// AquireUniqueIDString will try to aquire a unique ID, if found from local file then it will confirm it is a valid ID
        /// </summary>
        private async void AquireUniqueIDString()
        {
            if (app == null)
            {
                return;
            }

            bGettinUniqueString = true;
            string foundUniqueString = DataSaveAndLoader.ReadDataFromBinary<String>(DataSaveAndLoader.EDataReadTypes.TypeString, uniqueStringFileName);
            bool bRequireNew = true;

            // Verify locally found ID
            if (foundUniqueString != null)
            {
                bRequireNew = await VerifyLocalID(foundUniqueString);    
            }
           
            // If requires a new ID try ask user to load one, if not then try get completely new from database
            if (bRequireNew)
            {
                while (true)
                {
                    loadStringWindow = new LoadUniqueStringWindow();
                    loadStringWindow.ShowDialog();

                    if (loadStringWindow.bLoadUniqueString && loadStringWindow.uniqueIDString.Length > 0)
                    {
                        if(!await VerifyGivenID(loadStringWindow.uniqueIDString))
                        {
                            break;
                        }
                    }
                    else
                    {
                        AquireNewID();
                        break;
                    }
                }

            }

            bGettinUniqueString = false;
            ShowUniqueIDText();
        }

        /// <summary>
        /// VerifyLocalID takes in the locally found ID string and sends a request to verify it.
        /// </summary>
        /// <param name="foundID"></param>
        /// <returns>Returns a bool value indicating if needs a new ID or not</returns>
        private async Task<bool> VerifyLocalID(string foundID)
        {
            var result = await app.GetServiceProvider().GetRequiredService<IConnectivityFunctionality>().IsUniqueIDAttached(foundID);
            if (result.status == EConnectionReturnStatus.PreConnectionFailed)
            {
                if(delayedMessageFunctionality != null)
                {
                    delayedMessageFunctionality.RequestMessage("ID couldn't be verified right now. Connection couldn't be established: \n" + result.message);
                }
                return false;
            }
            else if (result.status == EConnectionReturnStatus.Success)
            {
                if (result.content)
                {
                    InitUniqueString(foundID);
                }
                else
                {
                    MessageBoxResult msgResult = MessageBox.Show("Given ID is not attached to application. If ID was entered correctly, do you want to delete the ID file and try load it on next application launch?", "ATTENTION", MessageBoxButton.YesNo);
                    if (msgResult == MessageBoxResult.Yes)
                    {
                        DataSaveAndLoader.DeleteFile(uniqueStringFileName);
                        app.Shutdown();
                    }
                }
                return false;
            }
            else
            {
                if (delayedMessageFunctionality != null)
                {
                    delayedMessageFunctionality.RequestMessage("ID couldn't be verified right now: \n" + result.message);
                }
                return false;
            }
        }

        /// <summary>
        /// VerifyGivenID is run when user tried to load an ID at first application start when no ID was found locally.
        /// The given ID will be checked from database if it exists and is unlocked for this application to lock and own it.
        /// 
        /// </summary>
        /// <param name="givenID"></param>
        /// <returns>Returns false if no more needing to try to get an ID and returns true if if still needs an ID because this loading failed</returns>
        private async Task<bool> VerifyGivenID(string givenID)
        {
            var result = await app.GetServiceProvider().GetRequiredService<IConnectivityFunctionality>().LoadUniqueID(givenID);

            if (result.status == EConnectionReturnStatus.PreConnectionFailed)
            {
                MessageBox.Show("Given ID couldn't be verified right now: \n" + result.message);
                return false;
            }
            else if (result.status == EConnectionReturnStatus.Success)
            {
                InitUniqueString(givenID);
                SaveUniqueIDString();
                return false;
            }
            else
            {
                MessageBox.Show("Given ID couldn't be found!\n" + result.message);
                return true;
            } 
        }

        /// <summary>
        /// AquireNewID will try to get a completely new ID for user.
        /// </summary>
        private async void AquireNewID()
        {
            var result = await app.GetServiceProvider().GetRequiredService<IConnectivityFunctionality>().GetNewUniqueID();

            if (result.status == EConnectionReturnStatus.PreConnectionFailed)
            {
                if (delayedMessageFunctionality != null)
                {
                    delayedMessageFunctionality.RequestMessage("Failed to connect to server, new ID couldn't be received right now. \n" + result.message);
                }  
            }

            else if (result.status == EConnectionReturnStatus.Success)
            {
                InitUniqueString(result.content);
                SaveUniqueIDString();
                ShowUniqueIDText();
            }

            else
            {
                if (delayedMessageFunctionality != null)
                {
                    delayedMessageFunctionality.RequestMessage("Could not receive a new unique string right now! \n" + result.message);
                }
               
            }

        }

        /// <summary>
        /// InitUniqueString is called when an ID has been seen as valid and acquired. 
        /// It will also call the UniqueStringIDReadied event for each class depending on the uniqueID to get going.
        /// And lastly shows the unique id in its box
        /// </summary>
        /// <param name="inUniqueString"></param>
        private void InitUniqueString(string inUniqueString)
        {
            uniqueStringID = inUniqueString;
            bHasUniqueString = true;
            if (app != null)
            {
                app.UniqueStringIDReadied(uniqueStringID);
            }
            ShowUniqueIDText();
        }


        private void ShowUniqueIDText()
        {
            if (mainWindow != null && mainWindow.TBo_UniqueStringID != null)
            {

                if (bGettinUniqueString)
                {
                    mainWindow.TBo_UniqueStringID.Text = gettingUniqueStringMessage;
                }
                else
                {
                    string textToShow = bHasUniqueString ? uniqueStringID : uniqueStringError;
                    mainWindow.TBo_UniqueStringID.Text = textToShow;
                }

            }
        }

        /// <summary>
        /// SaveUniqueIDString is used to save the valid unique ID to local file as binary.
        /// </summary>
        private void SaveUniqueIDString()
        {
            DataSaveAndLoader.SaveDataAsBinary<String>(uniqueStringID, DataSaveAndLoader.EDataReadTypes.TypeString, uniqueStringFileName);
            if (logFunctionality != null)
                logFunctionality.ClickerLog("uniquestringid was saved to file", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// OnUniqueStringMouseenter is used both for the ID textbox and button
        /// Called when mouse enters either UIElement
        /// It handles displaying the copy ID button and tooltip for the button when mouse is over either UIElement
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void OnUniqueStringMouseEnter(object Sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when mouse entered uielement", EClickerLogVerbosity.Warning);
                return;
            }

            // If tooltip text was changed earlier then change back to normal text.
            if (bCopied && mainWindow.BTN_CopyUniqueStringID != null && mainWindow.BTN_CopyUniqueStringID.ToolTip is ToolTip)
            {
                bCopied = false;
                var castToolTip = (ToolTip)mainWindow.BTN_CopyUniqueStringID.ToolTip;
                castToolTip.Content = copyBTNToolTipNormalMessage;
            }

            // If the mouse entered the textbox area then just show the copy button icon
            if (mainWindow.TBo_UniqueStringID != null && Sender.Equals(mainWindow.TBo_UniqueStringID))
            {
                if (mainWindow.BTN_CopyUniqueStringID != null)
                {
                    mainWindow.BTN_CopyUniqueStringID.Background = iconBrush;
                }
            }

            //If mouse entered the button area, then show button icon but also show tooltip
            if (mainWindow.BTN_CopyUniqueStringID != null && Sender.Equals(mainWindow.BTN_CopyUniqueStringID))
            {

                if (mainWindow.BTN_CopyUniqueStringID.ToolTip != null && mainWindow.BTN_CopyUniqueStringID.ToolTip is ToolTip)
                {

                    var castToolTip = (ToolTip)mainWindow.BTN_CopyUniqueStringID.ToolTip;
                    castToolTip.PlacementTarget = (UIElement)mainWindow.BTN_CopyUniqueStringID;
                    castToolTip.IsOpen = true;
                    castToolTip.Placement = copyBTNCustomToolTipSettings.placementMode;
                    castToolTip.HorizontalOffset = copyBTNCustomToolTipSettings.horizontalOffset;
                    castToolTip.VerticalOffset = copyBTNCustomToolTipSettings.verticalOffset;
                    castToolTip.HorizontalAlignment = copyBTNCustomToolTipSettings.horizontalAlignment;

                }

                if (mainWindow.BTN_CopyUniqueStringID != null)
                {
                    mainWindow.BTN_CopyUniqueStringID.Background = iconBrush;
                }

            }

        }

        /// <summary>
        /// OnUniqueStringMouseLeave handles also both the ID's textbox and button mouse over events
        /// Here it will hide the button icon and tooltip when mouse leaving
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void OnUniqueStringMouseLeave(object Sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when mouse leaving uielement", EClickerLogVerbosity.Warning);
                return;
            }


            // If mouse left the textbox area, then set the button icon hidden by setting brush to the defaultbrush
            if (mainWindow.TBo_UniqueStringID != null && Sender.Equals(mainWindow.TBo_UniqueStringID))
            {

                if (mainWindow.BTN_CopyUniqueStringID != null)
                {
                    mainWindow.BTN_CopyUniqueStringID.Background = defaultBrush;
                }

            }


            // If mouse left the button area then hide the tooltip and hide the button icon by setting brush to the defaultbrush
            if (mainWindow.BTN_CopyUniqueStringID != null && Sender.Equals(mainWindow.BTN_CopyUniqueStringID))
            {

                if (mainWindow.BTN_CopyUniqueStringID.ToolTip != null && mainWindow.BTN_CopyUniqueStringID.ToolTip is ToolTip)
                {
                    var castToolTip = (ToolTip)mainWindow.BTN_CopyUniqueStringID.ToolTip;
                    castToolTip.IsOpen = false;
                }

                if (mainWindow.BTN_CopyUniqueStringID != null)
                {
                    mainWindow.BTN_CopyUniqueStringID.Background = defaultBrush;
                }

            }

        }

        /// <summary>
        /// OnCopyButtonClick is called when the ID copy button is pressed
        /// It will try to set the unique string ID to clipboard, if it fails it will display a messagebox which can be copied.
        /// Then it also changes the currently displayed tooltip text to "copied"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCopyButtonClick(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when clicking copy ID button", EClickerLogVerbosity.Warning);
                return;
            }


            // If the copy uniquestringid button was clicked, then copy the unique string to clipboard, and change tooltip text to notify it was copied to clipboard.
            if (mainWindow.BTN_CopyUniqueStringID != null)
            {
                
                try
                {
                    Clipboard.SetDataObject(uniqueStringID);
                }
                catch
                {
                    MessageBox.Show("Couldn't copy ID to clipboard, please copy this window text CTRL + C or from the ID box directly yourself \n" + uniqueStringID);
                }


                if (mainWindow.BTN_CopyUniqueStringID.ToolTip is ToolTip)
                {
                    var castToolTip = (ToolTip)mainWindow.BTN_CopyUniqueStringID.ToolTip;
                    castToolTip.Content = copyBTNToolTipCopiedMessage;
                    castToolTip.HorizontalOffset = copyBTNCustomToolTipSettings.clickedHorizontalOffset;
                    bCopied = true;
                }

                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Copy uniqueid button was clicked", EClickerLogVerbosity.Verbose);
            }

        }

        /// <summary>
        /// OnUnlockButtonClick is called when the unique ID unlock button is pressed
        /// This will disable the button while it tries to unlock the ID by contacting the database through connectivityfunctionality class
        /// And finally will try to copy the ID to clipboard and show a messagebox if it was successfully unlocked and then exit the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnUnlockButtonClick(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when clicking unlock ID button", EClickerLogVerbosity.Warning);
                return;
            }


            // If the unlock uniquestringid button was clicked, then unlock the unique string and change tooltip text to notify it was unlocked.
            if (mainWindow.BTN_UnlockUniqueStringID != null && !bUnlocked)
            {
                DisableUnlockButton();


                MessageBoxResult msgResult = MessageBox.Show("Are you sure you want to unlink your ID from this installation, meant for adding the ID to another computer or installation?", "ATTENTION", MessageBoxButton.YesNo);
                if (msgResult == MessageBoxResult.No)
                {
                    EnableUnlockButton();
                    return;
                }



                var result = await app.GetServiceProvider().GetRequiredService<IConnectivityFunctionality>().UnlockUniqueID(uniqueStringID);
                if (result.status == EConnectionReturnStatus.PreConnectionFailed)
                {
                    MessageBox.Show("Failed to connect to server, ID couldn't be unlocked right now. " + result.message);
                    EnableUnlockButton();
                }

                else if (result.status == EConnectionReturnStatus.Success)
                {

                    bHasUniqueString = false;
                    bUnlocked = true;

                    try
                    {
                        Clipboard.SetDataObject(uniqueStringID);
                    }
                    catch
                    {
                        MessageBox.Show("Couldn't copy ID to clipboard please copy it from this window when it is focused CTRL + C \n" + uniqueStringID);
                    }


                    DataSaveAndLoader.DeleteFile(uniqueStringFileName);
                    if (logFunctionality != null)
                        logFunctionality.ClickerLog("UniqueID was unlocked and id file was removed", EClickerLogVerbosity.Verbose);
                    MessageBox.Show("ID unlocked and copied to clipboard, you can proceed to remove this application and load the string in a new installation.");
                    app.Shutdown();
                }
                else
                {
                    MessageBox.Show("Failed to unlock ID in server side. " + result.message);
                    EnableUnlockButton();
                }


            }

            if (logFunctionality != null)
                logFunctionality.ClickerLog("unlock uniqueid button was clicked", EClickerLogVerbosity.Verbose);
        }

        /// <summary>
        /// OnUnlockButtonMouseEnter is called when mouse enters the unlock button
        /// The tooltip for the button is displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnlockButtonMouseEnter(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when mouse entered uielement", EClickerLogVerbosity.Warning);
                return;
            }

            //If mouse entered the button area, then show button icon but also show tooltip
            if (mainWindow.BTN_UnlockUniqueStringID != null)
            {

                if (mainWindow.BTN_UnlockUniqueStringID.ToolTip != null && mainWindow.BTN_UnlockUniqueStringID.ToolTip is ToolTip)
                {

                    var castToolTip = (ToolTip)mainWindow.BTN_UnlockUniqueStringID.ToolTip;
                    castToolTip.PlacementTarget = (UIElement)mainWindow.BTN_UnlockUniqueStringID;
                    castToolTip.IsOpen = true;
                    castToolTip.Placement = unlockBTNCustomToolTipSettings.placementMode;
                    castToolTip.HorizontalOffset = unlockBTNCustomToolTipSettings.horizontalOffset;
                    castToolTip.VerticalOffset = unlockBTNCustomToolTipSettings.verticalOffset;
                    castToolTip.HorizontalAlignment = unlockBTNCustomToolTipSettings.horizontalAlignment;

                }



            }



        }

        /// <summary>
        /// OnUnlockButtonMouseLeave handles when mouse leaves the unlock ID button
        /// And will hide the tooltip for the button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnlockButtonMouseLeave(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when mouse leaving uielement", EClickerLogVerbosity.Warning);
                return;
            }

            // If mouse left the button area then hide the tooltip
            if (mainWindow.BTN_UnlockUniqueStringID != null)
            {

                if (mainWindow.BTN_UnlockUniqueStringID.ToolTip != null && mainWindow.BTN_UnlockUniqueStringID.ToolTip is ToolTip)
                {
                    var castToolTip = (ToolTip)mainWindow.BTN_UnlockUniqueStringID.ToolTip;
                    castToolTip.IsOpen = false;
                }

            }

        }

        /// <summary>
        /// DisableUnlockButton will be called when button will be clicked for to stop being able to multiple times click it
        /// </summary>
        private void DisableUnlockButton()
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when trying to Disable unlock ID button", EClickerLogVerbosity.Verbose);
                return;
            }

            if (mainWindow.BTN_UnlockUniqueStringID != null)
            {
                mainWindow.BTN_UnlockUniqueStringID.IsEnabled = false;
                mainWindow.BTN_UnlockUniqueStringID.Background = buttonBackgroundDisabledBrush;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Unlock ID button disabled", EClickerLogVerbosity.Verbose);
            }
        }

        /// <summary>
        /// EnableUnlockButton will be called if unlocking the ID failed because if it didn't fail the app will close.
        /// </summary>
        private void EnableUnlockButton()
        {
            if (mainWindow == null)
            {
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("mainwindow was null when trying to enable unlock ID button", EClickerLogVerbosity.Verbose);
                return;
            }


            if (mainWindow.BTN_UnlockUniqueStringID != null)
            {
                mainWindow.BTN_UnlockUniqueStringID.IsEnabled = true;
                mainWindow.BTN_UnlockUniqueStringID.Background = buttonBackgroundDefaultBrush;
                if (logFunctionality != null)
                    logFunctionality.ClickerLog("Unlock ID button enabled", EClickerLogVerbosity.Verbose);
            }

        }

    }
}
