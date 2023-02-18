# Issue Reporting Dahsboard Example
Demonstration of an Issue Reporting System

## Installation
Use:
```
./dotnet restore
```
To restore the **API** for the solution.

Use:
```
./client npm install
```
To restore the Angular usage in the **client**.

Add:
```
./client/ssl
```
folder with: 'server.srt' and 'server.key' for ssl certificate usage.

Export to production:
```
/client
ng build --prod
```

Publish to server
```
dotnet publish -o 'C:\System Exports\DD-MM-YYYY' -f net5.0 -r win7-x64
```
