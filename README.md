Install:
 - Download release.zip or build from source code

Manual:
 - Edit config/appsettings.json to specify video path and producer/consumer count
 - Output file will be match_results.csv
---
Task
Write a C# Console App that can capture screenshots from a video and read the data on the scoreboard shown in the video.


Cricket Basic Rules

This is like a turn based where each turn is called a 'Ball'.

An 'Over' consists of 6 'Balls'.

The first team will play 4 'Overs' consisting of 24 (4x6) balls.

The second team will then play 4 'Overs' or until they're total 'Runs' exceeds the amount the first team obtained.

For each ball, there will be an outcome which consists of 2 values:
	- The batting team can score a number of 'Runs'
	- The bowling team can score a 'Wicket'.
	
	

Instructions

-	Open the video at it's natural resolution (1920x1080)
	(This can be done either buy adjusting the monitor is displays on to this resolution and making the video full screen)
-	Map out the coordinates for the ROI's (region of interest) for the parts of the scoreboard that should be read.
-	Read the data and log it in a csv file (or store in memory).
-	Parse the data into a final CSV that contains just one row for each turn with the outcome (runs and wickets).
	(Note: sometimes a 'Ball' can consist of more than one ball throw)
	
	
Libraries
- Tesseract with OpenCVSharp for screenreader text recognition.
---
