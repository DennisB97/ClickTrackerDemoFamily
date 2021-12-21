// Copyright 2021 Dennis Baeckstroem 
import React from 'react';
import ReactDOM from 'react-dom';
import Layout from './components/Layout';
import IDReader from './components/IDReader';
import { BrowserRouter, Routes, Route} from "react-router-dom";
import Stats from './pages/stats';

// function as index page which contains IDReader component
// Which can be used to enter a GUID and continue to stats page from button.
function Index()
{
return(
<IDReader></IDReader>
);
}



// Only two pages index and stats
// which are then wrapped in a layout
ReactDOM.render(
  <React.StrictMode>
  <BrowserRouter>
  <Layout>
  <Routes>
    <Route path="/" element={<Index/>} />
    <Route path="Stats" element={<Stats/>}/>
    <Route
      path="*"
      element={
        <div style={{ padding: "1rem", margin: "auto",alignSelf: "center"}}>
          <p style={{fontSize: "32px"}}>There's nothing here!</p>
        </div>
      }
    />
  </Routes>
  </Layout>
  </BrowserRouter>
  </React.StrictMode>,
  document.getElementById('root')
);






