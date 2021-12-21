// Copyright 2021 Dennis Baeckstroem 
import React from 'react';
import { ParentSize } from '@visx/responsive';
import GraphNavigationPanel from './GraphNavigationPanel';
import styled from "styled-components";
import {useEffect,useRef} from "react";
import GraphStats from './GraphStats';
import {ClickVisualizer} from "./ClickVisualizer";

const StyledOuterDiv = styled.div`
background: transparent;
align-items: center;
justify-content: center;
display: flex;
flex-direction: column;
width: 100%;
height: 90%;
margin: auto;
padding: 0px;
`


const StyledInnerDiv = styled.div`
background: transparent;
margin: auto;
display: flex;
flex-direction: column;
align-items: center;
justify-content: center;
height: 100%;
width: 70%;
padding: 0px;
position: relative;
border: 3px solid #000 !important;
border-spacing: 0px;
border-top-left-radius: 10px; 
border-Bottom-Left-Radius: 10px;
border-Top-Right-Radius: 10px;
border-Bottom-Right-Radius: 10px; 
overflow:show;
@media (max-width: 800px) {
    height: 100%;
    width: 100%;
  }
`

// EGraphLayer works as a enum for checking which graph layer/scope is being used
export class EGraphLayer {
  
  static hour = new EGraphLayer("hour")
  static day = new EGraphLayer("day")
  static week = new EGraphLayer("week")
  static month = new EGraphLayer("month")
  static year = new EGraphLayer("year")

  constructor(name) {
    this.name = name
  }
}

// Helper function to get the next graph layer based on the current one
// Used when a dispatch forward has been made by clicking a bar in the graph
function getInnerGraphLayer(currentLayer)
{

  // Hour layer is already inner most layer
  if(currentLayer === EGraphLayer.hour)
  {
    return EGraphLayer.hour;
  }
  else if(currentLayer === EGraphLayer.day)
  {
    return EGraphLayer.hour;
  }
  else if(currentLayer === EGraphLayer.week)
  {
    return EGraphLayer.day;
  }
  else if(currentLayer === EGraphLayer.month)
  {
    return EGraphLayer.week;
  }
  else if(currentLayer === EGraphLayer.year)
  {
    return EGraphLayer.month;
  }
}

// ClickDataVisualizer component keeps the graph navigation bar
// graph component itself
// and graph stats component
export function ClickDataVisualizer(props)
{
  var outerMostLayer = null;
  const clickCalendar = props.clickCalendar;
  var allClickData = [];
  const childRef = useRef();
  if(clickCalendar.years.length > 0)
  {
    outerMostLayer = EGraphLayer.year;
    allClickData = clickCalendar.years;
  }
  else if(clickCalendar.months.length > 0)
  {
    outerMostLayer = EGraphLayer.month;
    allClickData = clickCalendar.months;
  }
  else if(clickCalendar.weeks.length > 0)
  {
    outerMostLayer = EGraphLayer.week;
    allClickData = clickCalendar.weeks;
  }
  else if(clickCalendar.days.length > 0)
  {
    outerMostLayer = EGraphLayer.day;
    allClickData = clickCalendar.days;
  }
  else 
  {
    outerMostLayer = EGraphLayer.hour;
    allClickData = clickCalendar.day;
  }

  
    const initialState = {bStatsOverlay: false,bShowLeftClicks: true,layerChain: [{layer: outerMostLayer, clickData: allClickData}]};
  

    const [{bStatsOverlay,bShowLeftClicks,layerChain}, dispatch] = React.useReducer(
        reducer,
        initialState
      );


    function reducer(state, action) {
      switch (action.type) {
          case 'backwards':
          {
            var newChain = [];

            for (var i = 0; i < state.layerChain.length - 1; ++i)
            {
              newChain.push({layer: state.layerChain[i].layer,clickData: state.layerChain[i].clickData});
            }  
           
              if(newChain.length < 1)
              {
                props.clearSearchData([]);
                return initialState;
              }

              return {bStatsOverlay: state.bStatsOverlay,bShowLeftClicks: state.bShowLeftClicks ,layerChain: newChain};
          }
         
          case 'forward':
          {
            var updatedChain = [];

            for (var x = 0; x < state.layerChain.length; ++x)
            {
              updatedChain.push({layer: state.layerChain[x].layer,clickData: state.layerChain[x].clickData});
            }  

            var newLayer = getInnerGraphLayer(updatedChain.at(-1).layer);
            var newClickData = action.clickData;

            var newChainData = {layer: newLayer, clickData: newClickData};
            updatedChain.push(newChainData);
              return {bStatsOverlay: state.bStatsOverlay,bShowLeftClicks: state.bShowLeftClicks , layerChain: updatedChain};
          }
        
          case 'showStats':
          {
              return {bStatsOverlay: !state.bStatsOverlay,bShowLeftClicks: state.bShowLeftClicks , layerChain: state.layerChain}
          }

          case 'clickSwitch':
          {
            return {bStatsOverlay: state.bStatsOverlay,bShowLeftClicks: !state.bShowLeftClicks , layerChain: state.layerChain}
          }
      
          case 'reset':
            {
              return initialState;
            }

        default:
          return state;
      }
    }


    function goToPrevious()
    {
      childRef.current.requestTooltipHidden();
      dispatch({type: 'backwards'});
    }

    function switchClickStats()
    {
      dispatch({type: 'clickSwitch'});
    }

    function showStats()
    {
      dispatch({type: 'showStats'});
    }
    
    var dateViewText = "";

    if(layerChain.at(-1).layer === EGraphLayer.hour)
    {
        dateViewText = layerChain.at(-1).clickData.date.toLocaleDateString('en-GB');
    }
    else if(layerChain.at(-1).layer === EGraphLayer.day)
    {
        dateViewText =layerChain.at(-1).clickData.at(0).date.toLocaleDateString('en-GB') + " - " + layerChain.at(-1).clickData.at(-1).date.toLocaleDateString('en-GB');
    }
    else if(layerChain.at(-1).layer === EGraphLayer.week)
    {
      dateViewText = layerChain.at(-1).clickData.at(0).at(0).date.toLocaleDateString('en-GB') + " - " + layerChain.at(-1).clickData.at(-1).at(-1).date.toLocaleDateString('en-GB');
    }
    else if(layerChain.at(-1).layer === EGraphLayer.month)
    {
      dateViewText = layerChain.at(-1).clickData.at(0).at(0).at(0).date.toLocaleDateString('en-GB') + " - " + layerChain.at(-1).clickData.at(-1).at(-1).at(-1).date.toLocaleDateString('en-GB');
    }
    else if(layerChain.at(-1).layer === EGraphLayer.year)
    {
      dateViewText = layerChain.at(-1).clickData.at(0).at(0).at(0).at(0).date.toLocaleDateString('en-GB') + " - " + layerChain.at(-1).clickData.at(-1).at(-1).at(-1).at(-1).date.toLocaleDateString("en-GB");
    }

    function requestInnerGraph(innerClickData)
    {
      dispatch({type: 'forward',clickData: innerClickData});
    }

    useEffect(() => {
    dispatch({type: 'reset'});
    },[]);
    
   

        return(
          <StyledOuterDiv>
          <GraphNavigationPanel statsToggle={showStats} backwards={goToPrevious} leftRightToggle={switchClickStats}></GraphNavigationPanel>
          <StyledInnerDiv>
            <div style={{height: '100%', width: '100%'}}>
            <ParentSize className="graph-container" debounceTime={0}>
            {({ width: visWidth, height: visHeight }) => (
              <>
              <strong style={{zIndex: 40 , position: 'absolute', color: '#923F3F', left: visWidth/3}}>{dateViewText}</strong>
               <ClickVisualizer ref={childRef} layer={layerChain.at(-1).layer} requestInnerGraph={requestInnerGraph} bShowLeftClicks={bShowLeftClicks} width={visWidth} height={visHeight} clickData={layerChain.at(-1).clickData}></ClickVisualizer>
              { bStatsOverlay && (<GraphStats width={visWidth} height={visHeight} layer={layerChain.at(-1).layer} clickData={layerChain.at(-1).clickData}></GraphStats>) }
              </>
            )}
          </ParentSize>
          </div>
          </StyledInnerDiv>
          </StyledOuterDiv>
            );
    
   
   
}