// Copyright 2021 Dennis Baeckstroem 

// function is used for styling a time integer 0-23 to string
// which looks a bit nicer with additional 0s.
// smallerScale indicates if :00 should be left away to shorten the size a bit.
function getHourIntervalStylized(inHour,smallerScale)
{
let stylizedHour = "";

if(inHour === undefined ||inHour === null)
{
    return stylizedHour;
}

if(inHour < 10)
{
    stylizedHour += "0";
}

stylizedHour += inHour.toString();

if(smallerScale)
{
    stylizedHour += " - ";
}
else 
{
    stylizedHour += ":00 - ";
}


if((inHour + 1) < 10)
{
    stylizedHour += "0";
}

stylizedHour += (inHour+1).toString();

if(!smallerScale)
{
    stylizedHour += ":00";
}


return stylizedHour;
};

export default getHourIntervalStylized;