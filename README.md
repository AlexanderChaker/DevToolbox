# WARNING
### DO NOT UPDATE \<MauiVersion\> TO GREATER THAN 8.0.7 UNTIL [THIS ISSUE](https://github.com/microsoft/fluentui-blazor/issues/1741) IS RESOLVED

# DeployGitBranch
This tool deploys a branch to a database, using those steps:
- Select the Working Directory of the repo
  - Optional: Select a YAML file so that the app generates the SQL scripts in the order defined in the "FoldersToProcess" section
- Select the Server URL you want to deploy on, and the default DB. (db objects who have the "Use <DB>" header will be deployed on that DB
- Select the Current Branch and the Source Branch for GIT to do its diffs
- The app will use the diffs to generate SQL statements and deploy on the server


# TODO
- Add a modifiable File List after the diffs have been created, with support for drag-and-drop and delete
- Catch all exceptions that crash the app, replace with Toast notifications (Application Level)
- Catch exception when cannot connect to database or repo
- Implement "Cancel" feature when running SQL queries
- Refactor Logs, change the UI console and the way the services write to the Logs variable
