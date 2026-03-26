# App Features
## KafkaSerializer
A utility for deserializing and formatting Kafka messages. Paste or type a JSON message into the input pane and click **Convert** to get pretty-printed output.

- **Decompresses** gzip-compressed (Base64-encoded) `Message` fields automatically
- **Unescapes** nested JSON strings recursively, expanding them into structured JSON
- **Copy** the formatted output to the clipboard, or **Copy Escaped** to re-serialize the `Message` field back into an escaped string
- **Paste from Clipboard** to quickly load a message without manual pasting
- Supports a system-tray shortcut that pastes from the clipboard and converts in one step

## DeployGitBranch
This tool deploys a branch to a database, using those steps:
- Select the Working Directory of the repo
  - Optional: Select a YAML file so that the app generates the SQL scripts in the order defined in the "FoldersToProcess" section
- Select the Server URL you want to deploy on, and the default DB. (db objects who have the "Use <DB>" header will be deployed on that DB
- Select the Current Branch and the Source Branch for GIT to do its diffs
- The app will use the diffs to generate SQL statements and deploy on the server

# How to Publish
- Create a personal certificate
- Replace <PackageCertificateThumbprint> in the `DevToolbox.csproj`
- Increment <ApplicationVersion> and <ApplicationDisplayVersion> in the `DevToolbox.csproj`
- Run task `publish-maui-windows`
- *Source*: [Publish a packaged .NET MAUI app for Windows with the CLI](https://learn.microsoft.com/en-us/dotnet/maui/windows/deployment/publish-cli?view=net-maui-10.0)

# TODO
- Add a sortable File List after the diffs have been created
- Implement "Cancel" feature when running SQL queries

