# sakunaTool

Tool for extraction and packing .arc archivies of **Sakuna of Rice and Ruin** game.

# Usage:

```sh
sakunaTool.exe [argument] <archive> [additional parameter]

Arguments:
-e:      Extracts all files
-p:      Packages a folder to a ARC
-i:      Info about archive

Additional parameters:
-nocompress:      Do not compress data during import.
-compress:        Compress the data during import.
```

# Notes:

This tool uses [lz4net](https://github.com/MiloszKrajewski/lz4net). 

> Thanks to **Visntse** for fixes files flags!
