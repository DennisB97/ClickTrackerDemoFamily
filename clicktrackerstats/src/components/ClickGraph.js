// Copyright 2021 Dennis Baeckstroem 
import { scaleLinear, scaleBand } from '@visx/scale';
import { LinearGradient} from '@visx/gradient';
import { Group } from '@visx/group';
import { AxisLeft, AxisBottom } from '@visx/axis';
import { Bar } from '@visx/shape';
import { localPoint } from '@visx/event';
import {EGraphLayer} from './ClickDataVisualizer';
import { getTotalClicksOneDay,getTotalClicks, getTotalClicksWeeks, getTotalClicksMonths } from '../utils/clickDataParsinUtilities';
import getHourIntervalStylized from "../utils/getHourIntervalStylized";
import {useState,useEffect} from 'react';

// ClickGraph is the component utilizing visx package to display a graph
// Depending on the layer it will either display with year,month,week,days or one day graph
export default function ClickGraph(props)
{
    const height = props.height;
    const width = props.width;

    const clickData = props.clickData;
    var dataMap = clickData.hourClickData;
    let tooltipTimeout;

    const margin = {top: 30, bottom: 70, left: 55, right: 5};

    const xMax = width - margin.left - margin.right;
    const yMax = height - margin.top - margin.bottom;

    var xLabel = "Hours";

    var x = d => getHourIntervalStylized(d.hour);
    if(window.innerWidth < 1150)
    {
      x = d => getHourIntervalStylized(d.hour,true);
    }
    var yLeft = d => d.leftClicks;
    var yRight = d => d.rightClicks;
    
    var xScale = null;

    var maxLeft = 0;
    var maxRight = 0;
  
    if(props.layer === EGraphLayer.hour)
    {
      maxLeft = Math.max(...clickData.hourClickData.map(yLeft));
      maxRight = Math.max(...clickData.hourClickData.map(yRight));
      xScale = scaleBand({
        range: [0,xMax],
        round: true,
        domain: clickData.hourClickData.map(x),
        padding: 0.5
    });
    }
    else if(props.layer === EGraphLayer.day)
    {
      if(window.innerWidth < 800)
      {
        x = d => 
        {
          var dateString = d.date.toLocaleDateString('en-GB');
          return dateString.substring(0, 5) + " " + dateString.substring(6, dateString.length);
        }
      }
      else
      {
        x = d => d.date.toLocaleDateString('en-GB');
      }
      yLeft = d => getTotalClicksOneDay(d,true);
      yRight = d => getTotalClicksOneDay(d,false);
      xLabel = "Days";
    }

    else if(props.layer === EGraphLayer.week)
    {
      if(window.innerWidth < 800)
      {
        x = d => 
        {
          var firstDateString = d.at(0).date.toLocaleDateString('en-GB');
          var secondDateString = d.at(-1).date.toLocaleDateString('en-GB');
          return firstDateString.substring(0, 5) + " " + firstDateString.substring(6, firstDateString.length) + " - " + secondDateString.substring(0, 5) + " " + secondDateString.substring(6, secondDateString.length);
        }
      }
      else
      {
        x = d => d.at(0).date.toLocaleDateString('en-GB') + " - " + d.at(-1).date.toLocaleDateString('en-GB');
      }
     
      yLeft = d => getTotalClicks(d,true);
      yRight = d => getTotalClicks(d,false);
      xLabel = "Weeks";
    }
    else if(props.layer === EGraphLayer.month)
    {

      if(window.innerWidth < 800)
      {
        x = d => 
        {
          var firstDateString = d.at(0).at(0).date.toLocaleDateString('en-GB');
          var secondDateString = d.at(-1).at(-1).date.toLocaleDateString('en-GB');
          return firstDateString.substring(0, 5) + " " + firstDateString.substring(6, firstDateString.length) + " - " + secondDateString.substring(0, 5) + " " + secondDateString.substring(6, secondDateString.length);
        }
      }
      else
      {
        x = d => d.at(0).at(0).date.toLocaleDateString('en-GB') + " - " + d.at(-1).at(-1).date.toLocaleDateString('en-GB');
      }
  
      yLeft = d => getTotalClicksWeeks(d,true);
      yRight = d => getTotalClicksWeeks(d,false);
      xLabel = "Months";
    }
    else if(props.layer === EGraphLayer.year)
    {
      x = d => d.at(0).at(0).at(0).date.toLocaleDateString('en-GB') + " - " + d.at(-1).at(-1).at(-1).date.toLocaleDateString('en-GB');
      yLeft = d => getTotalClicksMonths(d,true);
      yRight = d => getTotalClicksMonths(d,false);
      xLabel = "Years";
    }

    if(props.layer !== EGraphLayer.hour)
    {
      maxLeft = Math.max(...clickData.map(yLeft));
      maxRight = Math.max(...clickData.map(yRight));
      dataMap = clickData;

      xScale = scaleBand({
        range: [0,xMax],
        round: true,
        domain: clickData.map(x),
        padding: 0.3
    });

    }

    // If the whole days clickvalues are 0
    // Then set the scale range anyway to 1000 for it not to have the bar from middle
    if(maxLeft === 0)
    {
      maxLeft = 1000;
    }

    if(maxRight === 0)
    {
      maxRight = 1000;
    }

    const yScaleLeft = scaleLinear({
        range: [yMax, 0],
        round: true,
        nice: true,
        domain: [0,maxLeft],
    });

    const yScaleRight = scaleLinear({
        range: [yMax, 0],
        round: true,
        domain: [0,maxRight]

    })
    
  
    const compose = (scale,accessor) => data => scale(accessor(data));
    const xPoint = compose(xScale,x);
    const yPointLeft = compose(yScaleLeft,yLeft);
    const yPointRight = compose(yScaleRight,yRight);

    // event listener is needed for checking if the graph window isn't active for the toolTip not to be shown
    const [windowIsActive, setWindowIsActive] = useState(true)
    function handleActivity(forcedFlag) {
      if (typeof forcedFlag === 'boolean') {
        return forcedFlag ? setWindowIsActive(true) : setWindowIsActive(false)
      }
  
      return document.hidden ? setWindowIsActive(false) : setWindowIsActive(true)
    }
    
    useEffect(() => {
      const handleActivityFalse = () => handleActivity(false)
      const handleActivityTrue = () => handleActivity(true)
  
      document.addEventListener('visibilitychange', handleActivity)
      document.addEventListener('blur', handleActivityFalse)
      window.addEventListener('blur', handleActivityFalse)
      window.addEventListener('focus', handleActivityTrue )
      document.addEventListener('focus', handleActivityTrue)
  
      return () => {
        window.removeEventListener('blur', handleActivity)
        document.removeEventListener('blur', handleActivityFalse)
        window.removeEventListener('focus', handleActivityFalse)
        document.removeEventListener('focus', handleActivityTrue )
        document.removeEventListener('visibilitychange', handleActivityTrue )
      }
    }, [])



    return (
        <svg style={{zIndex: 30, borderTop: '1px'}} ref={props.containerRef} width={width} height={height}>
    <LinearGradient id="backgroundGradient" from="#ffce95" to="#f75c5c" />;
    <LinearGradient id="barBackgroundGradient" from="#399cf4" to="#1e65a4" />;
    <rect style={{zIndex: 20}} x={0} y={0} width={width} height={height} fill="url('#backgroundGradient')" rx={5} />
     <Group top={margin.top} left={margin.left}>
    <AxisLeft
          scale={props.btoggleClickStats? yScaleLeft : yScaleRight}
          top={1}
          left={0}
          label={props.btoggleClickStats? "Left mouse clicks" : "Right mouse clicks"}
          labelProps={{fontSize: '18px', y:'-40', x: -height/2 || '0'}}
          fontColor={"black"}
          stroke={"black"}
          strokeWidth={2}
          tickLength={8}
          tickTextFill={"black"}
          tickFormat={(d) =>
          {
            var newNumber = 0;
            if(d >= 1000000)
            {
              newNumber = Math.round((d/1000000)).toString() + "M";
             
            }
            else if(d >= 10000)
            {
              newNumber = Math.round((d/1000)).toString() + "k";
              
            }
            else {
              newNumber = d.toString();
            }
            return newNumber;
          }}
          tickLabelProps={() => ({
            fontWeight: '700',
            width: 50,
            fontSize: '0.8em',
            textAnchor: 'end',
            verticalAnchor: 'middle',
            scaleToFit: false,
            dx: -2
            
          })}
        />
<AxisBottom
          scale={xScale}
          top={yMax + 1}
          label={xLabel}
          labelProps={{fontSize: '18px', y: '65px', x: width/2-70}}
          stroke={'#1b1a1e'}
          strokeWidth={2}
          numTicks={24}
          tickLength={8}
          tickTextFill={'#1b1a1e'}
          tickLabelProps={() => ({
            fontWeight: '700',
            width: 25,
            fontSize: '0.8em',
            textAnchor: 'middle',
            verticalAnchor: 'middle',
            scaleToFit: false,
            dy: 15
            
          })}
        />
{dataMap.map((data) => {
        var barHeightLeft = yMax - yPointLeft(data);
        var barHeightRight = yMax - yPointRight(data);

        var clickBar = null; 
      
        if(props.btoggleClickStats)
        {
          clickBar =<Bar  onMouseMove={(event) => {
            if(windowIsActive === true)
            {
              const point = localPoint(event.target.ownerSVGElement, event) || { x: 0, y: 0 };
              props.showTooltip({
              tooltipData: data,
              tooltipTop: point.y,
              tooltipLeft: point.x
            });
            }
            
  
          }} 
          onMouseLeave={() => {
            tooltipTimeout = window.setTimeout(() => {props.hideTooltip();},25);
          }}

          
          onClick={() => props.requestInnerGraph && props.requestInnerGraph(data)}
          

          x={xPoint(data)}
          y={yMax - barHeightLeft}
          height={barHeightLeft}
          width={xScale.bandwidth()}
          fill={"url('#barBackgroundGradient')"}
        />
        }
        else
        {
          clickBar =<Bar onMouseMove={(event) => {
            if(windowIsActive === true)
            {
            const point = localPoint(event.target.ownerSVGElement, event) || { x: 0, y: 0 };
            props.showTooltip({
            tooltipData: data,
            tooltipTop: point.y,
            tooltipLeft: point.x
  });
}
          }} onMouseLeave={() => {
            tooltipTimeout = window.setTimeout(() => {props.hideTooltip();},150);
          }}

          onClick={() => props.requestInnerGraph && props.requestInnerGraph(data)}

          x={xPoint(data)}
          y={yMax - barHeightRight}
          height={barHeightRight}
          width={xScale.bandwidth()}
          fill={"url('#barBackgroundGradient')"}
          />
        }
        return (
          <Group key={`bar-${barHeightLeft}-${Math.random()}`}>
            {clickBar}
          </Group>
        );
      })}

</Group>
  </svg>
        )
}