// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows;
using System.Globalization;


namespace ClickTracker
{
    /// <summary>
    /// EConnectionReturnStatus enum is used inside return value of all method calls to API. 
    /// PreConnectionFailed for non server issue
    /// Success for successfully done the API call
    /// ServerSideFailed for failing inside the server for some reason
    /// </summary>
    public enum EConnectionReturnStatus
    {
        PreConnectionFailed,
        Success,
        ServerSideFailed
    }

    /// <summary>
    /// FConnectionReturn is the return value from method calls to connectionfunctionality handler
    /// status see above enum definition
    /// message is given in case of a failure which declares some kind of error message from the server or locally from exception.
    /// content is a T. This is a string for function that doesn't need it but those that need a return value this will be used and set whatever the function needs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct FConnectionReturn<T>
    {
        public FConnectionReturn(EConnectionReturnStatus returnStatus , string inMessage, T inContent)
        {
            status = returnStatus;
            message = inMessage;
            content = inContent;
        }
       
        public EConnectionReturnStatus status;
        public string message;
        public T content;
    }

    /// <summary>
    /// Interface for the methods, this interface and it's class is used with the ServiceCollection for a httpclient factory
    /// </summary>
    public interface IConnectivityFunctionality
    {
        /// <summary>
        /// LoadUniqueID is used to "load" a unique ID which means it will try to contact API and lock the given ID to this application&computer
        /// </summary>
        /// <param name="uniqueString">This is the given ID which should be tried to be locked to this application&computer</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is unused</returns>
        public Task<FConnectionReturn<string>> LoadUniqueID(string uniqueString);

        /// <summary>
        /// GetNewUniqueID is used to request a new unique ID which means it will try to contact API and ask for a new ID.
        /// </summary>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is also used and should contain the new string ID</returns>
        public Task<FConnectionReturn<string>> GetNewUniqueID();

        /// <summary>
        /// IsUniqueIDAttached is used to check a unique ID which means it will try to contact API and see if it is locked and matches this computer's product/deviceID.
        /// </summary>
        /// <param name="uniqueString">This is the given ID which should be checked</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is used and should contain a bool value indicating if the ID is locked or unlocked</returns>
        public Task<FConnectionReturn<bool>> IsUniqueIDAttached(string uniqueString);

        /// <summary>
        /// UnlockUniqueID is used to unlock a unique ID which means it will try to contact API and set the used state to false and erase the attached product/deviceID in case it matches this computer's.
        /// </summary>
        /// <param name="uniqueString">This is the given ID which should be tried to be unlocked</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is unused</returns>
        public Task<FConnectionReturn<string>> UnlockUniqueID(string uniqueString);

        /// <summary>
        /// SyncData is used to sync clickdata to database, it will contact API and post as XML.
        /// </summary>
        /// <param name="uniqueString">The given uniqueString id to try push the data to</param>
        /// <param name="dataDate">The date which the data was collected from</param>
        /// <param name="syncData">The clickdata for one day which will be sent</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is unused</returns>
        public Task<FConnectionReturn<string>> SyncData(string uniqueString, DateTime dataDate, ClickTracker.ClickCountingFunctionality.FDayClickCount syncData);

    }

    /// <summary>
    /// Uses Httpclient factory, this class is responsible of containing all required methods for communicating with the clicktracker API. 
    /// </summary>
    public class ConnectivityFunctionality : IConnectivityFunctionality
    {
        private readonly HttpClient httpClient;
        private readonly string machineID;
        private readonly string productID;
        private readonly App app;

        /// <summary>
        /// In the constructor the HttpClient is received automatically, the httpclient is setup, and from the registry the Window's machineID and productID is searched.
        /// </summary>
        /// <param name="inHttpClient"></param>
        public ConnectivityFunctionality(HttpClient inHttpClient)
        {
            httpClient = inHttpClient;
                                             // EXAMPLE CONNECTION URL TO API
            httpClient.BaseAddress = new Uri("https://localhost:44360/clicktracker/");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));



            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\SQMClient")) { 
                if (rk != null)
                {
                    object ID1 = rk.GetValue("MachineId");
                    if (ID1 != null && ID1 is String)
                    {
                        machineID = (string)(ID1);
                    }
                }
            }

            using (RegistryKey rk2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
            {
                if (rk2 != null)
                {
                    object ID2 = rk2.GetValue("ProductId");
                    if (ID2 != null && ID2 is String)
                    {
                        productID = (string)ID2;
                    }
                }
            }

            if (Application.Current != null)
            {
                if (Application.Current is App)
                {
                    app = (App)Application.Current;
                }
            }

        }

        /// <summary>
        /// Request handles sending the request to API with given parameters
        /// </summary>
        /// <typeparam name="T">T is used to specifiy return type for the content of FConnectionReturn which is tried to be received from the body of response</typeparam>
        /// <param name="method">Method is the HttpMethod used, if data is provided then a default PostAsXmlAsync is used instead and the HttpMethod is not used </param>
        /// <param name="url">url is the request Uri for the API call</param>
        /// <param name="data">data is optional and used when syncing data, if used it will contain one day of clickdata</param>
        /// <returns>This returns an FConnectionReturn<typeparamref name="T"/> with status and message is also used in case an error happened. content is used and set from the body of response if found and is of type <typeparamref name="T"/></returns>
        private async Task<FConnectionReturn<T>> Request<T>(HttpMethod method, string url,ClickTracker.ClickCountingFunctionality.FDayClickCount data = null)
        {
            // return if no httpclient for some reason
            if (httpClient == null)
            {
                return new FConnectionReturn<T>(EConnectionReturnStatus.PreConnectionFailed, "httpClient was null", default(T));
            }
            

            // Try to send to API and wait for a response
            HttpResponseMessage responseMessage;
            try
            {
                if(data != null)
                {
                    responseMessage = await httpClient.PostAsXmlAsync<ClickTracker.ClickCountingFunctionality.FDayClickCount>(url,data);
                }
                else
                {
                    var request = new HttpRequestMessage(method, url);
                    responseMessage = await httpClient.SendAsync(request);
                }
               
            }
            catch (Exception e)
            {
                return new FConnectionReturn<T>(EConnectionReturnStatus.PreConnectionFailed, e.Message,default(T));
            }

            // If success code then try load something from the body, and then return success.
            if (responseMessage.IsSuccessStatusCode)
            {
                T value = default(T);
                try
                {
                    using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
                    value = await JsonSerializer.DeserializeAsync<T>(responseStream);
                }
                catch
                {
                    //Nothing
                }
                return new FConnectionReturn<T>(EConnectionReturnStatus.Success, "",value);
            }
            // If API didn't return a success code then try read a string message from body and send that as message in FConnectionReturn with the status ServerSideFailed.
            else
            {
                string message = "";
                try
                {
                    using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
                    message = await JsonSerializer.DeserializeAsync<string>(responseStream);
                }
                catch
                {
                    message = responseMessage.ReasonPhrase != null ? responseMessage.ReasonPhrase : "";
                }

                return new FConnectionReturn<T>(EConnectionReturnStatus.ServerSideFailed, message,default(T));
            }

        }

        /// <summary>
        /// LoadUniqueID is used to "load" a unique ID which means it will try to contact API and lock the given ID to this application&computer
        /// </summary>
        /// <param name="uniqueString">This is the given ID which should be tried to be locked to this application&computer</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is unused</returns>
        public Task<FConnectionReturn<string>> LoadUniqueID(string uniqueString)
        {
            return Request<string>(HttpMethod.Put, "SOMEAPIROUTINGHERE1" + "?id=" + "SOMEAUTHORIZATIONIDHERE" + "&" + "uniqueString=" + uniqueString + "&" + "deviceID=" + machineID + "&" + "productID=" + productID);
        }
        /// <summary>
        /// GetNewUniqueID is used to request a new unique ID which means it will try to contact API and ask for a new ID.
        /// </summary>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is also used and should contain the new string ID</returns>
        public Task<FConnectionReturn<string>> GetNewUniqueID()
        {
            return Request<string>(HttpMethod.Post, "SOMEAPIROUTINGHERE2" + "?id=" + "SOMEAUTHORIZATIONIDHERE" + "&" + "deviceID=" + machineID + "&" + "productID=" + productID);   
        }
        /// <summary>
        /// IsUniqueIDAttached is used to check a unique ID which means it will try to contact API and see if it is locked and matches this computer's product/deviceID.
        /// </summary>
        /// <param name="uniqueString">This is the given ID which should be checked</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is used and should contain a bool value indicating if the ID is locked or unlocked</returns>
        public Task<FConnectionReturn<bool>> IsUniqueIDAttached(string uniqueString)
        {
            return Request<bool>(HttpMethod.Get, "SOMEAPIROUTINGHERE3" + "?id=" + "SOMEAUTHORIZATIONIDHERE" + "&" + "uniqueString=" + uniqueString + "&" + "deviceID=" + machineID + "&" + "productID=" + productID);
        }
        /// <summary>
        /// UnlockUniqueID is used to unlock a unique ID which means it will try to contact API and set the used state to false and erase the attached product/deviceID in case it matches this computer's.
        /// </summary>
        /// <param name="uniqueString">This is the given ID which should be tried to be unlocked</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is unused</returns>
        public Task<FConnectionReturn<string>> UnlockUniqueID(string uniqueString)
        {
            return Request<string>(HttpMethod.Put, "SOMEAPIROUTINGHERE4" + "?id=" + "SOMEAUTHORIZATIONIDHERE" + "&" + "uniqueString=" + uniqueString + "&" + "deviceID=" + machineID + "&" + "productID=" + productID);
        }
        /// <summary>
        /// SyncData is used to sync clickdata to database, it will contact API and post as XML.
        /// </summary>
        /// <param name="uniqueString">The given uniqueString id to try push the data to</param>
        /// <param name="dataDate">The date which the data was collected from</param>
        /// <param name="syncData">The clickdata for one day which will be sent</param>
        /// <returns>This returns an FConnectionReturn with status and message is also used in case an error happened. content is unused</returns>
        public Task<FConnectionReturn<string>> SyncData(string uniqueString, DateTime dataDate, ClickTracker.ClickCountingFunctionality.FDayClickCount syncData)
        {
            return Request<string>(HttpMethod.Post, "SOMEAPIROUTINGHERE5" + "?id=" + "SOMEAUTHORIZATIONIDHERE" + "&" + "uniqueString=" + uniqueString + "&" + "date=" + dataDate.ToString(CultureInfo.InvariantCulture) + "&" + "deviceID=" + machineID + "&" + "productID=" + productID, syncData);
        }
    }
}
