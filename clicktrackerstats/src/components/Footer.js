// Copyright 2021 Dennis Baeckstroem 
import styled from "styled-components";

const StyledDiv = styled.div`
height: 50px;
background-image: radial-gradient( circle farthest-corner at 10% 20%,  rgba(160,67,67,1) 0%, rgba(140,67,67,1) 100% );
border-radius: 1px;
border-style: hidden;
border-color: black;
display: flex;
flex-direction: column;
justify-content: center;
padding-left: 25px;
`
const StyledStrong = styled.strong`
text-align: left;
`
// Simple footer component displaying copyright
export default function Footer()
{
    return(
        <>
        <StyledDiv>
        <StyledStrong>Copyright &copy; Dennis Bäckström</StyledStrong>
        </StyledDiv>
        </>
        );
}