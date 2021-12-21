// Copyright 2021 Dennis Baeckstroem 
import styled from 'styled-components'
import {
    Link
  } from "react-router-dom";

const StyledNav = styled.nav`
background: transparent;
justify-content: center;
position: relative;
display: flex;
flex-direction: column;

div{
display: flex;
flex-direction: row;
}


ul{
list-style-type: none;
margin: 0;
display: flex;
flex-direction: row;
}

li{
    a:link {text-decoration: none; color:grey;}
    a:visited { text-decoration: none; color:grey; }
    a:hover { text-decoration: none; color:darksalmon; }
    a:focus { text-decoration: none; color:grey; }
    a:active { text-decoration: none; color:grey }

}

a{
    padding-left: 20px;
    text-decoration: none;
}

`


// Simple navbar that only contains one Link to main page
export default function Navbar()
{
    return(
        <StyledNav>
        <div>
            <ul>
            <li>
            <Link to="/">Stats Search</Link>
            </li>
            </ul>
        </div>
        </StyledNav>
        )
}