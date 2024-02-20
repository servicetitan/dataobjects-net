FROM mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04
USER root

ENV ACCEPT_EULA=Y \
    MSSQL_SA_PASSWORD=dbatools.I0

RUN apt-get update \
    && apt-get -qq install -y gnupg curl \
    && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && echo >>/etc/apt/sources.list.d/mssql-release.list 'deb [arch=amd64,armhf,arm64] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main' \
    && apt-get update

RUN apt-get install -y apt-utils mssql-server-fts
