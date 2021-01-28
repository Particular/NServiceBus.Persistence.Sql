@docker run --name SqlServer -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=NServiceBusPwd!" -p 1433:1433 -d  mcr.microsoft.com/mssql/server:2017-latest
