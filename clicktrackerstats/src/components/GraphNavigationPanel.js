// Copyright 2021 Dennis Baeckstroem 
import styled from "styled-components";
import { BiArrowBack, BiStats} from "react-icons/bi";
import * as styledIcons from "../styles/GraphInteractionButtons.module.css";
import {HiSwitchHorizontal} from "react-icons/hi";

const StyledDiv = styled.div`
display:flex;
flex-direction: row;
overflow: hidden;
margin: auto;
background-color: rgb(252, 204, 152);
width: 70%;
padding-left: 20px;
border: 3px solid #000 !important;
border-top-left-radius: 10px; 
border-Bottom-Left-Radius: 10px;
border-Top-Right-Radius: 10px;
border-Bottom-Right-Radius: 10px; 
min-height: 60px;
@media (max-width: 800px) {
    width: 100%;
  }

`

const StyledButton = styled.button`
min-height: 50px;
width: 50px;
margin-left: 15px;
display: flex;
align-items: center;
background: transparent;
pointer-events: all;
border: none;


`

// This component contains buttons for navigating the graph and displaying stats
export default function GraphNavigationPanel(props)
{

return(
    <StyledDiv>
    <StyledButton onClick={() => props.backwards()} title="Return to previous view"><BiArrowBack size='50' className={styledIcons.buttonIcons}></BiArrowBack></StyledButton>
    <StyledButton onClick={() => props.leftRightToggle()} title="Change between left and right click stats"><HiSwitchHorizontal  size='50' className={styledIcons.buttonIcons}></HiSwitchHorizontal></StyledButton>
    <StyledButton onClick={() => props.statsToggle()} title="toggle stats overlay"><BiStats size='50' className={styledIcons.buttonIcons}></BiStats></StyledButton>
    </StyledDiv>
    );
}