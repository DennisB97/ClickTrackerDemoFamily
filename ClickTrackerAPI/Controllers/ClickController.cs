// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using ClickTrackingAPI.Models;


namespace ClickTrackingAPI.Controllers
{
    /// <summary>
    /// The main controller for clicktracker wpf application and stats webpage
    /// </summary>
    [RoutePrefix("clicktracker")]
    public class ClickController : ApiController
    {
        /// <summary>
        /// GetUniqueStringCheck is an GET call for checking if provided uniqueString is valid and in use
        /// </summary>
        /// <param name="id">Is an additional step to avoid direct calls without knowing the hash value</param>
        /// <param name="uniqueString">Is the user's unique ID which will be compared in database if found or not. And if the given ID is in use or is not.</param>
        /// <param name="deviceID">Is the user's windows device ID compared with the device ID found attached to given user ID</param>
        /// <param name="productID">Is the user's windows product ID compared with the product ID found attached to given user ID</param>
        /// <param name="androidID">Is not in use atm.</param>
        /// <returns>If found returns if the given ID is in use or not</returns>
        [HttpGet]
        [Route("SOMEAPIROUTINGHERE3")]
        public IHttpActionResult GetUniqueStringCheck(string id, string uniqueString, string deviceID = "", string productID = "", string androidID = "")
        {
            //If hash value doesn't match then return unauthorized
            if (id != System.Configuration.ConfigurationManager.AppSettings["SecurityHash"])
            {
                return Unauthorized();
            }

            try
            {
                using (ClickTrackerDBContext dBContext = new ClickTrackerDBContext())
                {
                    Guid userID;
                    if (Guid.TryParse(uniqueString, out userID))
                    {
                        // Try find from database userdata that matches the one with given uniquestring
                        UserData foundData = dBContext.UserDatas.FirstOrDefault(e => e.UniqueStringID == userID);

                        if (foundData != null)
                        {
                            // If any of the additional IDs match then it is a valid ID
                            if (deviceID != "" && deviceID == foundData.WinDeviceID)
                            {
                                return Ok(foundData.InUse);
                            }
                            else if (productID != "" && productID == foundData.WinProductID)
                            {
                                return Ok(foundData.InUse);
                            }
                            else if (androidID != "" && androidID == foundData.AndroidID)
                            {
                                return Ok(foundData.InUse);
                            }
                            else
                            {
                                return NotFound();
                            }
                        }

                    }

                    return NotFound();
                }
            }
            // Catch any exception regarding database connection
            catch (Exception e)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(string.Format("Something went wrong with trying to get ID from database" + e.Message)),
                    ReasonPhrase = "ID query failed"
                };
                throw new HttpResponseException(resp);
            }
        }

        /// <summary>
        /// PutUniqueStringOwnerShip is a PUT call where a given uniqueString id is tried to be locked to an application installation
        /// </summary>
        /// <param name="id">Is an additional step to avoid direct calls without knowing the hash value</param>
        /// <param name="uniqueString">Is the user's unique ID which will be tried to be locked if it isn't already locked or if it even exists at all.</param>
        /// <param name="deviceID">Is the user's windows device ID compared with the device ID found attached to given user ID</param>
        /// <param name="productID">Is the user's windows product ID compared with the product ID found attached to given user ID</param>
        /// <param name="androidID">Is not in use atm.</param>
        /// <returns>returns 200 OK if it was able to lock the given ID</returns>
        [HttpPut]
        [Route("SOMEAPIROUTINGHERE1")]
        public IHttpActionResult PutUniqueStringOwnerShip(string id, string uniqueString, string deviceID = "", string productID = "", string androidID = "")
        {

            //If hash value doesn't match then return unauthorized
            if (id != System.Configuration.ConfigurationManager.AppSettings["SecurityHash"])
            {
                return Unauthorized();
            }

            bool bUniqueStringAttached = false;

            using (ClickTrackerDBContext dBContext = new ClickTrackerDBContext())
            {
                Guid userID;
                if (Guid.TryParse(uniqueString, out userID))
                {
                    // Try find from database userdata that matches the one with given uniquestring
                    UserData foundData = dBContext.UserDatas.FirstOrDefault(e => e.UniqueStringID == userID);

                    if (foundData != null)
                    {
                        // Only if not in use atm then it can be set to in use.
                        if (!foundData.InUse)
                        {
                            // Set the founddata in database as modified so it all gets saved when SaveChanges gets called
                            dBContext.Entry(foundData).State = EntityState.Modified;
                            foundData.InUse = true;

                            // If a device id was given then modify that field
                            if (deviceID != "")
                            {
                                foundData.WinDeviceID = deviceID;
                                bUniqueStringAttached = true;
                            }

                            // If a product id was given then modify that field
                            if (productID != "")
                            {
                                foundData.WinProductID = productID;
                                bUniqueStringAttached = true;
                            }

                            // If a android id was given then modify that field
                            if (androidID != "")
                            {
                                foundData.AndroidID = androidID;
                                bUniqueStringAttached = true;
                            }

                            //If some additional verify id was given then can save the changes to the database
                            if (bUniqueStringAttached)
                            {
                                dBContext.SaveChanges();
                            }


                        }
                    }

                }

            }

            if (bUniqueStringAttached)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }

        }


        /// <summary>
        /// PostNewUniqueString is a POST call that will make a new field in the userdata table in the database
        /// </summary>
        /// <param name="id">Is an additional step to avoid direct calls without knowing the hash value</param>
        /// <param name="deviceID">Is the user's windows device ID compared with the device ID found attached to given user ID</param>
        /// <param name="productID">Is the user's windows product ID compared with the product ID found attached to given user ID</param>
        /// <param name="androidID">Is not in use atm.</param>
        /// <returns> If successfully will return the created unique string ID/GUID together with OK 200.</returns>
        [HttpPost]
        [Route("SOMEAPIROUTINGHERE2")]
        public IHttpActionResult PostNewUniqueString(string id, string deviceID = "", string productID = "", string androidID = "")
        {

            //If hash value doesn't match then return unauthorized
            if (id != System.Configuration.ConfigurationManager.AppSettings["SecurityHash"])
            {
                return Unauthorized();
            }

            bool bDeviceIDAttached = false;
            string generatedUniqueString = "";

            using (ClickTrackerDBContext dBContext = new ClickTrackerDBContext())
            {
                UserData newUserData = new UserData();
                newUserData.InUse = true;
                newUserData.DateCreated = DateTime.Now;


                if (deviceID != "")
                {
                    newUserData.WinDeviceID = deviceID;
                    bDeviceIDAttached = true;
                }
                if (productID != "")
                {
                    newUserData.WinProductID = productID;
                    bDeviceIDAttached = true;
                }
                if (androidID != "")
                {
                    newUserData.AndroidID = androidID;
                    bDeviceIDAttached = true;
                }

                // An additional verification ID must be given else won't be able to create a new unique string ID.
                if (bDeviceIDAttached)
                {
                    try
                    {
                        UserData addedData = dBContext.UserDatas.Add(newUserData);
                        dBContext.SaveChanges();

                        if (addedData != null)
                        {
                            generatedUniqueString = addedData.UniqueStringID.ToString();
                        }

                    }

                    catch (Exception ex)
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent(string.Format("Something went wrong with trying to create a new user in database" + ex.Message)),
                            ReasonPhrase = "User adding failed"
                        };
                        throw new HttpResponseException(resp);
                    }

                }

            }

            if (bDeviceIDAttached && generatedUniqueString != "")
            {
                return Ok(generatedUniqueString);
            }
            else
            {
                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(string.Format("Something went wrong with trying to create a new user in database")),
                    ReasonPhrase = "User adding failed"
                };
                throw new HttpResponseException(resp);
            }

        }

        /// <summary>
        /// PutRemoveUniqueStringOwnerShip is a PUT call, is used for unlocking given unique string ID if possible
        /// </summary>
        /// <param name="id">Is an additional step to avoid direct calls without knowing the hash value</param>
        /// <param name="uniqueString">Is the user's unique ID which will be tried to be unlocked if it isn't already unlocked or if it even exists at all.</param>
        /// <param name="deviceID">Is the user's windows device ID compared with the device ID found attached to given user ID</param>
        /// <param name="productID">Is the user's windows product ID compared with the product ID found attached to given user ID</param>
        /// <param name="androidID">Is not in use atm.</param>
        /// <returns>Returns OK 200 if was successfully unlocked, else Not found 404</returns>
        [HttpPut]
        [Route("SOMEAPIROUTINGHERE4")]
        public IHttpActionResult PutRemoveUniqueStringOwnerShip(string id, string uniqueString, string deviceID = "", string productID = "", string androidID = "")
        {

            //If hash value doesn't match then return unauthorized
            if (id != System.Configuration.ConfigurationManager.AppSettings["SecurityHash"])
            {
                return Unauthorized();
            }

            bool bUniqueStringUnlocked = false;

            using (ClickTrackerDBContext dBContext = new ClickTrackerDBContext())
            {
                Guid userID;
                if (Guid.TryParse(uniqueString, out userID))
                {
                    // Try find from database userdata that matches the one with given uniquestring
                    var foundData = dBContext.UserDatas.FirstOrDefault(e => e.UniqueStringID == userID);

                    if (foundData != null)
                    {
                        // Only if currently id then try to unlock it if additional verification ID was given that matches with the database values
                        if (foundData.InUse)
                        {
                            dBContext.Entry(foundData).State = EntityState.Modified;

                            foundData.InUse = false;

                            if (deviceID != "" && deviceID == foundData.WinDeviceID)
                            {
                                foundData.WinDeviceID = null;
                                bUniqueStringUnlocked = true;
                            }

                            if (productID != "" && productID == foundData.WinProductID)
                            {
                                foundData.WinProductID = null;
                                bUniqueStringUnlocked = true;
                            }

                            if (androidID != "" && androidID == foundData.AndroidID)
                            {
                                foundData.AndroidID = null;
                                bUniqueStringUnlocked = true;
                            }

                            if (bUniqueStringUnlocked)
                            {
                                dBContext.SaveChanges();
                            }


                        }
                    }

                }

            }

            if (bUniqueStringUnlocked)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }

        }


        /// <summary>
        /// PostNewSyncData is a POST call, which is used when clickdata wants to be synced to database from the windows application
        /// </summary>
        /// <param name="id">Is an additional step to avoid direct calls without knowing the hash value</param>
        /// <param name="uniqueString">Is the user's unique ID which will be used to confirm possibilty of syncing data and syncing to correct location.</param>
        /// <param name="date">Date is for which date the data is from, so it can be added or the existing data for that data can be retrieved and added to</param>
        /// <param name="deviceID">Is the user's windows device ID compared with the device ID found attached to given user ID</param>
        /// <param name="productID">Is the user's windows product ID compared with the product ID found attached to given user ID</param>
        /// <param name="androidID">Is not in use atm.</param>
        /// <returns>Returns OK 200 if was successfully done else possibly Not found 404</returns>
        [HttpPost]
        [Route("SOMEAPIROUTINGHERE5")]
        public async Task<IHttpActionResult> PostNewSyncData(string id, string uniqueString, string date, string deviceID = "", string productID = "", string androidID = "")
        {

            //If hash value doesn't match then return unauthorized
            if (id != System.Configuration.ConfigurationManager.AppSettings["SecurityHash"])
            {
                return Unauthorized();
            }

            bool bDataSynced = false;

            using (ClickTrackerDBContext dBContext = new ClickTrackerDBContext())
            {
                Guid userID;
                if (Guid.TryParse(uniqueString, out userID))
                {
                    bool bValidToPushData = false;
                    // Try find from database userdata that matches the one with given uniquestring
                    UserData foundData = dBContext.UserDatas.FirstOrDefault(e => e.UniqueStringID == userID);

                    if (foundData != null && foundData.InUse)
                    {
                        if (deviceID != "" && deviceID == foundData.WinDeviceID)
                        {
                            bValidToPushData = true;
                        }

                        else if (productID != "" && productID == foundData.WinProductID)
                        {
                            bValidToPushData = true;
                        }

                        else if (androidID != "" && androidID == foundData.AndroidID)
                        {
                            bValidToPushData = true;
                        }

                        // If the given id was seen to be valid to be able to sync data then it can proceed
                        if (bValidToPushData)
                        {
                            bool bPreviousWasFound = false;
                            ClickData newClickData = null;
                            DateTime syncDate = new DateTime(1999, 1, 1);
                            if (dBContext.ClickDatas != null)
                            {
                                //Make the given string date to a DateTime
                                try
                                {
                                    syncDate = DateTime.Parse(date, CultureInfo.InvariantCulture);
                                }
                                catch
                                {
                                    var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                    {
                                        Content = new StringContent(string.Format("Something went wrong trying to parse date")),
                                        ReasonPhrase = "DateTime parse exception"
                                    };
                                    throw new HttpResponseException(resp);
                                }

                                //Try to query any data from database where userid matches and date matches
                                try
                                {
                                    newClickData = dBContext.ClickDatas.Where(x => DbFunctions.TruncateTime(x.ClickedDate) == syncDate.Date && x.UserID == foundData.UserID).FirstOrDefault();
                                }
                                catch
                                {
                                    var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                    {
                                        Content = new StringContent(string.Format("Something went wrong with trying to get previous clickdata with date and userid")),
                                        ReasonPhrase = "Clickdata where query fail"
                                    };
                                    throw new HttpResponseException(resp);
                                }

                            }

                            // If any previous data was found, then the xml string content from the database to be deserialized
                            ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount oldSyncData = null;
                            if (newClickData != null)
                            {
                                dBContext.ClickDatas.Attach(newClickData);
                                oldSyncData = DeserializeString(newClickData.ClickedData);
                                bPreviousWasFound = true;
                            }
                            // If no previous sync data found for the date, then it will create a new clickdata object and fill that
                            else
                            {
                                newClickData = new ClickData();
                                newClickData.UserID = foundData.UserID;
                                newClickData.ClickedDate = syncDate;
                            }

                            // Next it will try to make the body content into a string
                            string xmlString = "";
                            try
                            {
                                using (var stream = await Request.Content.ReadAsStreamAsync())
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    using (var sr = new StreamReader(stream))
                                    {
                                        xmlString = await sr.ReadToEndAsync();
                                    }
                                }
                            }
                            catch
                            {
                                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                {
                                    Content = new StringContent(string.Format("Something went wrong with trying to get string xmlstring from request content ")),
                                    ReasonPhrase = "Request content read failed"
                                };
                                throw new HttpResponseException(resp);
                            }

                            // And will call function to deserialize that content to a similar class which exists in the windows application
                            ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount newSyncData = DeserializeString(xmlString);

                            // Then it will add if old sync data exists the new one together through operator overloaded additioning between the two classes.
                            try
                            {
                                if (newSyncData != null && oldSyncData != null)
                                {
                                    newSyncData = newSyncData + oldSyncData;
                                }
                            }

                            catch
                            {
                                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                {
                                    Content = new StringContent(string.Format("Something went wrong with trying to add old syncdata to newsyncdata ")),
                                    ReasonPhrase = "FDayClickCount addition failed"
                                };
                                throw new HttpResponseException(resp);
                            }

                            // Here it will then serialize the data finally to xml string
                            string newXMLData = DataContractSerializeTo(newSyncData);

                            // And added to the 
                            newClickData.ClickedData = newXMLData;

                            // If it will add new dataobject into database then do add
                            if (!bPreviousWasFound)
                            {
                                dBContext.ClickDatas.Add(newClickData);
                            }
                            // Finally save the changes
                            try
                            {
                                dBContext.SaveChanges();
                            }
                            catch
                            {
                                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                {
                                    Content = new StringContent(string.Format("Something went wrong with trying to savechanges to database ")),
                                    ReasonPhrase = "Database save failed"
                                };
                                throw new HttpResponseException(resp);
                            }

                            bDataSynced = true;

                        }



                    }

                }

            }

            if (bDataSynced)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }

        }



        /// <summary>
        /// GetClickData is a GET call, which is used in the stats webpage. It will query data between two dates with the given uniquestring ID.
        /// </summary>
        /// <param name="uniqueString">Is the user's unique ID which data will be tried to be found and retrieved between the given dates.</param>
        /// <param name="startDate">Only data from this date to enddate would be included in the search</param>
        /// <param name="endDate">Only data from startDate to this date would be included in the search</param>
        /// <returns></returns>
        [HttpGet]
        [Route("SOMEAPIROUTINGHERE6")]
        public IHttpActionResult GetClickData(string uniqueString, string startDate, string endDate)
        {

            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            // Try parse the string dates
            try
            {
                start = DateTime.Parse(startDate, CultureInfo.InvariantCulture);
                end = DateTime.Parse(endDate, CultureInfo.InvariantCulture);
            }
            catch
            {
                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(string.Format("Something went wrong with trying to parse dates ")),
                    ReasonPhrase = "DateTime parse failure"
                };
                throw new HttpResponseException(resp);
            }

            using (ClickTrackerDBContext dBContext = new ClickTrackerDBContext())
            {
                Guid userID;
                if (Guid.TryParse(uniqueString, out userID))
                {
                    // Try find from database userdata that matches the one with given uniquestring
                    UserData foundData = dBContext.UserDatas.FirstOrDefault(e => e.UniqueStringID == userID);

                    if (foundData != null)
                    {
                        // Make the query from clickdatas where the data is between the dates given, then select and create a new class which can then be sent back in the body as JSON
                        var clickCollection = (from u in dBContext.ClickDatas
                                               where u.UserID == foundData.UserID && DbFunctions.TruncateTime(u.ClickedDate) >= start.Date && DbFunctions.TruncateTime(u.ClickedDate) <= end.Date
                                               select new ClickTrackingAPI.Models.ClickCountingFunctionality.FClickQueryData
                                               {
                                                   date = u.ClickedDate,
                                                   xmlClickData = u.ClickedData
                                               }).ToList();


                        return Ok(clickCollection);

                    }

                }

            }


            return NotFound();
        }


        /// <summary>
        /// DeserializeString is used for taking a string containing xml, and trying to deserialize that into a class
        /// </summary>
        /// <param name="xmlString">Is the string containing clickdata as xml</param>
        /// <returns>Returns the deserialized class</returns>
        private ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount DeserializeString(string xmlString)
        {
            object clickObject = null;
            using (Stream stream = new MemoryStream())
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xmlString);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(typeof(ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount));

                try
                {
                    clickObject = deserializer.ReadObject(stream);
                }

                catch
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent(string.Format("Something went wrong with trying deserialize xmlstring {0}", xmlString)),
                        ReasonPhrase = "Deserialization failed"
                    };
                    throw new HttpResponseException(resp);
                }

            }

            ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount receivedDayClickCount = null;
            try
            {
                receivedDayClickCount = (ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount)clickObject;
            }
            catch
            {
                return null;
            }


            return receivedDayClickCount;
        }

        /// <summary>
        /// DataContractSerializeTo is used when class needs to be serialized into xml before submitting it into the database xml field in clickdatas.
        /// </summary>
        /// <param name="data">data is the class object containing clickdata for one day</param>
        /// <returns>Returns the serialized string containing the xml</returns>
        public static string DataContractSerializeTo(ClickTrackingAPI.Models.ClickCountingFunctionality.FDayClickCount data)
        {
            try
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    var serializer = new DataContractSerializer(data.GetType());
                    serializer.WriteObject(memStream, data);

                    memStream.Seek(0, SeekOrigin.Begin);

                    using (var streamReader = new StreamReader(memStream))
                    {
                        string xmlString = streamReader.ReadToEnd();
                        return xmlString;
                    }
                }
            }

            catch
            {
                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(string.Format("Something went wrong with trying to serialize FDayClickCount to xml string ")),
                    ReasonPhrase = "XML string serialization failed"
                };
                throw new HttpResponseException(resp);
            }


        }

    }
}
