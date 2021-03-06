
# This repository contains the whole Clicktracker functionality which includes a Windows WPF application, ASP.NET WEB API and a React webpage.

### **These were done as a demo part of a web development oriented coding bootcamp. I did not have any previous web development experience other than my basic Gatsby portfolio site. Personally I have the API running in Azure together with an SQL Database, but I have replaced the API strings and routes here with an example string.**


## Clicktracker

The ClickTracker is a WPF application. Made without previous knowledge of WPF and hence it does not follow the MVVM pattern.
ClickTracker has functionality to hook the computer mouse, and track left and right mouse button clicks. Which it will add into C# dictionaries together with the occured hour as key. 
And finally make a day dictionary which can contain a whole day of mouse clicks. This can then be synced to a database for the React webpage to show stats.

ClickTracker features couple of adjustable settings such as automatic database syncing, launch on startup, launch hidden (meaning no window will be opened when it launches, only tray icon). 

![image](https://drive.google.com/uc?export=view&id=12tcZuVtD8lDu6NiqSHZhidR-6GjBS6We)


## ClickTrackerAPI

The ClickTrackerAPI is an ASP.NET WEB API 2.0 application. This API gets both calls from the React webpage and windows application.



## ClickTrackerStats

The ClickTrackerStats is a ReactJS website. This website provides a way to show own mouseclick data through graphs for any queried day. 
This site has the ability to query clicks with a given ID which was received from the clicktracker application. One can then choose the dates which of data would be searched from, and the data would be shown in a graph made with the visx package.
![image](https://drive.google.com/uc?export=view&id=1omEf44MuxVHpn3Xnkv-lZLrEztiS-S1A)
