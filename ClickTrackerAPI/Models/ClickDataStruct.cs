// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ClickTrackingAPI.Models
{
    /// <summary>
    /// ClickCountingFunctionality is the same class name as in the windows app where inside the clickdata classes reside
    /// </summary>
    [DataContract(Name = "CCF", Namespace = "")]
    public class ClickCountingFunctionality
    {
        /// <summary>
        /// FDayClickCount is exactly same as in the windows application so that it will load deserialize it into this from the xml string
        /// This has also the + operator overloaded so that existing data got from database can be combined with received data from windows application
        /// </summary>
        [DataContract(Name = "DCC", Namespace = "")]
        public class FDayClickCount
        {
            public FDayClickCount() { }

            // dayclickdata consists of a key for the hours 0-23 which then value itself contains the amount of clicks
            [DataMember]
            public SortedDictionary<ushort, FHourClickCount> dayClickData = new SortedDictionary<ushort, FHourClickCount>();


            public static FDayClickCount operator +(FDayClickCount a, FDayClickCount b)
            {
                if(a == null || b == null)
                {
                    return new FDayClickCount();
                }


                FDayClickCount newDayClickCount = new FDayClickCount();

                foreach (var hour in a.dayClickData.Keys)
                {
                    FHourClickCount foundHourClickCount = null;
                    a.dayClickData.TryGetValue(hour, out foundHourClickCount);

                    if(foundHourClickCount != null)
                    {
                        if(!newDayClickCount.dayClickData.ContainsKey(hour))
                        {
                            newDayClickCount.dayClickData.Add(hour, foundHourClickCount);
                        }
                        else
                        {
                            newDayClickCount.dayClickData[hour] = foundHourClickCount;
                        }
                    }
                }

                foreach (var hour in b.dayClickData.Keys)
                {
                    FHourClickCount foundHourClickCount = null;
                    b.dayClickData.TryGetValue(hour, out foundHourClickCount);

                    if (foundHourClickCount != null)
                    {
                        if (!newDayClickCount.dayClickData.ContainsKey(hour))
                        {
                            newDayClickCount.dayClickData.Add(hour, foundHourClickCount);
                        }
                        else
                        {
                            newDayClickCount.dayClickData[hour] = newDayClickCount.dayClickData[hour] + foundHourClickCount;
                        }
                    }

                }



                return newDayClickCount;
            }



        }

        /// <summary>
        /// FHourClickCount is also exactly the same as in the windows application
        /// It is the inner most clickdata class and stores the click counts itself in couple integers.
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

        /// <summary>
        /// FClickQueryData is the class that will be sent as JSON to react webpage querying for clickdata
        /// Contains a date for which date the clickdata belongs to
        /// And a string of xmldata containing the clicks for each hour recorded for that date
        /// </summary>
        public class FClickQueryData
        {
           public DateTime date;

           public string xmlClickData = "";

        }

    }
}