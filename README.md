# TfsInfoService

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
