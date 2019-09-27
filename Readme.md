# DarkRift CLI Tool
This tool is designed to make DarkRift projects more structured and uniform across all users as well as providing a single interface to simplify common tasks.

To start with, the tool handles three tasks:
- Creating projects and plugins from scratch;
- downloading and installing plugins from online locations; and,
- running a server from centralized binaries.

In future, this tool will likely handle:
- Package management to allow easier download of common plugins (network listeners, log writers etc.)
- Managing remote servers (e.g. executing commands over SSH/HTTP, retrieving logs)

# Installation
To install the DarkRift CLI tool...

TODO content

# Usage
## New
To create a new project or plugin:
```bash
darkrift new project my-project
darkrift new plugin my-plugin
```

## Get
To download and install a plugin from a remote location use:
```bash
darkrift get http://url-of.your/plugin.zip
```
The archive will be decompressed into the `plugins` directory.

Note: this sub-command may be deprecated in the future if/once a  full package management system is implemented.

## Run
To run your project use:
```bash
darkrift run
```

# Development
Pull requests are actively encouraged on all open source DarkRift projects! This section will provide some useful advice for extending or improving the DarkRift CLI tool.

## Project Structure
This is the structure the tool generates for projects:
```
<project>/
  |- .darkrift/
  |- logs/
  |- log-writers/
  |- network-listeners/
  |- plugins/
  |- Readme.md
  '- Server.xml
```

This is the structure the tool generates for plugins:
```
<plugin>/
  |- src/
  |- Readme.md
  '- Server.xml
```

## Templates
Templates are stored in the `templates/` directory and each must be added to the build file (`darkrift-cli.proj`) in order to be copied to the correct location.

There is a very basic templating syntax at the moment to ensure that files (particularly those with special meaning in git) are extracted correctly.

| | | | |
|-|-|-|-|
| **Keep** | `__k__` | This will simply be removed. This is useful for *Keep* a name so that git will only parse it when extracted and not while it is in this repository. | `.gitignore__k__` -> `.gitignore` |
| **Delete** | `__d__` | This will cause a file to be deleted once extracted. This is useful for ensuring folders are tracked by git in this repository but appear empty when the template is extracted. | `.gitkeep__d__` -> *Deleted* |
| **Name** | `__n__` | This will be replaced with the name of the resource that was created. | `__n__.txt` -> `my-project.txt` |
| **Template** | `__c__` | This will simply be removed but templating will occur over the content of the file; any variables defined such as `$__n__` will be resolved. This currently only supports `$__n__` in the file | `file__c__.txt` -> `file.txt` (*But content will be templated*) |
