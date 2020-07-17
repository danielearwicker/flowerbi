# TinyBI

_Ultra-minimal BI analytics query and UI tools_

This is very much an early stage work-in-progress. Stuff is going to be moving around in a whimsical fashion.

## Live Demo

https://earwicker.com/tinybi/demo/

This runs the whole stack in-browser, using some WASM-based components. This is not part of the real solution; no WASM is needed. It's just a way to run a live demo without having to pay to run real boxes!

- [sql.js](https://github.com/sql-js/sql.js) representing the RDBMS
- The dotnet core engine built in [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor), representing an application server
- The UI "fetches" data from the Blazor app, which uses `TinyBI.Engine` to generate SQL queries and runs them against sql.js. It does some ugly hackery to make the queries compatible, as they are currently generated to target Microsoft SQL Server which has a different syntax for many basic things.
