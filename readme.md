![Icon](https://raw.githubusercontent.com/NServiceBusSqlPersistence/NServiceBus.SqlPersistence/master/Icon/package_icon.png)

NServiceBus.SqlPersistence
===========================

Add support for [NServiceBus](https://docs.particular.net/nservicebus/) to persist to a Sql Database.


## The nuget package  [![NuGet Status](http://img.shields.io/nuget/v/NServiceBus.Persistence.Sql.svg?style=flat)](https://www.nuget.org/packages/NServiceBus.Persistence.Sql/)

https://www.nuget.org/packages/NServiceBus.Persistence.Sql/

    PM> Install-Package NServiceBus.Persistence.Sql


## Documentation


## Usage

```
var config = new EndpointConfiguration("EndpointName");
config.UsePersistence<SqlPersistence>();
```


## Icon

<a href="http://thenounproject.com/term/database/735720/" target="_blank">Database</a> designed by <a href="http://thenounproject.com/Deepz/" target="_blank">Deepz</a> from The Noun Project
