The error that is happening is the table in the drawing is plotting with '####' in the bottom right most cell. This only occurs when following these exact steps

1.Open the Drawing1.dwg drawing
2.Make a new table by running 'AddTable' command
3.Place it in paper space, delete the old table.
4.Save As to a new folder
5.Select both layouts and run Publish

After the PDF is created, the table that is in paperspace now has '####' in its Total Cell, where before it did not.

This does NOT happen if after using Save As, the file is closed and reopened before running publish.
