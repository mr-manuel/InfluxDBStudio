# Changelog

## v0.2.1

### Project
* Added `CHANGELOG.md`
* Updated `README.md`

### Program
* Added icon to EXE
* Fixed `Connections -> Refresh` button
* Changed default date format to `yyyy/M/dd`
* Changed default time format to `HH:mm:ss`
* Changed `New Query` icon (SQL Management Studio style)
* Changed `Run Query` icon (SQL Management Studio style)
* Updated .NET Framework to 4.6.2
* Updated jacobslusser.ScintillaNET to 3.6.3
* Updated log4net to 2.0.15
* Updated Newtonsoft.Json to 13.0.2
* Updated System.Net.Http to4.3.4
* Updated System.Security.Cryptography.Algorithms to 4.3.1
* Updated System.Security.Cryptography.X509Certificates to 4.3.2

### Tabs
* Middle mouse click on tab --> Close tab

### Keyboard shortcuts
* Added: press `F5` to run the current query
* Removed: press `Ctrl + R` to run the current query
* Added: press `Ctrl + N` to create new query
* Added: press `Ctrl + F5` to reload connections
* Added: press `Ctrl + A` to select all rows in the results table
* Added: press `Ctrl + C` to copy all selected rows from the results table to the clipboard


### Query
* New default query:
```sql
SELECT *
FROM "measurement"
ORDER BY "time" DESC
LIMIT 500
```

### GitHub Actions
* Added configurations that program compiles on every commit and on every release. This allows to download the compiled version

## v0.2.0-beta-1

This is a copy of the [last release](https://github.com/CymaticLabs/InfluxDBStudio/releases/tag/v0.2.0-beta.1) from the [forked repository](https://github.com/CymaticLabs/InfluxDBStudio).

### New in this release

* Upgraded InfluxData.Net version to 8.0.1
* Tested with InfluxDB version 1.3.6
* Added a tab context menu when right-clicking with the following options:
  * Close
  * Close All
  * Close All But This

### Fixes

* Empty user and password should work with a default install of InfluxDB
* Passwords which include '#' should work

### Notes

* Important: The assembly version number of the project has changed, this usually invalidates user settings in .NET applications (so for this app settings will be things like the connections you have configured). Your settings should get upgraded automatically, but you should created a backup of your settings just in case using File → Export → Settings. If your settings disappear in this new release you can import them from the exported settings file to avoid having to reenter all of your settings again.
