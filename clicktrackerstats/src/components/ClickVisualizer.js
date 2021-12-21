// Copyright 2021 Dennis Baeckstroem 
import {React,forwardRef, useImperativeHandle} from 'react';
import {useTooltip, useTooltipInPortal, defaultStyles } from '@visx/tooltip';
import getHourIntervalStylized from "../utils/getHourIntervalStylized";
import ClickGraph from './ClickGraph';
import { EGraphLayer } from './ClickDataVisualizer';
import { getTotalClicksOneDay, getTotalClicks, getTotalClicksWeeks,getTotalClicksMonths } from '../utils/clickDataParsinUtilities';




// ClickVisualizer component is the outer component which contains the svg graph component
// and handles the toolTip from the graph
export const ClickVisualizer = forwardRef((props,ref) =>
{
  
const ClickData = props.clickData;

const {
  tooltipOpen,
  tooltipTop,
  tooltipLeft,
  hideTooltip,
  showTooltip,
  tooltipData
} = useTooltip();


const tooltipStyles = {
  ...defaultStyles,
  minWidth: 60,
  minHeight: 60,
  backgroundColor: "rgba(0,0,0,0.9)",
  color: "white",
  zIndex: 45
};



const { containerRef, TooltipInPortal } = useTooltipInPortal();

const width = props.width;
const height = props.height;
var toolTipDateField = null;
var toolTipValueField = null;


useImperativeHandle(ref,() =>({

    requestTooltipHidden()
    {
      hideTooltip();
    }

}));


if(tooltipOpen && tooltipData)
{
  if(props.layer === EGraphLayer.hour)
  {
    toolTipDateField = getHourIntervalStylized(tooltipData.hour);
    toolTipValueField = props.bShowLeftClicks? tooltipData.leftClicks : tooltipData.rightClicks;
  }
 
  if(props.layer === EGraphLayer.day)
  {
    toolTipDateField = tooltipData.date.toLocaleDateString('en-GB');
    toolTipValueField = props.bShowLeftClicks? getTotalClicksOneDay(tooltipData,true) : getTotalClicksOneDay(tooltipData,false);
  }
  else if(props.layer === EGraphLayer.week)
  {
    toolTipDateField = tooltipData.at(0).date.toLocaleDateString('en-GB') + " - " + tooltipData.at(-1).date.toLocaleDateString('en-GB');
    toolTipValueField = props.bShowLeftClicks? getTotalClicks(tooltipData,true) : getTotalClicks(tooltipData,false);
  }
  else if(props.layer === EGraphLayer.month)
  {
    toolTipDateField = tooltipData.at(0).at(0).date.toLocaleDateString('en-GB') + " - " + tooltipData.at(-1).at(-1).date.toLocaleDateString('en-GB');
    toolTipValueField = props.bShowLeftClicks? getTotalClicksWeeks(tooltipData,true) : getTotalClicksWeeks(tooltipData,false);
  }
  else if(props.layer === EGraphLayer.year)
  {
    toolTipDateField = tooltipData.at(0).at(0).at(0).date.toLocaleDateString('en-GB') + " - " + tooltipData.at(-1).at(-1).at(-1).date.toLocaleDateString('en-GB');
    toolTipValueField = props.bShowLeftClicks? getTotalClicksMonths(tooltipData,true) : getTotalClicksMonths(tooltipData,false);
  }
}

function requestInnerGraph(data)
{
  if(props.layer !== EGraphLayer.hour)
  {
    hideTooltip();
    props.requestInnerGraph(data);
  }
}


if(width > 10 && ClickData != null && height > 0)
return(
  <div style={{ width: {width},height: {height}, margin: '0px',padding: '0px', display: 'flex', flexDirection: 'flex-column', background: 'transparent'}}>
  <ClickGraph requestInnerGraph={requestInnerGraph} layer={props.layer} hideTooltip={hideTooltip} showTooltip={showTooltip} clickData={ClickData} width={width} height={height} containerRef={containerRef} btoggleClickStats={props.bShowLeftClicks}></ClickGraph>
  {tooltipOpen && tooltipData && (
    <TooltipInPortal
    key={Math.random()}
    top={tooltipTop}
    left={tooltipLeft}
    style={tooltipStyles}>
      <div style={{ color: '#923F3F' }}>
          <strong>{props.bShowLeftClicks? "Left clicks" : "Right clicks"}</strong>
      </div>
      <div style={{ color: 'white' }}>
          <strong>{toolTipDateField}</strong>
      </div>
      <div><strong>{toolTipValueField}</strong></div>
    </TooltipInPortal>
  )}
  </div>
  );

else
{
  return null;
}

});

