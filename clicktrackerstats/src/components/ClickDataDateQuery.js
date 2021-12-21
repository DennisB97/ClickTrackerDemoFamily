// Copyright 2021 Dennis Baeckstroem 
import {useState, useEffect, useRef} from "react";
import DatePicker from "react-datepicker";
import styled from "styled-components";
import {useSearchParams} from "react-router-dom";
import appendSeachParams from "../utils/appendSearchParams";
import fetchClickData from "../utils/fetchClickData";
import React from 'react';

const StyledOuterDiv = styled.div`
background: transparent;
display: flex;
flex-direction: column;
overflow: hidden;
margin: auto;
padding: 0px;
z-index: 99;
flex-grow: 0;
align-items: center;
align-content: center;
justify-content: center;
`

const StyledDiv = styled.div`
display: flex;
flex-direction: row;
align-items: center;
background: transparent;



@media (max-width: 800px)
{
    flex-direction: column;
}

`
const StyledGrid = styled.div`
display:grid;
position: relative;
grid-template-columns: repeat(2, 1fr); 
grid-template-rows: repeat(2, 1fr); 
overflow: hidden;
width: 100%;

@media (max-width: 800px)
{
    grid-template-columns: repeat(1, 1fr); 
    grid-template-rows: repeat(4, 1fr); 
}
`

const StyledButton = styled.button`
height: 25px;
border-radius: 1px;
border-width: 1px;
margin: auto;
width: 100%;
cursor: pointer;

:disabled {
    background-color: #cfb5b5;
}

`
// ClickDataDateQuery component utilizes the datepicker package, for selecting the clickdata dates.
// It will add the dates into URL as params
// When button has been pressed for search it will try to fetch with the given dates,
// and while fetching it will change the state and state will show on the button text.
export default function ClickDataDateQuery(props)
{
    const initialState = {bSearching: false, buttonText: "Search"};

    const [{ bSearching, buttonText}, dispatch] = React.useReducer(
        reducer,
        initialState
      );

    const [searchParams, setSearchParams] = useSearchParams();
    const [startDate, setStartDate] = useState( searchParams.get('startDate') !== null ? getValidDate(new Date(JSON.parse(searchParams.get('startDate')))) : new Date());
    const [endDate, setEndDate] = useState( searchParams.get('endDate') !== null ? getValidDate(new Date(JSON.parse(searchParams.get('endDate'))))  : new Date());
   
    const today = new Date();

    const bFirstRun = useRef(true);
    useEffect(() =>{
        setSearchParams(appendSeachParams({startDate: JSON.stringify(startDate), endDate: JSON.stringify(endDate)},searchParams));

        if(bFirstRun.current)
        {
            bFirstRun.current = false;
            return;
        }
       

        },[startDate,endDate])



        function reducer(state, action) {
            switch (action.type) {
                case 'searching':
                {
                    return {bSearching: true, buttonText: "Searching..."};
                }
               
                case 'idle':
                {
                    return {bSearching: false, buttonText: "Search"};
                }
              
                case 'error':
                {
                    return {bSearching: false, buttonText: "Nothing found!"}
                }
            
              default:
                return state;
            }
          }
          


    function getValidDate(checkDate)
    {
        var today = new Date();
        var minDate = new Date(process.env.REACT_APP_FIRST_DATA_DATE);
       
        
        if(checkDate > today)
            {
                checkDate = today;
            }
        else if(checkDate < minDate)
            {
                checkDate = minDate;
            }

        return checkDate;
    }

    function QueryData()
    {
        dispatch({type: 'searching'});
    
       
        var fromDate = new Date();
        var toDate = new Date();
        var currentDate = new Date();

        if(searchParams.get('startDate') !== null && searchParams.get('endDate') !== null)
        {
            try{
                fromDate = new Date(JSON.parse(searchParams.get('startDate')));
                toDate = new Date(JSON.parse(searchParams.get('endDate')));
            }
            catch (e)
            {
                console.log(e.message);
            } 
        }

       var minDate = new Date(process.env.REACT_APP_FIRST_DATA_DATE);
      
       if(fromDate < minDate)
       {
           fromDate = minDate;
       }

       if(toDate > currentDate)
       {
           toDate = currentDate;
       }

       if(toDate < fromDate)
       {
           toDate = fromDate;
       }

      
        fetchClickData(fromDate,toDate,searchParams.get("stringID")).then(clickCalendar => {if(!clickCalendar.bHasData){dispatch({type: 'error'});}  props.setClickCalendar(clickCalendar);});   
    }
    
   
return(
    <StyledOuterDiv>
    <h1 style={{alignSelf: 'center' , color: 'white'}}>Date Selection</h1>
    <StyledGrid>
    <StyledButton disabled={bSearching} onClick={() => {setStartDate(getValidDate(new Date(today.getFullYear(),today.getMonth(),today.getDate()-6))); setEndDate(new Date());}}>Last 7 Days</StyledButton>
    <StyledButton disabled={bSearching} onClick={() => {setStartDate(getValidDate(new Date(today.getFullYear(),today.getMonth(),today.getDate()-13))); setEndDate(new Date());}}>Last 14 Days</StyledButton>
    <StyledButton disabled={bSearching} onClick={() => {setStartDate(getValidDate(new Date(today.getFullYear(),today.getMonth(),1))); setEndDate(new Date());}}>Current Month</StyledButton>
    <StyledButton disabled={bSearching} onClick={() => {setStartDate(getValidDate(new Date(today.getFullYear(),0,1))); setEndDate(new Date());}}>Current Year</StyledButton>
    </StyledGrid>
    <StyledDiv>
    <DatePicker disabled={bSearching} showYearDropdown  
    selectsStart startDate={startDate} endDate={endDate} selected={startDate} onChange={(date) => {setStartDate(date)}}></DatePicker>
    <DatePicker disabled={bSearching}  showYearDropdown  
    selectsEnd startDate={startDate} endDate={endDate}   selected={endDate} onChange={(date) => {setEndDate(date)}}></DatePicker>
    </StyledDiv>
    <button disabled={bSearching} style={{padding: '0px',width: '200px',height: '30px', display: 'flex',flexDirection: 'row' , alignContent: 'center' , borderRadius: '3px' ,justifyContent: 'center'}} onClick={() => QueryData()} type="button" className="btn btn-secondary"> {buttonText}</button>
    </StyledOuterDiv>
    );
}

