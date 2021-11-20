// Copyright 2021 Dennis Baeckstroem 
using System.Collections.Generic;
using System.Web.Http;
using WebApiThrottle;

namespace ClickTrackingAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            // enable cors
            config.EnableCors();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            

           // Set a throttling rule for all api calls
           config.MessageHandlers.Add(new ThrottlingHandler()
            {
                Policy = new WebApiThrottle.ThrottlePolicy(perSecond: 1, perMinute: 10, perHour: 60)
                {
                    IpThrottling = true,
                    ClientThrottling = true,
                    EndpointThrottling = true,
                    EndpointRules = new Dictionary<string, RateLimits>
                    {
                       {"clicktracker/SOMEAPIROUTINGHERE3", new RateLimits {PerSecond = 1, PerMinute = 10, PerHour = 30} },
                       {"clicktracker/SOMEAPIROUTINGHERE1", new RateLimits {PerSecond = 1, PerMinute = 1, PerHour = 5}},
                       {"clicktracker/SOMEAPIROUTINGHERE2", new RateLimits {PerSecond = 1, PerMinute = 1, PerHour = 1, PerDay = 5}},
                       {"clicktracker/SOMEAPIROUTINGHERE4", new RateLimits {PerSecond = 1, PerMinute = 1, PerHour = 5}},
                       {"clicktracker/SOMEAPIROUTINGHERE5", new RateLimits {PerSecond = 2, PerMinute = 30, PerHour = 100}},
                       {"clicktracker/SOMEAPIROUTINGHERE6", new RateLimits {PerSecond = 1, PerMinute = 15, PerHour = 200}}
                    }

                },
                Repository = new CacheRepository()

            }); ; 
            
            


        }
    }
}
