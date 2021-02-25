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

NOTE: Requires [building your own docker image](/dev/oracle-docker-image.md)

`docker run -d --name oracledb -p 1521:1521 -p 5500:5500 -e ORACLE_PWD=super-secret-password -e ORACLE_CHARACTERSET=AL32UTF8 oracle/database:18.4.0-xe`

Add an environment variable called `OracleConnectionString` with the connection string:

`User Id=system;Password=super-secret-password;Data Source=localhost:1521/XEPDB1;`

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
8. Adjust to connection string in the sample 
   - include `Column Encryption Setting=Enabled;`
   - Using a connecting builder, set `ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled`
11. Rerun the sample, everything should work as expected
