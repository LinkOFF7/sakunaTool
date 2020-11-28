# Usage:

```sh
sakunaTool.exe [argument] <archive> [additional parameter]

Arguments:
-e:      Extracts all files
-p:      Packages a folder to ARC
-i:      Info about archive

Additional parameters:
-nocompress:      Do not compress data during import.
-compress:        Compress the data during import.
```

# Notes:

- This tool uses [lz4net](https://github.com/MiloszKrajewski/lz4net). 
- **sakunaTool** create a cache during extract archivies. It can be finded in Temp folder on system drive (Example: C:\Users\Username\Appdata\Local\Temp\Vsn_Sakuna). This cache uses to keep information about files flags and sort filenames.

> Thanks to **Visntse** for fixes some bugs!
