#!/bin/bash

#NOTE: this is getting a bit tricky because the gocd user and the root container's permissions don't line up. The files inside the location i mount are invisible.
#      i think i have to copy the files into the container then I can run my commands... this isn't too bad as I can use my original plan and make a local container to do the build.
#      the trick is going to be the publish directory, but I could do a path on the host with everyone access.

# It was a little tricky getting quotes to pass through
# GoCD, so this script is what will be called making the build command simpler.


# with gocd I found some strange issues with the user go that is created by default.
# it is best to create a new user before installing gocd server/agents
# since I didn't an alternative is to copy to a directory as root then have root run the containers.
podman run -it --rm --name msBuild -v ./:/var/opt/ mcr.microsoft.com/dotnet/sdk:8.0 bash -c "cd /var/opt/CellPhoneContactsAPI && dotnet restore && dotnet build -c Release && dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish"
