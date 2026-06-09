# Server deployment notes

This codebase now supports configurable NFCRing service endpoints so a central
server, such as a domain controller, can host registration/config traffic for
domain workstations.

## Roles

- Server/DC: runs the NFCRing service with registration traffic reachable by
  domain devices.
- Workstations: keep the credential provider and local NFCRing service installed.
  Windows logon still needs a local service because the credential provider
  exchanges credentials over the workstation-local credential port.
- UI/management tools: point `NFCRing.ServiceHost` at the server/DC DNS name when
  they should manage central token configuration.

## Server/DC configuration

In `NFCRing.Service.Host.exe.config`, set:

```xml
<appSettings>
  <add key="NFCRing.CredentialBindAddress" value="Loopback"/>
  <add key="NFCRing.CredentialPort" value="28416"/>
  <add key="NFCRing.RegistrationBindAddress" value="Any"/>
  <add key="NFCRing.RegistrationPort" value="28417"/>
  <add key="NFCRing.ServiceHost" value="127.0.0.1"/>
</appSettings>
```

Open TCP port `28417` from the workstation subnet to the server/DC. Do not expose
TCP port `28416`; that port is for workstation-local credential-provider traffic.

## UI/management configuration

In `NFCRing.UI.View.exe.config` or `CredentialRegistration.exe.config`, set:

```xml
<appSettings>
  <add key="NFCRing.ServiceHost" value="dc01.example.local"/>
  <add key="NFCRing.RegistrationPort" value="28417"/>
</appSettings>
```

Replace `dc01.example.local` with the DNS name clients use for the server/DC.

## Current limits

The registration/config socket is still the existing raw TCP/JSON protocol. Use
this only on trusted LAN/domain networks until authentication and transport
encryption are added.

Unlocking still happens on each workstation. A DC cannot directly satisfy the
Windows credential provider on another machine without a workstation agent,
because that provider runs inside the local Windows logon flow.
