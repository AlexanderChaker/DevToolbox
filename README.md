# DeployGitBranch
This tool deploys a branch to a database, using those steps:
- Select the Working Directory of the repo
  - Optional: Select a YAML file so that the app generates the SQL scripts in the order defined in the "FoldersToProcess" section
- Select the Server URL you want to deploy on, and the default DB. (db objects who have the "Use <DB>" header will be deployed on that DB
- Select the Current Branch and the Source Branch for GIT to do its diffs
- The app will use the diffs to generate SQL statements and deploy on the server


# TODO
- Add a modifiable File List after the diffs have been created, with support for drag-and-drop and delete
- Catch all exceptions that crash the app, replace with Toast notifications (Application Level 