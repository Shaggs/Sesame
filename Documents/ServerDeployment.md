# Server deployment notes

This codebase now supports configurable NFCRing service endpoints so a central
server, such as a domain controller, can host registration/config traffic for
domain workstations. Cards can be registered once on the server/DC and then
resolved by each workstation at swipe time.

## Roles

- Server/DC: runs the NFCRing service with registration traffic reachable by
  domain devices.
- Workstations: keep the credential provider and local NFCRing service installed.
  Windows logon still needs a local service because the credential provider
  exchanges credentials over the workstation-local credential port. Configure the
  workstation service to point at the server/DC for remote token lookup.
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
  <add key="NFCRing.EnableRemoteTokenLookup" value="True"/>
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

## Workstation service configuration

In `NFCRing.Service.Host.exe.config` on each workstation, keep local credential
traffic on loopback and point central lookups at the server/DC:

```xml
<appSettings>
  <add key="NFCRing.CredentialBindAddress" value="Loopback"/>
  <add key="NFCRing.CredentialPort" value="28416"/>
  <add key="NFCRing.RegistrationBindAddress" value="Loopback"/>
  <add key="NFCRing.RegistrationPort" value="28417"/>
  <add key="NFCRing.ServiceHost" value="dc01.example.local"/>
  <add key="NFCRing.EnableRemoteTokenLookup" value="True"/>
</appSettings>
```

With that setup, the workstation first checks its own local config. If the token
is not local, it sends `ResolveToken` to the server/DC and runs the returned
plugin assignment locally.

## Assigning a card once

Run the registration UI/management tool with `NFCRing.ServiceHost` set to the
server/DC. Register the card against the domain user, preferably in
`DOMAIN\username` format. The server stores the salted card hash and plugin
assignment once; any workstation configured for remote lookup can use that
assignment.

## Current limits

The registration/config socket is still the existing raw TCP/JSON protocol. Use
this only on trusted LAN/domain networks until authentication and transport
encryption are added.

Unlocking still happens on each workstation. A DC cannot directly satisfy the
Windows credential provider on another machine without a workstation agent,
because that provider runs inside the local Windows logon flow.
