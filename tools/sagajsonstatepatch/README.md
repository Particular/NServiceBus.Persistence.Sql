# Saga json state patch tool

Version 7.0.3 introduced a bug where Saga json state could become corrupted if the saga state is updated with a string value that is shorter than the previous. Data from the previous state is trailing at the end. This is not causing any issues for the SQL persister its JSON Serializer which ignores this but the column can contain invalid JSON data and can result in parsing issues. Another side effect is that the values always have a length of 4,000 characters (stored as 8,000 bytes) which can cause storage issues.

This bug is patched in 7.0.4

## When to use

1. Run into json parser issues because you are using a custom tool
2. Want to trim the data to free space

Please note that if you use version 7.0.4 and sagas are updated the JSON data column will be automatically trimmed.

## Build

Build the `sagajsonstatepatch` solution. It currently requires .net 8 but if needed manually change the target to another version.

## Usage

When the tool is run it shows this data and asks to run the tool as a dry run and shows the "patch" UPDATE queries that would be executed and some statistics on processed records and patch success/failure statistics.

NOTE: It is recommended to backup the data and thoroughly review the output of the tool. 

### Arguments

The tool requires a connection string and a table name which need to be provided on the command line:

    sagajsonstatepatch.exe ""connection string"" ""table name""

Example:

    sagajsonstatepatch.exe ""%SQLServerConnectionString%"" NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga

The output can be piped to a results file to be manually run on the database server:

Create a `patch.sql` file:

    sagajsonstatepatch.exe ""%SQLServerConnectionString%"" NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga >patch.sql

## Output example

```txt
Please contact Particular support at <support@particular.net> if assistance is required.

Connection string: data source=localhost;Database=nservicebus;persist security info=True;User Id=sa;Password=yourStrong(!)Password;Encrypt=false
Table name       : NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga
Dry run? [Y/n]
Dry run: True
UPDATE NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga SET Data = left(Data, 331) WHERE Id=a9711f31-daef-47ba-8ef0-b0bc00d928e5
UPDATE NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga SET Data = left(Data, 331) WHERE Id=f22712c4-fdf9-44f9-adfb-b0bc00d95a6e
UPDATE NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga SET Data = left(Data, 131) WHERE Id=91cbbdc0-9724-48a5-8946-b0bc00da0a33
UPDATE NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga SET Data = left(Data, 131) WHERE Id=ff989822-96c3-4835-b702-b0bc00da0a54
UPDATE NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga SET Data = left(Data, 131) WHERE Id=b0ff5e31-ac84-4b00-b512-b0bc00da0a88
UPDATE NsbSamplesSqlPersistence.dbo.Samples_SqlPersistence_EndpointSqlServer_OrderSaga SET Data = left(Data, 131) WHERE Id=9eb6a13e-ec1a-4726-a596-b0bc00dae643

Process 6 matching rows in 00:00:00.1263438, patched 6 records and 0 need manual patching.
```
