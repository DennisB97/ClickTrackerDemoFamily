// Copyright 2021 Dennis Baeckstroem 
import {createSearchParams } from "react-router-dom";

// helper function to append multiple params into the url
export default function appendSeachParams(obj, searchParams)
{
    
    const sp = createSearchParams(searchParams);
    Object.entries(obj).forEach(([key, value]) => {
    if (Array.isArray(value)) {
      sp.delete(key);
      value.forEach((v) => sp.append(key, v));
    } else if (value === undefined) {
      sp.delete(key);
    } else {
      sp.set(key, value);
    }
  });
         return sp;
        
        
}