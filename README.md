# Legends of Equestria Private Server
Tested on Windows 10 with 27.07.2016 (July 2016) client release.
# Content
Server currently only has testing content (npcs, dialogs, monsters), not any content from the official servers. <br>

# How to use (Server owners only)
You need to use php+mysql web server like <a href="http://sourceforge.net/projects/wampserver/">Wamp Server.</a> Don't use any version newer than version 2.5
<ol>
<li>Execute <i>legends_of_equestria.sql</i> on mysql server.</li>
<li>Compile and run <i>Server.exe</i> for generating default configs file.</li>
<li>For local using:<ul>
<li>Copy <i>connection_s.json</i> to you Legends of Equestria game folder.</i></li>
<li>Replace mysql user and password in <i>loe_server.cfg</i> and <i>config.php</i> by your own.</li></ul></li>
<li>Copy files from <i>www</i> folder to web server <i>www</i> folder.</li>
<li>Run <i>Server.exe</i> and wait full loading them type command: 
<b>user create</b> <ins>login</ins> <ins>password</ins> <ins>access</ins>
<br>access:<ul>
<li>Player = 1</li>
<li>TeamMember = 20</li>
<li>Implementer = 25</li>
<li>Moderator = 30</li>
<li>Admin = 255</li></ul>
Type help for full commands list</li>
<li>?????</li>
<li>PROFIT!</li></ol><br>
# How to Play
If you just interested in playing the game, download the appropriate client using one of the links below. Please read the rules <a href="https://drive.google.com/open?id=1LyqCj58siA432Rljs2TPhqx-6U1yogmtYudrJe7a6iw">here</a> before playing.<br>
<li>Windows 32 bit: https://drive.google.com/open?id=0B3BIzDp2-1UlQ0tLc240UzdzZ2M</li>
<li>Windows 64 bit: https://drive.google.com/open?id=0B3BIzDp2-1UlSGZvU0RPeVpGSW8</li>
<li>OS X: https://drive.google.com/open?id=0B3BIzDp2-1Ulc1dySWhVTE85RU0</li>
<li>Linux: https://drive.google.com/open?id=0B3BIzDp2-1UldnN6eVR2TmpKa2c</li>
# Known Bugs
 Feel free to make a patch or pull request if you've fixed any of these bugs listed below. <br>
<li>  Server is known to crash when someone connects to the server, usually if there were no logins in for the last few hours.</li>
<li>  The server doesn't update files.  In Windows, the server stores its files in C:users/username/appdata/locallow/LoE.  I don't know what the OS X/Linux folders are.  Deleting the files inside the data folder of the LoE folder allows the server to update.  </li>
<li>  Inventory doesn't work on the January 2016 client and older.  
<li>  Lantern Monster model not visible, but still deals damage.  </li>
<li>  New ponies sometimes start off at level 0 instead of level 1. </li>
<li>  Adding friends doesn't work </li>
<li>  Portals fail to work on occasion </li>
<li>  Number of talent points always remains at zero.  
<li>  On occasion using /stuck and respawning after fainting sends players to the portal they arrived at instead of the default spawn point</li>
