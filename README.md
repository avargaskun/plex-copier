## Command Line Syntax

| Option | Notes |
| - | - |
| `-t`, `--target` | **Required** The path to the file or directory to use as input. |
| `-o`, `--options` | The path to the [configuration file](#configuration-file). *Default:* `settings.yaml` |
| `-r`, `--recursive` | Recursively traverse all subfolders when target is a directory. |
| `-w`, `--watch` | Monitor for changes in the specified directory and apply the match rules to any newly created files. |
| `-i`, `--ignore` | Does not attempt to match any files below a given path. This switch can be specified multiple times. **Only applies to watch mode.** |
| `-n`, `--test` | Only print what copy operations would take place but do not actually copy any files. |

### Example: Copy single file

The following example copies a single file into the Plex library

```
dotnet plex-copier.dll -o settings.yaml -t path\to\my\file.mp4
```

### Example: Show what files would be copied from a folder

The following example recursively matches files within a folder but only prints what would be copied to the Plex library (without actually copying the files)

```
dotnet plex-copier.dll -n -o settings.yaml -r -t path\to\my\files
```

### Example: Monitor folder and subfolders for new files

The following example monitors for any files added to a folder or subfolders and copies them to the Plex library, but ignores files from a specific folder

```
dotnet plex-copier.dll -w -o settings.yaml -r -t path\to\monitor -i path\to\monitor\incomplete
```

## Configuration File

A configuration file is required in order to specify important operation parameters. The format of the file is YAML and has the following schema:

```yaml
# The collection points to the path of your Plex library
# This is where files will be copied into
collection: D:\Media\Plex\Library
# The tvDb section holds values used to authenticate with tvdb
# These values are obtained from your TvDB user dashboard
tvDb:
    apiKey: 01234567890ABCD
    userKey: 01234567890ABCD
    userName: my_tvdb_user
# The series section specifies all series that can be recognized
# from the input file(s). The regex patterns are tried in order
# and only the first match is applied
series:    
    - id: 222222                                        # The id of the series from tvdb
      patterns:                                         # One or more regex patterns to match
        - expression: .*Eureka S3 - ([0-9]{2,3}).*      # Should contain a single capture group for episode number
          seasonStart: 3                                # Season number to start counting episodes (default: 1)
          episodeOffset: 2                              # Added to the value captured by the regex (default: 0)
    - id: 333333
      patterns:
        - expressions: .*Dragon Quest (Specials) - ([0-9]+).*
          seasonStart: 0
    # ... etc
```

## Series Metadata

<img src="https://thetvdb.com/images/attribution/logo2.png" width="100">

Metadata for the series is retrieved from TheTVDB. Please consider adding missing information or creating a subscription on their service. You will need to create an API key and store it in the PlexCopier [configuration file](#configuration-file).