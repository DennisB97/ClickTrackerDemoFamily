// Copyright 2021 Dennis Baeckstroem 
import React from 'react';
import Navbar from './Navbar.js';
import * as layoutStyle from '../styles/layout.module.css';
import Footer from './Footer.js';

// The main layout all pages use
export default function Layout(props){
return(
  <div className={layoutStyle.mainBody}>
  <div className={layoutStyle.column}>
  <div className={layoutStyle.topBar}>
  <Navbar></Navbar>
  </div>
  <div className={layoutStyle.bottomContent}>{props.children}</div>

  </div>
  <Footer></Footer>
  </div>
  )
}

