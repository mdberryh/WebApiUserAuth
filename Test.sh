# It was a little tricky getting quotes to pass through
# GoCD, so this script is what will be called making the build command simpler.
podman run -it --rm --name msBuild -v ./:/var/opt/ mcr.microsoft.com/dotnet/sdk:8.0 bash -c "cd /var/opt/CellPhoneContactsAPI && dotnet restore && dotnet build -c Release && dotnet test --filter  'CellPhoneContactsAPI.Tests.ControllersTest.ContactsControllerTest'"
