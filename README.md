# TfsInfoService

## Overview

A service that can provide additional information on TFS (builds) that are not provided
by the stock VSTS or TFS services.

The general URL syntax looks like this:

     http://<servicehost>:<port>/_apis/infos/<project-guid>/<build-id>/<type>/badge[?<parameters>]

For example, one could get a code coverage badge like so:

     http://<servicehost>:<port>/_apis/infos/<project-guid>/<build-id>/coverage/badge
     
You could create a custom badge:

     http://<servicehost>:<port>/_apis/infos/<project-guid>/<build-id>/custom/badge?title=time&value=12:00
     
Note that for `custom` badges you can use any valid guid as `<project-guid>` and any integer as `build-id`.
Additionally, in this case no connection to a TFS/VSTS instance is made, so you can use it for tests, etc.
     
For available information simply invoke 

     http://<servicehost>:<port>/_apis/infos
     
Additional URL parameters that can be used to tweak to look of the badges:

* `title`: set a custom title for the badge (the "left" text of the badge)
* `titlefg`: specify a custom foreground color (e.g. `#fff`) for the title.
* `titlebg`: specify a custom background color (e.g. `#fff`) for the title.
* `valuefg`: specify a custom foreground color (e.g. `#fff`) for the value.
* `valuebg`: specify a custom background color (e.g. `#fff`) for the value.
* `value`: specify a custom value (only used with type `custom`).

## Configuration

The `appsettings.json` file contains the configuration options to apply. Appart from .NET build in
options, there are currently only two settings in the `tfs` section:

* `tfs:ServerUrl`: the URL of the TFS/VSTS server, including the collection name (e.g. "http://localhost:8080/tfs/DefaultCollection")
* `tfs:Token`: the personal access token (PAT) that the service uses to access the TFS/VSTS instance.
Check the [Microsoft Documentation](https://docs.microsoft.com/de-de/vsts/accounts/use-personal-access-tokens-to-authenticate) for further information

You can also specify options as command line parameters, for example (`--urls` specifies the listen addresses of the service):

```
.\TfsInfoService.exe --urls "http://localhost:4711/" --tfs.ServerUrl "https://..." --tfs.Token "...."
```

## Security

The stock .NET client API for TFS/VSTS doesn't allow the usage of unencrypted (non-https) server connections
together with basic authentication methods (which PAT is a part of) to prevent network sniffing of the
PAT (or username / password). While this is absolutely correct to do, enforcing it can hinder the usage
in corporate or local test environments, where having TFS running with HTTPS is sometimes not possible.
(Corporate IT-departments sometimes have their own idea about such things).

Therefore, the TfsInfoService provides its own credentials class that _does_ allow usage of PAT over
un-encrypted connections. **Remember that when you target a TFS server that is not on your local machine
or you just feel reluctant to do this. In this case consider only connecting the HTTPS based TFS server
URLs.**

## Running

The implementation will detect if being run manually, interactively, by a user as a foreground process.

```
.\TfsInfoService.exe
```

Likewise it can also be run as a service. To do so use the `sc.exe` tool to register the application as
a service (you need to run these commands as admin):

```cmd
C:\> sc.exe create TfsInfoService binPath= "<fullpath>\TfsInfoService.exe"
```
Note the space between (binPath= and the actual path). `sc.exe` has more options to tailer aspects of
the service (start type, user, etc.). Use `sc.exe create /?` for details.

The use tools of choice to start/stop the service:

```cmd
C:\> sc.exe start TfsInfoService
```
