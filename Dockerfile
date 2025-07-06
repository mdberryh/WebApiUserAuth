FROM debian:bullseye-slim

WORKDIR /app
COPY ./CellPhoneContactsAPI/publish/ .

# Make the binary executable (just in case)
RUN chmod +x CellPhoneContactsAPI

# Expose the app's port (usually 5000 or 80)
EXPOSE 5000
#NOTE pass in environment variables with podman run -e MySecret=xyz -e ConnectionStrings__DefaultConnection=abc myapp
ENTRYPOINT ["./CellPhoneContactsAPI"] 