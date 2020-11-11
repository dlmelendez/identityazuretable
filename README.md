identityazuretable
==================

This project provides a high performance cloud solution for ASP.NET Identity Core using Azure Table storage replacing the Entity Framework / MSSQL provider.

[![Build Status](https://dev.azure.com/elcamino/Azure%20OpenSource/_apis/build/status/IdentityAzureTableCore?branchName=master)](https://dev.azure.com/elcamino/Azure%20OpenSource/_build/latest?definitionId=4&branchName=master)
[![NuGet Badge](https://buildstats.info/nuget/ElCamino.AspNetCore.Identity.AzureTable)](https://www.nuget.org/packages/ElCamino.AspNetCore.Identity.AzureTable/)
[![NuGet Badge](https://buildstats.info/nuget/ElCamino.AspNet.Identity.AzureTable)](https://www.nuget.org/packages/ElCamino.AspNet.Identity.AzureTable/)

Project site at https://dlmelendez.github.io/identityazuretable/.

Identity Core 5 template
```
dotnet new --install ElCamino.AspNetCore.Identity.AzureTable.Templates

#MVC Template
dotnet new mvc-id-azure-tables 

#Razor Pages Template
dotnet new rzp-id-azure-tables 
```

Identity Core 3.x (uses PageModel - latest) - Use ElCamino.AspNetCore.Identity.AzureTable, sample mvc app: https://github.com/dlmelendez/identityazuretable/tree/master/sample/samplemvccore4

Identity Core 2.x (uses PageModel - latest) - Use ElCamino.AspNetCore.Identity.AzureTable, sample mvc app: https://github.com/dlmelendez/identityazuretable/tree/master/sample/samplemvccore3

Identity Core 2.x (uses MVC - older) - Use ElCamino.AspNetCore.Identity.AzureTable, sample mvc app: https://github.com/dlmelendez/identityazuretable/tree/master/sample/samplemvccore2
