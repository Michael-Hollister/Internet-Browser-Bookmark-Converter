===================================================
===== Internet Browser Bookmark Converter 1.0 =====
===================================================

Created by: Mike

===============================================

1.  Information
2.  Command Line Documentation
3.  Application Configuarion Documentation
4.  Version History

===============================================

1: Information

This utility converts Internet Explorer and Mozilla Firefox bookmarks to each respective format.

===============================================

2: Command Line Documentation

"ffc|ffconvert": [bool] : Converts Internet Explorer favorites to Firefox bookmarks
"ffpp|ffprofilepath=": [string] : The path to firefox profile folder
"ffbpe|ffbookmarkexclusions=" : [string] : A space delimited list of string of paths to the folders that will be excluded in the conversion
"iec|ieconvert": [bool] : Converts Firefox bookmarks to Internet Explorer favorites
"iefp|iefavoritespath=": [string] : The path to the favorites folder
"iefpe|iefavoritespathexclusions=" : [string] : A space delimited list of string of paths to the folders that will be excluded in the conversion
"h|help|?": [bool] : Shows the help message

===============================================

3: Application Configuarion Documentation

Key: "backupEnabled" : [bool] : Setting to true enables the backup of Internet Explorer favorites directory and Firefox's bookmark database
Key: "backupDirectory" : [string] : Sets the path to where the backups are stored
Key: "backupMaximumAmountKept" : [int] : Sets the ammount of old backups that are preserved.  if the limit is exceeded, then the old directories will automaticly be deleated.

===============================================

4: Version History

-----
1.2
-----

Made some performance improvements.
Corrected a serious bug that corrupts the firefox database.
Added extra information at the end of the conversion operation.

-----
1.1
-----

Added a syncing system between favories and bookmarks.
Added a backup system before conversion.
Fixed invalid path problems with bookmarks converting to favorites.


-----
1.0
-----

Initial working release.