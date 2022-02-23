# ChonkyBot

## Getting Started

### Environment Variables

* Add key `ConnectionString` with a value obtained via [Azure](https://portal.azure.com/#@chonky.market/resource/subscriptions/1b3b81b1-777b-4086-bd8d-1f9efec426ef/resourceGroups/nope/providers/Microsoft.AppConfiguration/configurationStores/nope-configuration/keys), either Primary or Secondary is fine to use.
* Add key `AppConfigurationLabel` with a value that is a short text description for your developer environment, for example `BillDev`. This value will be be used in the Configuration Explorer step below.

### Environment Variables Alternative (`launchSettings.json`)

* Instead of adding environment variables it is possible to add the required keys to `launchSettings.json` in the `environmentVariables` section. This will require you exclude the file from checkin so it's not recommended. _WARNING: if you add a key with no value it will unset the variable!_

### Azure Configuration Explorer

* In [Configuration Explorer](https://portal.azure.com/#@chonky.market/resource/subscriptions/1b3b81b1-777b-4086-bd8d-1f9efec426ef/resourceGroups/nope/providers/Microsoft.AppConfiguration/configurationStores/nope-configuration/kvs) add additional values for any developer specific Keys, for example `discordBotToken`. Use a `Label` for your value that you picked in the previous step, for example `BillDev`. Not all keys need per developer values.
