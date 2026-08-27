# Unity MCP Reference

Project root: `D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022`

The Unity Editor bridge listens on `http://localhost:7777`. The project MCP server is a Node stdio process:

```text
D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server\dist\index.js
```

Its source is under `mcp-server\src`; build it with `npm run build` from `mcp-server` after changing server code.

Available editor operations include connection checks, generic command execution, scene information, GameObject creation, asset import, animation retargeting, project structure, and Unity console logs.

Before an editor mutation, check connectivity. After it, inspect the resulting scene or console logs. If port `7777` refuses connections, Unity is closed or the bridge has not started; do not claim an editor action succeeded.
