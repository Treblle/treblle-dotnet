# treblle-dotnet
Offical Treblle .NET package

Treblle makes it super easy to understand what’s going on with your APIs and the apps that use them. To get started with Treblle create a FREE account on https://treblle.com.

Requirements
.NET 4.7.2

Dependencies
System.Web.Http
Newtonsoft.Json

Installation
You can add Treblle to your current solution and add a reference to it in a project you want to use it for, or build the project and add Treblle.dll to you existing project.
NuGet package coming soon.

Getting started
The first thing you need to do is create a FREE account on https://treblle.com to get an API key and Project ID.
You'll need to add those values to your Web.config file under 
<configuration>
  <appSettings>
    <add key="TreblleApiKey" value="Your Treblle API key"/>
    <add key="TreblleProjectId" value="Your Treblle project ID"/>

To the ApiController you want to use it with you need to add
using Treblle;

After that you need to add [TreblleActionFilterAttribute] and apply it to the entire controller or any method you want to use it for.

That's it. Your API requests and responses are now being sent to your Treblle project. 
Just by adding that line of code you get features like: auto-documentation, real-time request/response monitoring, error tracking and so much more.

Treblle will catch everything that is sent to your API endpoints as well as everything that the endpoints return. 

In case you wish to add even more information to track specific things in your API but NOT return them in the response you can call add meta information to a specific API endpoint or all endpoints. To do so you can do the following:

License
Copyright 2020, Treblle. Licensed under the MIT license: http://www.opensource.org/licenses/mit-license.php
