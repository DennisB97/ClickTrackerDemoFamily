// Copyright 2021 Dennis Baeckstroem 
import {useSearchParams} from "react-router-dom";
import {useState, useEffect , useRef} from "react";
import styled from "styled-components";
import validator from 'validator';
import { Navigate } from 'react-router-dom';

import "react-datepicker/dist/react-datepicker.css";
import ClickDataDateQuery from "../components/ClickDataDateQuery";
import {ClickDataVisualizer} from "../components/ClickDataVisualizer";

const StyledOuterDiv = styled.div`
background: transparent;
padding-top: 50px;
align-self: center;
align-items: center;
margin: auto;
display: flex;
flex-direction: column;
height: 100%;
width: 100%;
overflow:hidden;

@media (max-height: 800px) {
  padding-top: 5px;
  }

`

export default function Stats()
{
    // graphContent hook is used for containing the jsx to be rendered
    const [graphContent, setGraphContent] = useState(null);
    // clickCalendar is hook used for containing the clickdata after it has been fetched through API
    const [clickCalendar,setClickCalendar] = useState(undefined);
    // bFirstRun is just used to skip the first time useeffect is run
    const bFirstRun = useRef(true);
    // In this useeffect the clickCalendar is depended on, so when it changes it will chech if it
    // has data, and if it does it will set the graphcontent
    useEffect(() => {
    
        if(bFirstRun.current)
        {
            bFirstRun.current = false;
            return;
        }

       if(clickCalendar !== undefined && clickCalendar.bHasData)
       {
        setGraphContent(<ClickDataVisualizer clearSearchData={clearSearchData} clickCalendar={clickCalendar}></ClickDataVisualizer>);
       }
       
      },[clickCalendar]);
   
      function clearSearchData()
      {
        setClickCalendar(undefined);
      }

      // Get the url params, so that ID can be checked
      const [searchParams] = useSearchParams();
      
      // Check that the found ID in URL is actually a valid GUID
      // else navigate back to front page with the ID inputter.
      var stringID = searchParams.get('stringID');
      if(!validator.isUUID(stringID))
      {
        return <Navigate to = "/"/>;
      }


      // if clickcalendar has data then show the graph, else show the date query
      var displayData;
      if(clickCalendar !== undefined && clickCalendar.bHasData)
      {
        displayData = graphContent;
      }
      else
      {
        displayData = <ClickDataDateQuery setClickCalendar={setClickCalendar}></ClickDataDateQuery>;
      }
    
    return(
        <StyledOuterDiv>
          {displayData}  
        </StyledOuterDiv>
    );
    
}
