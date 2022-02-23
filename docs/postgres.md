# Postgres

## Developer Notes

* [Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver15) is a good tool for database management. If you use Azure Data Studio, it requires an extension for postgres, this can be installed via the left nav menu like vscode.
* There is an admin connection string in the [Configuration Explorer](https://portal.azure.com/#@chonky.market/resource/subscriptions/1b3b81b1-777b-4086-bd8d-1f9efec426ef/resourceGroups/nope/providers/Microsoft.AppConfiguration/configurationStores/nope-configuration/kvs) named `ConnectionStrings.DatabaseConnection`.
* The database has [Connection security](https://portal.azure.com/#@chonky.market/resource/subscriptions/1b3b81b1-777b-4086-bd8d-1f9efec426ef/resourceGroups/nope/providers/Microsoft.DBforPostgreSQL/servers/chonky/connectionSecurity) which includes a firewall that requires explicitly adding IP ranges to be allowed to connect.
