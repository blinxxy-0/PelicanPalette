# The Pelican Palette Discord server

`setup_server.py` builds the whole server in one go: the name and icon (Sable), five roles, four categories of channels with their permissions, the forums and their tags, Community mode, the onboarding question, and the welcome, rules, FAQ and other pinned text.

The text it posts is in `messages/`. Edit it there before you run the script. `---` on its own line splits a file into separate messages, `{#channel}` becomes a link to that channel.

Check everything without touching Discord:

```
python tools/discord/setup_server.py --dry-run
```

## Running it (about 10 minutes)

**1. Make the server.** In Discord, click **+** at the bottom of the server list, then **Create My Own**, then **For a club or community**. Any name will do; the script renames it.

**2. Make a setup bot.**
1. Go to <https://discord.com/developers/applications> and click **New Application**. Call it *Pelican Palette Setup*.
2. On the **Bot** page, click **Reset Token** and copy the token. Keep it private: it's the bot's password.
3. On the **OAuth2** page, under **OAuth2 URL Generator**, tick **bot**, then under Bot Permissions tick **Administrator**. Open the URL at the bottom, choose your new server, and authorise.

**3. Run the script.** In PowerShell, from the `MuseStardew` folder:

```
$env:DISCORD_BOT_TOKEN = "paste the token here"
python tools\discord\setup_server.py
```

It prints each step. If one fails it says why and carries on; fix it and run it again (re-running is safe: nothing is duplicated, and text is only posted once).

**4. Tidy up.**
- Kick the bot (right-click it in the member list, **Kick**). Its messages stay.
- Back on the bot's page in the Developer Portal, click **Reset Token** once more so the old one is useless, or delete the application.

## After it's built

- **Invite link:** right-click #welcome, **Invite People**, **Edit invite link**, set it to never expire. Put it on the Nexus page and the wiki's Home page.
- **Roles you hand out yourself:** *Moderator* and *Tester* (Server Settings, Members). *Translator* and *Release Pings* members choose themselves in **Channels & Roles**.
- **Announcing a release:** post in #announcements and mention `@Release Pings`. Only you (Mod Author) can ping that role. Click **Publish** on the post so servers following the channel get it too.
- **Bug and suggestion status:** the *Confirmed*, *Fixed next update*, *Can't reproduce*, *Not a bug*, *Planned*, *Added* and *Not planned* tags can only be added by you and moderators.
- **#sneak-peeks** is the vague 1.4 teaser (a new place, reached by bus). Nothing names the Gilded Vale yet.

## What it builds

| Category | Channels |
|---|---|
| Welcome (read-only) | #welcome, #rules, #announcements, #sneak-peeks |
| The Studio | #general, #gallery (forum, gallery view), #off-topic |
| Help | #faq (read-only), #help, #bug-reports, #suggestions (forums) |
| Makers (private) | #translations (Translators), #testers (Testers), #mod-log (Moderators) |

Roles, top to bottom: **Mod Author** (given to you), **Moderator**, **Tester**, **Translator**, **Release Pings**.

Server settings: Community on, members need a verified email, media from everyone is scanned for explicit content, notifications default to mentions only, and members can't ping @everyone.
