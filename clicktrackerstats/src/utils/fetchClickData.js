// Copyright 2021 Dennis Baeckstroem 
import { getAllDatesBetween } from "./clickDataParsinUtilities";

function FindAmountClicks(index,xmlString, bSeekLeftClicks)
{
    if (window.DOMParser)
    {  
        
        var parser = new DOMParser();
        var xmlDoc = parser.parseFromString(xmlString, "application/xml");
       
       var keys = xmlDoc.getElementsByTagName("a:Key");
      
       for(var i = 0; i < keys.length; ++i)
       {
           if(parseInt(keys[i].textContent) === index)
           {
               if(bSeekLeftClicks)
               {
                return parseInt(keys[i].nextSibling.childNodes[0].textContent);
               }
              else {
                  return parseInt(keys[i].nextSibling.childNodes[1].textContent);
              }
           }
       }
      
        return 0;
    }
}

// function is used for connecting to API through fetch call
// to receive the clickdata between given startDate and endDate with stringID.
async function fetchClickData(startDate,endDate,stringID) {

    var clickCalendar = {
        years: [],
        months: [],
        weeks: [],
        days: [],
        day: undefined,
        bHasData: false
    }
    try {

    var allClickData = [];
    var fetchedClickData = [];
    if(process.env.REACT_APP_LOCALBUILD === 'false')
    {
        const url = `/.netlify/functions/queryClickData?uniqueString=${stringID}&startDate=${startDate.toLocaleString("en-US")}&endDate=${endDate.toLocaleString("en-US")}`;
        fetchedClickData = await fetch(url).then((res) => res.json());
    }
    else {
        const response = await fetch(process.env.REACT_APP_API_DEBUG_KEY + "uniqueString=" + stringID + "&" 
    + "startDate=" + startDate.toLocaleString("en-US") + "&" 
    + "endDate=" + endDate.toLocaleString("en-US"), {
        method: 'GET', 
        mode: 'cors', 
        headers: {
            'Content-Type': 'application/json'
        },
        });
        fetchedClickData = await response.json()
    }
    
    
    if(fetchedClickData.length < 1)
    {
        return allClickData;
    }
    
    // Get all days between the clickdata dates,
    // because clickdata might have skipped some days which didn't contain any clickdata
    var allDays = [];
    if(fetchedClickData.length > 1)
    {
        allDays = getAllDatesBetween(new Date(fetchedClickData.at(0).date),new Date(fetchedClickData.at(-1).date));
    }
    else if(fetchedClickData.length === 1)
    {
        allDays.push(new Date(fetchedClickData.at(0).date));
    }

    // Now loop through all days then access the clicks and collect
    // it into objects
    allDays.forEach(day =>{
        var clickDayData = {
            date: new Date(day),
            hourClickData: [],
        };

        var foundClickDay = fetchedClickData.find(data => 
            new Date(data.date).getTime() === day.getTime() 
            );

        
        for (var i = 0; i < 24; ++i)
        {

            var hourClick = {
                hour: i,
                leftClicks: 0,
                rightClicks: 0
            };
          
            if(foundClickDay !== undefined)
            {
                hourClick.leftClicks = FindAmountClicks(i,foundClickDay.xmlClickData,true);
                hourClick.rightClicks = FindAmountClicks(i,foundClickDay.xmlClickData,false);
            }

            clickDayData.hourClickData.push(hourClick);
        }

        
        allClickData.push(clickDayData);
    });

    
    // Bundle the clickdata days into days,weeks,months and years.
    // depending how many days there are
    var currentDays = [];
    var currentWeeks = [];
    var currentMonths = [];
    var currentYears = [];

    if(allClickData.length === 1)
    {
        clickCalendar.day = allClickData[0];
        clickCalendar.bHasData = true;
    }

    else if(allClickData.length > 1)
    {
        allClickData.forEach(data => {

        currentDays.push(data);
        if(currentDays.length === 7)
        {
            currentWeeks.push(currentDays);
            currentDays = [];
            if(currentWeeks.length === 4)
            {
                currentMonths.push(currentWeeks);
                currentWeeks = [];
                if(currentMonths.length === 12)
                {
                    currentYears.push(currentMonths);
                    currentMonths = [];
                }
            }
        }

        if(new Date(data.date).getTime() === new Date(allClickData.at(-1).date).getTime())
        {
            
            if(currentYears.length > 0)
            {
                if(currentDays.length > 0)
                {
                    currentWeeks.push(currentDays);
                    currentDays = [];
                }

                if(currentWeeks.length > 0)
                {
                    currentMonths.push(currentWeeks);
                    currentWeeks = [];
                }

                if(currentMonths.length > 0)
                {
                    currentYears.push(currentMonths);
                    currentMonths = [];
                }
            }

            else if(currentMonths.length > 0)
            {
                if(currentDays.length > 0)
                {
                    currentWeeks.push(currentDays);
                    currentDays = [];
                }

                if(currentWeeks.length > 0)
                {
                    currentMonths.push(currentWeeks);
                    currentWeeks = [];
                }

            }

            else if(currentWeeks.length > 0)
            {
                if(currentDays.length > 0)
                {
                    currentWeeks.push(currentDays);
                    currentDays = [];
                }
            }

        }
        });

        // Lastly check the amount of data and add it into the clickCalendar
        if(currentDays.length > 0)
        {
            clickCalendar.days = currentDays;
            clickCalendar.bHasData = true;
        }
        else if(currentWeeks.length > 0)
        {
            clickCalendar.weeks = currentWeeks;
            clickCalendar.bHasData = true;
        }
        else if(currentMonths.length > 0)
        {
            clickCalendar.months = currentMonths;
            clickCalendar.bHasData = true;
        }
        else if(currentYears.length > 0)
        {
            clickCalendar.years = currentYears;
            clickCalendar.bHasData = true;
        }


    }
   

}
 catch(err)
 {
     return clickCalendar;
 }   
    return clickCalendar;
  }
  export default fetchClickData;