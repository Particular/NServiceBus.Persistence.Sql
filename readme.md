NServiceBus.SqlPersistence
===========================

Add support for [NServiceBus](https://docs.particular.net/nservicebus/) to persist to a Sql Database.


## Documentation

https://docs.particular.net/persistence/sql/

## Running the tests

There are tests targetting multiple database engines. These can be installed on your machine or run in a docker container.
The tests require a connectionstring set up in environment variables (remember that Visual Studio and Rider load these at start up, so restarting the IDE might be necessary).

For convenience, scripts have been provided in `/dev` folder.

**For SQL Server**

`docker run --name SqlServer -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=NServiceBusPwd!" -p 1433:1433 -d  mcr.microsoft.com/mssql/server:2017-latest`

Add an environment variable called `SQLServerConnectionString` with the connection string:
`Server=localhost;User Id=sa;Password=NServiceBusPwd!;Database=nservicebus;`

**For MySql**

`docker run --rm --name test-mysql -p 3306:3306 -e MYSQL_ROOT_PASSWORD=my-super-secret-password -e MYSQL_DATABASE=NServiceBus -e MYSQL_USER=nsbuser -e MYSQL_PASSWORD=nsbuser-super-secret-pwd -d mysql:latest`

Add an environment variable called `MySQLConnectionString` with the connection string:
`Server=localhost;Port=3306;Database=NServiceBus;Uid=nsbuser;Pwd=nsbuser-super-secret-pwd;AllowUserVariables=True;AutoEnlist=false`

**For Postgres**

`docker run -d --name PostgresDb -v my_dbdata:/var/lib/postgresql/data -p 54320:5432 -e POSTGRES_PASSWORD=super-secret-password postgres:11`

Add an environment variable called `PostgreSqlConnectionString` with the connection string:

`User ID=postgres;Password=super-secret-password;Host=localhost;Port=54320;Database=nservicebus;Pooling=true;`

**For Oracle**

Add an environment variable called `OracleConnectionString` with the connection string:
