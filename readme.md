# NServiceBus.SqlPersistence

Add support for [NServiceBus](https://docs.particular.net/nservicebus/) to persist to a Sql Database.

## Documentation

https://docs.particular.net/persistence/sql/

## Running the tests

There are tests targetting multiple database engines. These can be installed on your machine or run in a docker container.
The tests require a connectionstring set up in environment variables (remember that Visual Studio and Rider load these at start up, so restarting the IDE might be necessary).

### SQL Server

Docker:

    docker run --name SqlServer -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=NServiceBusPwd!" -p 1433:1433 -d  mcr.microsoft.com/mssql/server:2017-latest

Environment variable:

Add an environment variable called `SQLServerConnectionString` with the connection string:

    Server=localhost;User Id=sa;Password=NServiceBusPwd!

### MySql

Docker:

    docker run --rm --name test-mysql -p 3306:3306 -e MYSQL_ROOT_PASSWORD=my-super-secret-password -e MYSQL_DATABASE=NServiceBus -e MYSQL_USER=nsbuser -e MYSQL_PASSWORD=nsbuser-super-secret-pwd -d mysql:latest

Environment variable:

Add an environment variable called `MySQLConnectionString` with the connection string:

    Server=localhost;Port=3306;Database=NServiceBus;Uid=nsbuser;Pwd=nsbuser-super-secret-pwd;AllowUserVariables=True;AutoEnlist=false

### Postgres

Docker:

    docker run -d --name PostgresDb -v my_dbdata:/var/lib/postgresql/data -p 54320:5432 -e POSTGRES_PASSWORD=super-secret-password postgres:11

Environment variable:

Add an environment variable called `PostgreSqlConnectionString` with the connection string:

    User ID=postgres;Password=super-secret-password;Host=localhost;Port=54320;Database=nservicebus;Pooling=true;

### Oracle

Docker:

    docker run -d --name oracledb -p 1521:1521 -p 5500:5500 -e ORACLE_PWD=super-secret-password -e ORACLE_CHARACTERSET=AL32UTF8 oracle/database:18.4.0-xe

Above *Oracle Database Express* image can be created via the following procedure which is based on the [official guidance for running Oracle Database 18c Express Edition in a Docker container](https://github.com/oracle/docker-images/tree/master/OracleDatabase/SingleInstance#running-oracle-database-18c-express-edition-in-a-docker-container).

1. Clone <https://github.com/oracle/docker-images/>
2. Download [Oracle Database Express Edition (XE) Release 18.4.0.0.0 (18c)](https://www.oracle.com/database/technologies/xe-downloads.html)
3. Move it to `$/OracleDatabase/SingleInstance/dockerfiles/18.4.0`
4. Invoke `$/OracleDatabase/SingleInstance/dockerfiles/buildDockerImage.sh` (git bash or WSL) as `./buildDockerImage.sh -v 18.4.0 - x` (build express image)

Environment variable:

Add an environment variable called `OracleConnectionString` with the connection string:

    User Id=system;Password=super-secret-password;Data Source=localhost:1521/XEPDB1;
