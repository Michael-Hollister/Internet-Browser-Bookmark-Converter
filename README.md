##Internet Browser Bookmark Converter##


###Information###

This utility converts Internet Explorer favorites and Mozilla Firefox bookmarks to their differing file and database format.

---

###Command Line Documentation###

"ffc|ffconvert": [bool] : Converts Internet Explorer favorites to Firefox bookmarks

"ffpp|ffprofilepath=": [string] : The path to firefox profile folder

"ffbpe|ffbookmarkexclusions=" : [string] : A space delimited list of string of paths to the folders that will be excluded in the conversion

"iec|ieconvert": [bool] : Converts Firefox bookmarks to Internet Explorer favorites

"iefp|iefavoritespath=": [string] : The path to the favorites folder

"iefpe|iefavoritespathexclusions=" : [string] : A space delimited list of string of paths to the folders that will be excluded in the conversion

"h|help|?": [bool] : Shows the help message

---

###Application Configuarion Documentation###

Key: "backupEnabled" : [bool] : Setting to true enables the backup of Internet Explorer favorites directory and Firefox's bookmark database

Key: "backupDirectory" : [string] : Sets the path to where the backups are stored

Key: "backupMaximumAmountKept" : [int] : Sets the ammount of old backups that are preserved.  if the limit is exceeded, then the old directories will automaticly be deleated.

---

###License###

The MIT License (MIT)

Copyright (c) 2013, Michael Hollister

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.