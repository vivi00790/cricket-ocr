# The Sample Data is wrong!!
<img width="1308" height="212" alt="image" src="https://github.com/user-attachments/assets/e7406243-6e94-4665-84be-92aedcaa8da7" />
<img width="1328" height="213" alt="image" src="https://github.com/user-attachments/assets/c72fbf3c-a06b-40b1-9da4-55968ee16893" />
<img width="1228" height="204" alt="image" src="https://github.com/user-attachments/assets/0f63374e-215f-4a1a-9edc-46d9ada04b05" />
<img width="1209" height="218" alt="image" src="https://github.com/user-attachments/assets/6fad8489-bff8-4a40-a839-c65bb8459746" />
<img width="1277" height="195" alt="image" src="https://github.com/user-attachments/assets/037ba512-2486-4d39-89b5-ec43ca6c8ed3" />

# As the screenshot shows, 2.0 should be: P. Cummings Bowler, D. Malan First Batter, J. Roy Second Batter
<img width="858" height="144" alt="image" src="https://github.com/user-attachments/assets/a68f33b8-fa4d-4a01-ba4a-77061665d6b6" />

---
Install:
- Download [release.zip](https://drive.google.com/file/d/14QlPcUjy1G_xxKmMCscdozL6c3DzhkNC/view?usp=sharing) or build from source code

Manual:
- Edit config/appsettings.json to specify video path and producer/consumer count
- For local video file, put at root folder
- Run CricketScoreReader.exe
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
