# DarkRift CLI Tool
This tool is designed to make DarkRift projects more structured and uniform across all users as well as providing a single interface to simplify common tasks.

To start with, the tool handles four common tasks:
- Creating projects, plugins and other resources from scratch;
- downloading and managing different versions of DarkRift;
- downloading and installing plugins from online locations; and,
- running a server from centralized binaries.

In future, this tool will likely handle:
- Package management to allow easier download of common plugins (network listeners, log writers etc.)
- Managing remote servers (e.g. executing commands over SSH/HTTP, retrieving logs)

# Installation
## From Releases (recommended)
If you've downloaded a release version you simply need to extract the archive into a folder and add the folder to your path variable.

You can test your installation with
```bash
darkrift version
```

## From Source
To build from source, clone the repository and run `dotnet publish`. You will then have the same binaries as in the release in the `bin/` folder and can follow the steps above to install.

# Usage
## New
To create a new project etc. from template use:
```bash
darkrift new project my-project
darkrift new plugin my-plugin
darkrift new log-writer my-plugin
darkrift new network-listener my-plugin
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

## Pull
To pull a version of DarkRift from the remote server for use locally you can use:
```bash
darkrift pull 2.4.5
```
This version of DarkRift is than available for use in your projects when starting a server with `darkrift run`. Specifying without a version number will use your current project's version and specifying latest will get the latest version of DarkRift. You can also specify -f to force a download

In most cases you do not need to do this yourself as `darkrift run` will automatically download the correct version.

You can also list all the DarkRift version you have installed with:
```bash
darkrift pull --list
```

## Docs
To access the DarkRift documentation you can simply use:
```bash
darkrift docs 2.4.5
```
Specifying without a version number will use your current project's version and specifying latest will get the documentation for the latest version of DarkRift. You can also add the `--local` flag to download the documentation to your local machine (note, this doesn't seem to render correctly in Firefox though).

# Development
Pull requests are actively encouraged on all open source DarkRift projects! This section will provide some useful advice for extending or improving the DarkRift CLI tool.

An [EditorConfig](https://editorconfig.org/) file is provided that will automatically configure your IDE with the DarkRift style guidelines while working on this project. Many IDEs support it natively but some like VS Code require [an extension](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig).

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
  |- Server.config
  |- Project.xml
  '- .gitignore
```

This is the structure the tool generates for plugins:
```
<plugin>/
  |- src/
  |  '- Plugin1.cs
  |- Readme.md
  |- <plugin>.csproj
  '- .gitignore
```

## Templates
Templates are stored in the `templates/` directory and each must be added to the build file (`darkrift-cli.proj`) in order to be copied to the correct location.

There is a very basic templating syntax at the moment to ensure that files (particularly those with special meaning in git) are extracted correctly.

To enable templating of the file content, use `__c__` in the file path as directed below.

### Valid In File Paths and File Content
| | | | |
|-|-|-|-|
| **Keep** | `__k__` | This will simply be removed. This is useful for making a file that git will only parse it when extracted and not while it is in this repository. | `.gitignore__k__` -> `.gitignore` |
| **Name** | `__n__` | This will be replaced with the name of the resource that was created. | `__n__.txt` -> `my-project.txt` |
| **Version** | `__v__` | This will be replaced with the version of DarkRift being used. | `__v__.txt` -> `2.4.5.txt` |
| **Tier** | `__t__` | This will be replaced with the tier of DarkRift being used. | `__t__.txt` -> `Pro.txt` |
| **Platform** | `__p__` | This will be replaced with the platform the DarkRift being used was build for (`Framework` or `Standard`). | `__p__.txt` -> `Standard.txt` |

### Valid In File Paths Only
| | | | |
|-|-|-|-|
| **Delete** | `__d__` | This will cause a file to be deleted once extracted. This is useful for ensuring folders are tracked by git in this repository but appear empty when the template is extracted. | `.gitkeep__d__` -> *Deleted* |
| **Template** | `__c__` | This will simply be removed but will enable templating of the content of the file; any variables defined such as `$__n__` will be resolved. | `file__c__.txt` -> `file.txt` (*But content will be templated*) |
