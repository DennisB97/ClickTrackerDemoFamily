// Copyright 2021 Dennis Baeckstroem 

// helper function ot return all dates between including the fromdate and endate
export const getAllDatesBetween = (fromDate,endDate) =>
{
    var allDates = [];
    var currentDate = new Date(fromDate);

    while(currentDate <= endDate)
    {
        allDates.push(new Date(currentDate));
        currentDate = addDays(currentDate,1);
    }

    return allDates;
}

// helper function to add n amount of days into js Date
function addDays(date, days) {
    var result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  // helper function to get total clicks for an array of clickdata
  export const getTotalClicks = (clickData,bLeftClicks) =>
  {
    var totalClicks = 0;

    clickData.forEach(data =>
        {
            data.hourClickData.forEach(hour => {
                if(bLeftClicks)
                {
                        totalClicks += hour.leftClicks;
                }
                else 
                {
                        totalClicks += hour.rightClicks;
                }

            })
        })
    

        return totalClicks;
  }

  // helper function to get clicks for one day of clickdata
  export const getTotalClicksOneDay = (clickData,bLeftClicks) =>
  {
    var totalClicks = 0;

    clickData.hourClickData.forEach(hour => {
        if(bLeftClicks)
        {
                totalClicks += hour.leftClicks;
        }
        else 
        {
                totalClicks += hour.rightClicks;
        }

    })

    return totalClicks;
  }

  // helper function to get clickdata from weeks array of clickdata
  export const getTotalClicksWeeks = (clickData,bLeftClicks) =>
  {
    var totalClicks = 0;

    clickData.forEach(data =>
        {
            totalClicks += getTotalClicks(data,bLeftClicks);
        });

    return totalClicks;
  }

  // helper function to get clickdata from months array of clickdata
  export const getTotalClicksMonths = (clickData,bLeftClicks) =>
  {
    var totalClicks = 0;

    clickData.forEach(data =>
        {
            totalClicks += getTotalClicksWeeks(data,bLeftClicks);
        });


    return totalClicks;
  }

  // helper function to get clickdata from years array of clickdata
  export const getTotalClicksYears = (clickData,bLeftClicks) =>
  {
    var totalClicks = 0;

    clickData.forEach(data =>
        {
            totalClicks += getTotalClicksMonths(data,bLeftClicks);
        });

        return totalClicks;
  }