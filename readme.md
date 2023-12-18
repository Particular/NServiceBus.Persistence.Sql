# NServiceBus.SqlPersistence

NServiceBus.SqlPersistence provides support for NServiceBus to persist to an Sql Database.

It is part of the [Particular Service Platform](https://particular.net/service-platform), which includes [NServiceBus](https://particular.net/nservicebus) and tools to build, monitor, and debug distributed systems.

See the [Sql Persistence documentation](https://docs.particular.net/persistence/sql/) for more details on how to use it.

## Running tests locally

There are tests targeting multiple database engines. These can be installed on your machine or run in a Docker container. The tests require a connection string set up in environment variables (remember that Visual Studio and Rider load these at start up, so restarting the IDE might be necessary).

### SQL Server

For convenience, scripts have been provided in a `/dev` folder.

Docker:

    docker run --name SqlServer -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=NServiceBusPwd!" -p 1433:1433 -d  mcr.microsoft.com/mssql/server:2017-latest

Environment variable:

Add an environment variable called `SQLServerConnectionString` with the connection string:

    Server=localhost;User Id=sa;Password=NServiceBusPwd!;Database=nservicebus

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

Docker (using the [lightweight community image](https://hub.docker.com/r/gvenzl/oracle-xe) that we also use for CI/CD):

    docker run -d --name oracledb -p 1521:1521 -p 5500:5500 -e ORACLE_PASSWORD=super-secret-password gvenzl/oracle-xe:21.3.0-slim

Docker (official image):

    docker run -d --name oracledb -p 1521:1521 -p 5500:5500 -e ORACLE_PWD=super-secret-password -e ORACLE_CHARACTERSET=AL32UTF8 container-registry.oracle.com/database/express:21.3.0-xe

Add an environment variable called `OracleConnectionString` with the connection string:

    User Id=system;Password=super-secret-password;Data Source=localhost:1521/XEPDB1;

## Smoke testing SQL Always Encrypted

In the Azure Portal, set up a dedicated resource group for testing purposes, that's cleaned up when the tests are completed.

1. Create a [SQL database](https://portal.azure.com/#create/Microsoft.SQLDatabase)
2. Once deployed, access the SQL Server and go to "Firewalls and virtual networks" and add a rule that allows access from your local IP address
3. Choose / download a sample that you want to smoke test, for example the [SQL persistence simple saga sample](https://docs.particular.net/samples/sql-persistence/simple/)
   - Remove all endpoints except for SQL Server
   - Adjust the connection string to point to the database you created
   - Remove the call to `MarkAsComplete()`. That will keep the saga alive so that you can inspect the data in the table
4. Run the sample first with installers enabled
5. Check the database. You should see a table for the saga data, with one row in it.
6. Now we will encrypt some of the columns of the table
   - Right click on the table, and click 'Encrypt columns'
   - Choose the columns you want to encrypt, the ´Data´-column should be sufficient
   - Encryption type: Deterministic
   - Encryption key: a new one
   - Select Windows certificate store as a key store provider, under Current user
   - Choose 'Proceed to finish now'
   - Query the data to verify that the Data column's content is now encrypted
7. Adjust to connection string in the sample 
   - include `Column Encryption Setting=Enabled;`
   - Using a connecting builder, set `ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled`
8. Rerun the sample, everything should work as expected
