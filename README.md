
# This repository contains the whole Clicktracker functionality which includes a Windows WPF application, ASP.NET WEB API and a React webpage.

### **These were done as a demo part of a web development oriented coding bootcamp. I did not have any previous web development experience other than my basic Gatsby portfolio site.**


## Clicktracker

The ClickTracker is a WPF application. Made without previous knowledge of WPF and hence it does not follow the MVVM pattern.
ClickTracker has functionality to hook the computer mouse, and track left and right mouse button clicks. Which it will add into C# dictionaries together with the occured hour as key. 
And finally make a day dictionary which can contain a whole day of mouse clicks. This can then be synced to a database for the React webpage to show stats.

ClickTracker features couple of adjustable settings such as automatic database syncing, launch on startup, launch hidden (meaning no window will be opened when it launches, only tray icon). 



## ClickTrackerAPI

The ClickTrackerAPI is a ASP.NET WEB API 2.0 application. 
