# Sesame DC/server card allocation

This README covers the server/DC workflow added to Sesame so cards can be
registered once against a domain user and then used from multiple local
workstations.

## What changed

- Service endpoints are configurable with app settings instead of being fixed to
  `127.0.0.1`.
- The server/DC can listen for registration and card lookup traffic on TCP
  `28417`.
- Workstations can keep credential-provider traffic local on TCP `28416` while
  asking the server/DC to resolve unknown cards.
- A new `ResolveToken` message lets a workstation ask the server/DC which user
  and plugin assignment belongs to a swiped card.
- Credential registration now accepts the target username supplied by the
  registration UI, so an admin can assign a card to `DOMAIN\username` on the DC.

## How the pieces fit together

The DC/server stores the card-to-user assignment. Each workstation still needs
the local Sesame service and Windows credential provider because Windows logon
happens on the local device.

At swipe time, a workstation:

1. Reads the card locally.
2. Checks its local Sesame config.
3. If the card is not local, sends `ResolveToken` to the DC/server.
4. Receives the matching user/plugin assignment.
5. Sends credentials to the local Windows credential provider over loopback.

## DC/server setup

Install and run the Sesame service on the DC/server.

In `NFCRing.Service.Host.exe.config` on the DC/server, use:

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

Open firewall access from workstations to the DC/server on TCP `28417`.

Do not expose TCP `28416` from the DC/server. That port is intended for local
credential-provider traffic only.

## Workstation setup

Install the Sesame service and credential provider on each workstation.

In `NFCRing.Service.Host.exe.config` on each workstation, use:

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

Replace `dc01.example.local` with the DNS name of your DC/server.

## Registration UI setup

Run the registration UI from an admin workstation or directly on the DC/server.

In `NFCRing.UI.View.exe.config`, use:

```xml
<appSettings>
  <add key="NFCRing.ServiceHost" value="dc01.example.local"/>
  <add key="NFCRing.RegistrationPort" value="28417"/>
</appSettings>
```

Use the same settings for `CredentialRegistration.exe.config` if you use the
management registration tool.

## Allocate a new card to a new user on the DC

1. Confirm the DC/server Sesame service is running.
2. Confirm TCP `28417` is reachable from the registration workstation.
3. Start the Sesame registration UI or management registration tool configured
   with `NFCRing.ServiceHost` set to the DC/server DNS name.
4. Choose the add/register card flow.
5. In the username field, enter the target domain user as `DOMAIN\username`.
6. When prompted, swipe the new card.
7. Enter the target user's Windows password when prompted.
8. Finish the registration flow.
9. Restart or refresh the Sesame service on any workstation that was already
   running if needed.
10. Test by locking a workstation and swiping the card on that workstation's
    local NFC reader.

The card is now assigned on the DC/server. You do not need to repeat card
registration on every workstation, as long as each workstation points
`NFCRing.ServiceHost` at the DC/server and has remote token lookup enabled.

## Important limits

The current implementation still uses the existing raw TCP/JSON protocol. Use it
only on trusted LAN/domain networks until transport authentication and encryption
are added.

This workflow stores an encrypted Windows credential centrally. The password is
encrypted using the card token, matching the existing Sesame credential model.

The DC/server does not unlock workstations directly. Each workstation must keep
the local service and credential provider installed so Windows logon can complete
locally.
