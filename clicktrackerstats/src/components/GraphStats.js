// Copyright 2021 Dennis Baeckstroem 
import styled from 'styled-components';
import {useState} from 'react';
import { EGraphLayer } from "./ClickDataVisualizer";
import {getTotalClicksOneDay, getTotalClicks,getTotalClicksWeeks, getTotalClicksMonths, getTotalClicksYears} from '../utils/clickDataParsinUtilities';
import moment from 'moment';


const StyledGrid = styled.div`
display:flex;
position: absolute;
grid-template-columns: repeat(2, 1fr); 
grid-template-rows: repeat(1, 1fr); 
background-Color: rgba(30, 30, 30, 0.9);
z-index: 99;
left: 0px;
top: 0px;
`
const GridDiv = styled.div`
display:flex;
flex-direction: column;
z-index: 100;
padding: 25px;
`
const UnsortedList = styled.ul`
z-index: 100;
position: relative;
list-Style: none;
background-Color: 'transparent';
width: 100%;
max-width: 300px;
`
const ListItem = styled.li`
color: white;
z-index: 100;
margin: 10px;
width: 100%;
display:block;
padding-top: 20px;
`
// This function is used for getting the mouselifetime stats
// averageclicks is the total click count for current graph scope
// days is the amount of days the total click count was from
// the ratedlifetime is the user given mouse switch lifetime in days
// the mouseage is the user given mouseage in days, at least more than current graph scope days
function calculateRemainingMouseLifeTime(averageClicks,days,ratedLifeTime,mouseAge)
{
    var returnValue = {mouseHealth: "0%", clicksDone: 0, clicksLeft: 0};

    if(days > 0 && ratedLifeTime > 0 && mouseAge > 0)
    {
        const averageClicksPerDay = averageClicks / days;
        var clicksDone = mouseAge * averageClicksPerDay;
        var clicksLeft = ratedLifeTime - clicksDone;
        var lifeLeft = "0%";
        if(clicksLeft < 0) 
        {
            clicksLeft = 0;
        }
        else {
            lifeLeft = ((clicksLeft / ratedLifeTime) * 100).toFixed(2).toString() + "%";
        }
        
       
        returnValue.mouseHealth = lifeLeft;
        returnValue.clicksDone = clicksDone;
        returnValue.clicksLeft = clicksLeft;
    }

    return returnValue;
}

// Graphstats component is used for showing stats such as total click
// and calculating about how long mouse lifetime is left
export default function GraphStats(props)
{

    var leftData = [];
    var rightData = [];
    // received clickdata is all the clickdata in current graph scope
    const clickData = props.clickData;
    const [switchLifeTime, setSwitchLifeTime] = useState(sessionStorage.getItem('switchLifetime'));
    const [mouseAge,setMouseDay] = useState(sessionStorage.getItem('mouseAge'));

    rightData.push(<>
   <ListItem  key="switchLifeTimeInput">
    <strong style={{color: '#923F3F'}}>Enter your mouse switches rated click lifetime </strong>
    <input style={{width: '130px'}} value={switchLifeTime} placeholder="Enter rated clicks" type="number"  onChange={e => {
    setSwitchLifeTime(e.target.value);
    sessionStorage.setItem('switchLifetime',e.target.value);
    }}>
    </input>
    </ListItem>
    <ListItem style={{maxWidth: '200px'}} key="mouseAgeInput">
    <strong style={{color: '#923F3F'}}>Enter how many days old is your mouse </strong>
    <input style={{width: '80px'}} value={mouseAge} placeholder="Enter mouse age in days" type="number"  onChange={e => {
    setMouseDay(e.target.value);
    sessionStorage.setItem('mouseAge',e.target.value);
    }}>
    </input>
    </ListItem>
    </>);
   
    var totalLeftClicks = 0;
    var totalRightClicks = 0;
    var totalDays = 0;
    var startDate = new Date();
    var endDate = new Date();
    if(props.layer === EGraphLayer.hour)
    {
        totalLeftClicks = getTotalClicksOneDay(clickData,true);
        totalRightClicks = getTotalClicksOneDay(clickData,false);
        totalDays = 1;
    }
    else if(props.layer === EGraphLayer.day)
    {
        totalLeftClicks = getTotalClicks(clickData,true);
        totalRightClicks = getTotalClicks(clickData,false);
        startDate = moment(clickData.at(0).date);
        endDate = moment(clickData.at(-1).date);
        totalDays = endDate.diff(startDate,'days') + 1;
    }
    else if(props.layer === EGraphLayer.week)
    {
        totalLeftClicks = getTotalClicksWeeks(clickData,true);
        totalRightClicks = getTotalClicksWeeks(clickData,false);
        startDate = moment(clickData.at(0).at(0).date);
        endDate = moment(clickData.at(-1).at(-1).date);
        totalDays = endDate.diff(startDate,'days') + 1;
    }
    else if(props.layer === EGraphLayer.month)
    {
        totalLeftClicks = getTotalClicksMonths(clickData,true);
        totalRightClicks = getTotalClicksMonths(clickData,false);
        startDate = moment(clickData.at(0).at(0).at(0).date);
        endDate = moment(clickData.at(-1).at(-1).at(0).date);
        totalDays = endDate.diff(startDate,'days') + 1;
    }
    else if(props.layer === EGraphLayer.year)
    {
        totalLeftClicks = getTotalClicksYears(clickData,true);
        totalRightClicks = getTotalClicksYears(clickData,false);
        startDate = moment(clickData.at(0).at(0).at(0).at(0).date);
        endDate = moment(clickData.at(-1).at(-1).at(-1).date);
        totalDays = endDate.diff(startDate,'days') + 1;
    }


    if(totalDays < 0)
    {
        totalDays *= -1;
    }

    leftData.push(<ListItem key="totalleftclicks" style={{maxWidth: '150px'}}><strong style={{color: '#923F3F'}}>Total Left Clicks:  </strong><strong style={{fontSize: '14px'}}>{totalLeftClicks}</strong> </ListItem>);
    leftData.push(<ListItem key="totalrightclicks" style={{maxWidth: '150px'}}><strong style={{color: '#923F3F'}}>Total Right Clicks:  </strong><strong style={{fontSize: '14px'}}>{totalRightClicks}</strong> </ListItem>);


    if(mouseAge < totalDays)
    {
        setMouseDay(totalDays);
    }

    var mouseLeftLifeTime = calculateRemainingMouseLifeTime(totalLeftClicks,totalDays,switchLifeTime,mouseAge);
    var mouseRightLifeTime = calculateRemainingMouseLifeTime(totalRightClicks,totalDays,switchLifeTime,mouseAge);
  
   if(mouseLeftLifeTime.clicksDone > 1 || mouseRightLifeTime.clicksDone > 1)
   {
    
    rightData.push(<>
        <ListItem>
        <strong style={{color: '#923F3F'}}>Estimated lifetime left: </strong>
        </ListItem>
        </>)
        rightData.push(<>
        <ListItem key="leftClickLifeTimeRemaining" style={{maxWidth: '100px'}}><strong style={{color: '#923F3F'}}>Left switch:  </strong><strong style={{fontSize: '14px'}}>{mouseLeftLifeTime.mouseHealth}</strong> </ListItem>
        <ListItem key="rightClickLifeTimeRemaining" style={{maxWidth: '100px'}}><strong style={{color: '#923F3F'}}>Right switch:  </strong><strong style={{fontSize: '14px'}}>{mouseRightLifeTime.mouseHealth}</strong> </ListItem>
        </>)
   }
   
    
   

    const height = props.height;
    const width = props.width;
    return (
    <StyledGrid>
        <GridDiv style={{width: width/2, height: height}}>
        <UnsortedList>
         {rightData}
        </UnsortedList>
        </GridDiv>
        <GridDiv style={{width: width/2, height: height}}>
        <UnsortedList>
        {leftData}
        </UnsortedList>
        </GridDiv>
    </StyledGrid>
    );
}