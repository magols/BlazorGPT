# Prerequisites
* .NET 9 SDK  
* OpenAI API Key (or Azure OpenAI endpoint, resource name and key) or an Ollama endpoint.   
* SQL Server (optional, uses Sqlite by default)


## Quick start with Sqlite

1. Configure the OpenAI API Key and adjust model preferences in appsettings.json in the **src/BlazorGPT.Web** directory
```json
     "OpenAI": {
        
        "ApiKey": "[your api key]",

        "ChatModel": "gpt-4o-mini",
        "ChatModels": [ "gpt-4", "gpt-4o-mini", "gpt-3.5-turbo" ]
      },

```

2. Run the following command from the **src/BlazorGPT.Web** directory:

```bash
dotnet run
```  

This will create a default Sqlite database in the **BlazorGPT.Web** directory and run the application. 

Note: the Sqlite database is not suitable for production use. You should use SQL Server or another database for production. It does not support EF Core migrations correctly, so you will need to manually update the database schema if you make changes to the models.
 
3. Open the application in a browser at [http://localhost:5281/](`http://localhost:5281/`). Create an user account and login to the application

4. Enjoy the app!

## Setup with SQL Server and migrations

1. In the appsettings.json in the **src/BlazorGPT.Web** directory, set the database to SqlServer. 
    ```json
    "Database": "SqlServer", // Sqlite or SqlServer
    ```


2. Update the connection strings in appsettings.json (secrets.json) in the **BlazorGPT.Web** directory
   ```json
    "ConnectionStrings:BlazorGptDB": "[your connection string]",
    "ConnectionStrings:UserDB": "[your connection string]",
   ```




3. The applications is configured to use EF Core migrations automatically on startup. If you want to apply the migrations manually, change this setting in the appsettings.json in the **src/BlazorGPT.Web** directory:
   ```json
      "ApplyMigrationsAtStart": false,
   ```


4. Run the following commands from the **src/BlazorGPT** directory:

   ```bash 
    dotnet ef database update -s '..\BlazorGPT.Web\' --context BlazorGptDBContext
   ```
   
5. Run the following commands from the **src/BlazorGPT.Web** directory:
   ```bash
    dotnet ef database update --context ApplicationDbContext
   ```


6. Run the application from the **src/BlazorGPT.Web** directory:
   ```bash
    dotnet run
    ```

7. Open the application in a browser at [http://localhost:5281/](`http://localhost:5281/`)

8. Create a user account and login to the application

9. Enjoy the app!


