[Power BI](https://powerbi.microsoft.com/) is a proprietary app for building graphical reports from various tabular data sources. It is pretty good as a fast way to throw together something for short-term use. You tell it how tables relate to each other, and drag charts on a page, and they're pretty intuitive to set up because it uses mostly helpful defaults. This is good!

But as a development tool for embedding charting content into a maintained application, it's not so great, because:

- To distribute the same content to your 1000 tenants, you need to deploy 1000 copies of the report definitions into the Power BI service, and repeat this when anything changes. This is so you can set the database connection string and credentials appropriately in each copy.
- There's an API to let you do this kind of mass deployment. Apparently the ability to delete things you previously uploaded is [a wacky idea that people need to vote for before it's implemented](https://community.powerbi.com/t5/Developer/Deleting-a-dashboard-via-the-API/m-p/318564#M9407). Also it takes 5 seconds to accept a new version of a report file.
- The definitions are in a binary proprietary format. Good luck reviewing what your team has changed!
- Some features (dashboards) can't even be downloaded from their service, so you can't store the definitive version of what you deployed in git. The service doesn't have any versioning features.
- Detecting changes even by binary comparison is generally impossible because the service regularly edits your deployed definitions for its own inscrutable reasons.
- They have a feature where you can define your data model in one file and reuse it from several other report files. Want to switch a report file to use a shared model? Throw everything away and start over. It's just easier than trying to fix a broken report. Invisible state gets screwed up, and you can't tell what's wrong.
- Embedding is painful because it uses an iframe and loads slowly - put it inside a draggable tile and it steals mouse events, or reloads from scratch every time you move the tile.
- Want to localise your report so it displays strings in the user's choice of language? You'll need to create multiple copies of all your reports and edit them accordingly, and then keep maintaining those multiple copies. Also this means if you have 1000 tenants and 20 reports in 5 languages, that's 100,000 report files. At 5 seconds each, it's going to take about a week to release a new version of your application.

All this is just a symptom of the focus of the product: it is not aimed at coders building applications, but at non-coders building reports for their own local use. As a result, most of the basic things you depend on when maintaining application code are not possible.
