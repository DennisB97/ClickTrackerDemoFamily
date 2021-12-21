// Copyright 2021 Dennis Baeckstroem 
import styled from "styled-components";
import { LinkWithQuery } from './LinkWithQuery';
import appendSeachParams from "../utils/appendSearchParams";
import {
    useSearchParams

  } from "react-router-dom";
  


const StyledDiv = styled.div`
margin: auto;
background: transparent;
display: flex;
flex-direction: column;
align-items: center;
align-content: center;
justify-content: center;

`
const InnerDiv = styled(StyledDiv)`
background: transparent;
display: flex;
flex-direction: row;

`
// IDReader contains an input for entering the unique ID used with clicktracker
// And a button to proceed to stats page with the given ID
export default  function IDReader(props)
{
    var [searchParams, setSearchParams] = useSearchParams();

    return(
        <StyledDiv>
        <InnerDiv>
        <div className="input-group mb-3">
        <input onChange={e => {if(e.target.value !== null ) {setSearchParams(appendSeachParams({stringID: e.target.value},searchParams))}}} type="text" className="form-control" placeholder="Enter your ClickTracker ID" aria-label="Recipient's username" aria-describedby="basic-addon2" id="stringID"/>
        <div className="input-group-append">
          <LinkWithQuery to={`/stats`} className="btn btn-secondary" type="button">Find</LinkWithQuery>
        </div>
      </div>
        </InnerDiv>
        </StyledDiv>
        )
}

