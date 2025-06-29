# Intro
Note that it is not recommended to create your own user authentication, so this is purely experimental. If you do try running you're own user authentication make sure to comply with the OSWAP Secure Developer checklist. This example code is NOT using HTTPS by default because it was meant to run behind an NGINX proxy. For real usage you will want to make sure the traffic is encrypted. This example is also handling user password hashing in the database. This hashing should be moved to the web app, so the user's plaintext password NEVER leaves the web api in plaintext. 

## Prerequisits
1. Use podman or docker to run postgres and pgadmin4
2. (optional) create a new database to use for the web app
3. Run the CreateTables.sql to create the tables and functions for the web app.
4. update the webconfig to use the connection string.
5. add some users to play with

## Future Additions
1. Move the user password hashing to the web api itself.
2. Add peppers to the user's password. These peppers are stored in the secrets.json on the web app.