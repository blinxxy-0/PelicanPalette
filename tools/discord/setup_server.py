"""Builds the Pelican Palette Discord server: roles, categories, channels,
forum tags, permissions, Community mode, onboarding and the pinned text.

Run it once against a new, empty server, with a bot you've invited to it
(see README.md). Re-running is safe: it finds what already exists by name,
fixes its settings, and only posts text to channels the bot hasn't posted in.

    python setup_server.py --dry-run     # no Discord calls; checks the text
    python setup_server.py               # needs DISCORD_BOT_TOKEN set

Standard library only.
"""

import argparse
import base64
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

HERE = Path(__file__).resolve().parent
API = "https://discord.com/api/v10"
USER_AGENT = "DiscordBot (https://github.com/blinxxy-0/PelicanPalette, 1.0)"

SERVER_NAME = "Pelican Palette"
SERVER_DESCRIPTION = (
    "The community for Pelican Palette, the Stardew Valley painting mod: "
    "share your paintings, get help and report bugs."
)

# --- permissions ---------------------------------------------------------
P = {
    "CREATE_INSTANT_INVITE": 1 << 0, "KICK_MEMBERS": 1 << 1, "BAN_MEMBERS": 1 << 2,
    "MANAGE_CHANNELS": 1 << 4, "ADD_REACTIONS": 1 << 6, "VIEW_AUDIT_LOG": 1 << 7,
    "STREAM": 1 << 9, "VIEW_CHANNEL": 1 << 10, "SEND_MESSAGES": 1 << 11,
    "MANAGE_MESSAGES": 1 << 13, "EMBED_LINKS": 1 << 14, "ATTACH_FILES": 1 << 15,
    "READ_MESSAGE_HISTORY": 1 << 16, "MENTION_EVERYONE": 1 << 17,
    "USE_EXTERNAL_EMOJIS": 1 << 18, "CONNECT": 1 << 20, "SPEAK": 1 << 21,
    "USE_VAD": 1 << 25, "CHANGE_NICKNAME": 1 << 26, "MANAGE_NICKNAMES": 1 << 27,
    "USE_APPLICATION_COMMANDS": 1 << 31, "MANAGE_THREADS": 1 << 34,
    "CREATE_PUBLIC_THREADS": 1 << 35, "CREATE_PRIVATE_THREADS": 1 << 36,
    "USE_EXTERNAL_STICKERS": 1 << 37, "SEND_MESSAGES_IN_THREADS": 1 << 38,
    "MODERATE_MEMBERS": 1 << 40, "SEND_VOICE_MESSAGES": 1 << 46, "SEND_POLLS": 1 << 49,
}


def perms(*names):
    v = 0
    for n in names:
        v |= P[n]
    return v


# What every member can do. Leaves out @everyone/@here pings and TTS.
EVERYONE = perms(
    "CREATE_INSTANT_INVITE", "ADD_REACTIONS", "STREAM", "VIEW_CHANNEL", "SEND_MESSAGES",
    "EMBED_LINKS", "ATTACH_FILES", "READ_MESSAGE_HISTORY", "USE_EXTERNAL_EMOJIS",
    "CONNECT", "SPEAK", "USE_VAD", "CHANGE_NICKNAME", "USE_APPLICATION_COMMANDS",
    "CREATE_PUBLIC_THREADS", "USE_EXTERNAL_STICKERS", "SEND_MESSAGES_IN_THREADS",
    "SEND_VOICE_MESSAGES", "SEND_POLLS",
)
MODERATION = perms(
    "KICK_MEMBERS", "BAN_MEMBERS", "VIEW_AUDIT_LOG", "MANAGE_MESSAGES",
    "MANAGE_NICKNAMES", "MANAGE_THREADS", "MODERATE_MEMBERS",
)
POSTING = perms("SEND_MESSAGES", "SEND_MESSAGES_IN_THREADS", "CREATE_PUBLIC_THREADS",
                "CREATE_PRIVATE_THREADS")

# --- roles, highest first --------------------------------------------------
ROLES = [
    # The author can ping @Release Pings (MENTION_EVERYONE); members can't.
    {"name": "Mod Author", "color": 0xE3A83B, "hoist": True,
     "permissions": MODERATION | perms("MANAGE_CHANNELS", "MENTION_EVERYONE"),
     "give_to_owner": True},
    {"name": "Moderator", "color": 0x3BA7A0, "hoist": True, "permissions": MODERATION},
    {"name": "Tester", "color": 0x9B7BD4, "hoist": False, "permissions": 0},
    {"name": "Translator", "color": 0x6FAF5B, "hoist": False, "permissions": 0},
    {"name": "Release Pings", "color": 0, "hoist": False, "permissions": 0},
]

# --- channels --------------------------------------------------------------
# access: "public", "readonly" (only the Mod Author posts) or a list of the
# roles that can see a private channel (the Mod Author always can).
TEXT, CATEGORY, ANNOUNCEMENT, FORUM = 0, 4, 5, 15

CATEGORIES = [
    {"name": "Welcome", "channels": [
        {"name": "welcome", "type": TEXT, "access": "readonly", "message": "welcome.md",
         "topic": "What Pelican Palette is, where to get it, and where everything is here."},
        {"name": "rules", "type": TEXT, "access": "readonly", "message": "rules.md",
         "topic": "Please read before posting."},
        {"name": "announcements", "type": ANNOUNCEMENT, "access": "readonly",
         "message": "announcements.md",
         "topic": "New versions of Pelican Palette. Choose Release Pings in Channels & Roles to be pinged."},
        {"name": "sneak-peeks", "type": TEXT, "access": "readonly", "message": "sneak-peeks.md",
         "topic": "What's on the easel next."},
    ]},
    {"name": "The Studio", "channels": [
        {"name": "general", "type": TEXT, "access": "public",
         "topic": "Talk about Pelican Palette, Stardew Valley and painting."},
        {"name": "gallery", "type": FORUM, "access": "public", "layout": "gallery",
         "reaction": "\U0001F3A8",  # artist palette
         "topic": ("Show us what you've painted! One post per painting (or per set), with a "
                   "screenshot. Tag it so people can find it. Say which mode you painted it on "
                   "if you're proud of it."),
         "tags": [("Still life", False), ("Portrait", False), ("Plein air", False),
                  ("Animals", False), ("Festival", False), ("Masterpiece", False),
                  ("Restoration", False), ("Framed & hung", False), ("Hard mode", False),
                  ("Funny", False)]},
        {"name": "off-topic", "type": TEXT, "access": "public",
         "topic": "Anything that isn't about the mod. The rules still apply."},
    ]},
    {"name": "Help", "channels": [
        {"name": "faq", "type": TEXT, "access": "readonly", "message": "faq.md",
         "topic": "Quick answers. The full guide is on the wiki."},
        {"name": "help", "type": FORUM, "access": "public", "require_tag": True,
         "topic": ("Stuck? Start a post. Check #faq and the wiki first: "
                   "https://github.com/blinxxy-0/PelicanPalette/wiki\n\n"
                   "If something won't load, upload your SMAPI log at https://smapi.io/log "
                   "and paste the link. When you're sorted, add the Solved tag."),
         "tags": [("Installing", False), ("Painting", False), ("Paint & palettes", False),
                  ("Sable", False), ("Margot", False), ("Restoration", False),
                  ("Multiplayer", False), ("Other mods", False), ("Solved", False)]},
        {"name": "bug-reports", "type": FORUM, "access": "public", "require_tag": True,
         "reaction": "\U0001F41B",  # bug
         "topic": ("One bug per post. Search first: it may be reported already.\n\n"
                   "Please include:\n"
                   "- your Pelican Palette version,\n"
                   "- what you did, what you expected, and what happened,\n"
                   "- your SMAPI log: upload it at https://smapi.io/log right after the bug "
                   "(before restarting the game) and paste the link,\n"
                   "- whether you play with Stardew Valley Expanded or other big mods.\n\n"
                   "Pick a tag for what it's about. The Mod Author adds the status tags."),
         "tags": [("Painting", False), ("Shop & items", False), ("NPCs & events", False),
                  ("Museum & restoration", False), ("Multiplayer", False),
                  ("Other mods", False), ("Crash", False),
                  ("Confirmed", True), ("Fixed next update", True),
                  ("Can't reproduce", True), ("Not a bug", True)]},
        {"name": "suggestions", "type": FORUM, "access": "public", "require_tag": True,
         "reaction": "\U0001F44D",  # thumbs up
         "topic": ("One idea per post. React with \U0001F44D to the ideas you'd like most. "
                   "Search first, and add to an existing post rather than starting a new one."),
         "tags": [("Painting", False), ("Items & crafting", False), ("NPCs & events", False),
                  ("Quality of life", False), ("Other mods", False),
                  ("Planned", True), ("Added", True), ("Not planned", True)]},
    ]},
    {"name": "Makers", "channels": [
        {"name": "translations", "type": TEXT, "access": ["Translator", "Moderator"],
         "message": "translations.md",
         "topic": "For people translating the mod. Say which language you're working on."},
        {"name": "testers", "type": TEXT, "access": ["Tester"], "message": "testers.md",
         "topic": "Early builds. Nothing here leaves this channel."},
        {"name": "mod-log", "type": TEXT, "access": ["Moderator"],
         "topic": "Moderators only. Discord also posts its Community updates here."},
    ]},
]

# Shown on the onboarding screen and in Channels & Roles.
ONBOARDING_PROMPT = {
    "title": "What would you like?",
    "options": [
        {"title": "Ping me for new versions", "role": "Release Pings", "emoji": "\U0001F514",
         "description": "Get a notification when a new version is announced."},
        {"title": "I'd like to help translate", "role": "Translator", "emoji": "\U0001F310",
         "description": "Opens the translations channel."},
    ],
}

# Discord's starter channels on a new "Create My Own" server.
DISCORD_DEFAULTS = {(CATEGORY, "Text Channels"), (CATEGORY, "Voice Channels"), (2, "General")}

FORUM_TOPIC_MAX, TEXT_TOPIC_MAX, MESSAGE_MAX = 4096, 1024, 2000


# --- message text ------------------------------------------------------------
def load_message(filename):
    text = (HERE / "messages" / filename).read_text(encoding="utf-8").strip()
    return [part.strip() for part in re.split(r"^---$", text, flags=re.M) if part.strip()]


def fill_placeholders(text, channel_ids, role_ids):
    def chan(m):
        name = m.group(1)
        if name not in channel_ids:
            raise KeyError(f"unknown channel {{#{name}}}")
        return f"<#{channel_ids[name]}>"

    def role(m):
        name = m.group(1)
        if name not in role_ids:
            raise KeyError(f"unknown role {{@{name}}}")
        return f"<@&{role_ids[name]}>"

    text = re.sub(r"\{#([a-z0-9-]+)\}", chan, text)
    return re.sub(r"\{@([A-Za-z ]+)\}", role, text)


def all_channels():
    for cat in CATEGORIES:
        for ch in cat["channels"]:
            yield cat, ch


def check_text():
    """Everything Discord would reject for length or a bad placeholder."""
    fake_ids = {ch["name"]: "1" * 19 for _, ch in all_channels()}
    fake_roles = {r["name"]: "2" * 19 for r in ROLES}
    problems = []
    for _, ch in all_channels():
        limit = FORUM_TOPIC_MAX if ch["type"] == FORUM else TEXT_TOPIC_MAX
        if len(ch.get("topic", "")) > limit:
            problems.append(f"#{ch['name']}: topic is {len(ch['topic'])} chars (max {limit})")
        if len(ch.get("tags", [])) > 20:
            problems.append(f"#{ch['name']}: more than 20 tags")
        for tag, _ in ch.get("tags", []):
            if len(tag) > 20:
                problems.append(f"#{ch['name']}: tag '{tag}' is over 20 chars")
        if "message" in ch:
            for i, part in enumerate(load_message(ch["message"]), 1):
                try:
                    filled = fill_placeholders(part, fake_ids, fake_roles)
                except KeyError as e:
                    problems.append(f"{ch['message']} part {i}: {e}")
                    continue
                if len(filled) > MESSAGE_MAX:
                    problems.append(f"{ch['message']} part {i}: {len(filled)} chars (max {MESSAGE_MAX})")
    return problems


# --- Discord REST ------------------------------------------------------------
class Discord:
    def __init__(self, token):
        self.token = token

    def call(self, method, path, body=None, reason="Pelican Palette server setup"):
        data = None if body is None else json.dumps(body).encode("utf-8")
        for attempt in range(6):
            req = urllib.request.Request(API + path, data=data, method=method)
            req.add_header("Authorization", f"Bot {self.token}")
            req.add_header("User-Agent", USER_AGENT)
            req.add_header("X-Audit-Log-Reason", reason)
            if data is not None:
                req.add_header("Content-Type", "application/json")
            try:
                with urllib.request.urlopen(req, timeout=30) as resp:
                    raw = resp.read()
                    return json.loads(raw) if raw else None
            except urllib.error.HTTPError as e:
                raw = e.read().decode("utf-8", "replace")
                if e.code == 429:
                    try:
                        wait = float(json.loads(raw).get("retry_after", 1)) + 0.25
                    except ValueError:
                        wait = 5
                    print(f"  (Discord asked us to slow down; waiting {wait:.0f}s)", flush=True)
                    time.sleep(wait)
                    continue
                raise DiscordError(method, path, e.code, raw) from None
            except (urllib.error.URLError, TimeoutError, OSError) as e:
                reason = getattr(e, "reason", e)
                if attempt < 2:
                    print(f"  (no answer from Discord: {reason}; trying again)", flush=True)
                    continue
                raise DiscordError(method, path, 0, f"couldn't reach Discord: {reason}") from None
        raise DiscordError(method, path, 429, "still rate limited after retries")


class DiscordError(Exception):
    def __init__(self, method, path, code, body):
        super().__init__(f"{method} {path} -> {code}: {body}")
        self.code = code


def snowflake(counter=[0]):
    counter[0] += 1
    return str(((int(time.time() * 1000) - 1420070400000) << 22) + counter[0])


# --- setup steps ---------------------------------------------------------------
def pick_guild(api, guild_id):
    if guild_id:
        return api.call("GET", f"/guilds/{guild_id}")
    guilds = api.call("GET", "/users/@me/guilds")
    if not guilds:
        sys.exit("The bot isn't in any server yet. Invite it with the OAuth2 URL Generator link "
                 "(tick 'bot' and 'Administrator'), choose your new server, then re-run.")
    if len(guilds) != 1:
        names = ", ".join(f"{g['name']} ({g['id']})" for g in guilds)
        sys.exit(f"The bot is in {len(guilds)} servers ({names}). Pass --guild <server id>.")
    return api.call("GET", f"/guilds/{guilds[0]['id']}")


def setup_roles(api, gid, guild):
    existing = {r["name"]: r for r in api.call("GET", f"/guilds/{gid}/roles")}
    ids = {}
    for spec in ROLES:
        body = {"name": spec["name"], "color": spec["color"], "hoist": spec["hoist"],
                "permissions": str(spec["permissions"]), "mentionable": False}
        if spec["name"] in existing:
            role = api.call("PATCH", f"/guilds/{gid}/roles/{existing[spec['name']]['id']}", body)
            print(f"  role {spec['name']}: updated")
        else:
            role = api.call("POST", f"/guilds/{gid}/roles", body)
            print(f"  role {spec['name']}: created")
        ids[spec["name"]] = role["id"]

    # Order them under the bot's own role, highest first.
    all_roles = api.call("GET", f"/guilds/{gid}/roles")
    bot_role = next((r for r in all_roles if r.get("tags", {}).get("bot_id")), None)
    top = bot_role["position"] if bot_role else len(ROLES) + 1
    positions = [{"id": ids[s["name"]], "position": max(1, top - 1 - i)} for i, s in enumerate(ROLES)]
    try:
        api.call("PATCH", f"/guilds/{gid}/roles", positions)
    except DiscordError as e:
        print(f"  ! couldn't reorder roles ({e.code}); drag them into order in Server Settings > Roles")

    # @everyone's base permissions.
    api.call("PATCH", f"/guilds/{gid}/roles/{gid}", {"permissions": str(EVERYONE)})
    print("  @everyone: base permissions set (no @everyone pings, no TTS)")

    for spec in ROLES:
        if spec.get("give_to_owner"):
            try:
                api.call("PUT", f"/guilds/{gid}/members/{guild['owner_id']}/roles/{ids[spec['name']]}")
                print(f"  gave {spec['name']} to the server owner")
            except DiscordError as e:
                print(f"  ! couldn't give {spec['name']} to you ({e.code}); add it in Server Settings > Members")
    return ids


def overwrites_for(access, gid, role_ids):
    author = role_ids["Mod Author"]
    if access == "public":
        return []
    if access == "readonly":
        return [{"id": gid, "type": 0, "allow": "0", "deny": str(POSTING)},
                {"id": author, "type": 0, "allow": str(POSTING), "deny": "0"}]
    see = perms("VIEW_CHANNEL")
    rows = [{"id": gid, "type": 0, "allow": "0", "deny": str(see)},
            {"id": author, "type": 0, "allow": str(see), "deny": "0"}]
    for name in access:
        rows.append({"id": role_ids[name], "type": 0, "allow": str(see), "deny": "0"})
    return rows


def channel_body(ch, gid, role_ids, parent_id, position, tag_ids=None):
    body = {"name": ch["name"], "parent_id": parent_id, "position": position,
            "topic": ch.get("topic", ""),
            "permission_overwrites": overwrites_for(ch["access"], gid, role_ids)}
    if ch["type"] == FORUM:
        tags = []
        for name, moderated in ch.get("tags", []):
            tag = {"name": name, "moderated": moderated}
            if tag_ids and name in tag_ids:
                tag["id"] = tag_ids[name]  # keep existing tags (and the posts using them)
            tags.append(tag)
        body["available_tags"] = tags
        body["default_sort_order"] = 0  # latest activity
        if ch.get("layout") == "gallery":
            body["default_forum_layout"] = 2
        if ch.get("reaction"):
            body["default_reaction_emoji"] = {"emoji_name": ch["reaction"]}
    return body


def setup_channels(api, gid, role_ids, community):
    existing = api.call("GET", f"/guilds/{gid}/channels")
    by_name = {}
    for c in existing:
        if c["type"] in (2, 13):  # voice and stage channels aren't ours
            continue
        by_name.setdefault((c["type"] == CATEGORY, c["name"].lower()), c)
    ids = {}
    created = set()
    for cpos, cat in enumerate(CATEGORIES):
        key = (True, cat["name"].lower())
        body = {"name": cat["name"], "type": CATEGORY, "position": cpos}
        if key in by_name:
            parent = api.call("PATCH", f"/channels/{by_name[key]['id']}", body)
        else:
            parent = api.call("POST", f"/guilds/{gid}/channels", body)
        print(f"  category {cat['name']}")
        for pos, ch in enumerate(cat["channels"]):
            # Announcement channels need Community; until then it's a text channel.
            want_type = ch["type"]
            if want_type == ANNOUNCEMENT and not community:
                want_type = TEXT
            old = by_name.get((False, ch["name"]))
            tag_ids = {t["name"]: t["id"] for t in (old or {}).get("available_tags", [])}
            body = channel_body(ch, gid, role_ids, parent["id"], pos, tag_ids)
            if old:
                if old["type"] != want_type and {old["type"], want_type} <= {TEXT, ANNOUNCEMENT}:
                    body["type"] = want_type
                elif old["type"] != want_type:
                    print(f"  ! #{ch['name']} exists as a different kind of channel; rename or delete it and re-run")
                    continue
                result = api.call("PATCH", f"/channels/{old['id']}", body)
                print(f"    #{ch['name']}: updated")
            else:
                body["type"] = want_type
                result = api.call("POST", f"/guilds/{gid}/channels", body)
                created.add(ch["name"])
                print(f"    #{ch['name']}: created")
            if ch.get("require_tag"):
                api.call("PATCH", f"/channels/{result['id']}", {"flags": 1 << 4})
            ids[ch["name"]] = result["id"]
    return ids, created


def remove_discord_defaults(api, gid, ours):
    """Deletes the starter 'Text Channels'/'Voice Channels' categories and the
    'General' voice channel a new server comes with. Our #general is kept."""
    for c in list(api.call("GET", f"/guilds/{gid}/channels")):
        if c["id"] in ours:
            continue
        if (c["type"], c["name"]) in DISCORD_DEFAULTS:
            api.call("DELETE", f"/channels/{c['id']}")
            print(f"  removed Discord's starter {'category' if c['type'] == CATEGORY else 'channel'} '{c['name']}'")


def enable_community(api, gid, guild, channel_ids):
    if "COMMUNITY" in guild.get("features", []):
        print("  already a Community server")
        return True
    body = {
        "features": sorted(set(guild.get("features", [])) | {"COMMUNITY"}),
        "rules_channel_id": channel_ids["rules"],
        "public_updates_channel_id": channel_ids["mod-log"],
        "verification_level": 1,          # members need a verified email
        "explicit_content_filter": 2,     # scan media from everyone
        "default_message_notifications": 1,  # only @mentions by default
        "description": SERVER_DESCRIPTION,
    }
    try:
        api.call("PATCH", f"/guilds/{gid}", body)
        print("  Community turned on (rules: #rules, Discord's updates: #mod-log)")
        return True
    except DiscordError as e:
        print(f"  ! couldn't turn on Community ({e}).\n"
              "    Turn it on in Server Settings > Enable Community, then re-run.")
        return False


def setup_onboarding(api, gid, role_ids, channel_ids):
    defaults = [channel_ids[n] for n in channel_ids if n not in ("translations", "testers", "mod-log")]
    prompt = {
        "id": snowflake(), "type": 0, "title": ONBOARDING_PROMPT["title"],
        "single_select": False, "required": False, "in_onboarding": True,
        "options": [{"id": snowflake(), "title": o["title"], "description": o["description"],
                     "emoji_name": o["emoji"],
                     "role_ids": [role_ids[o["role"]]], "channel_ids": []}
                    for o in ONBOARDING_PROMPT["options"]],
    }
    body = {"prompts": [prompt], "default_channel_ids": defaults, "enabled": True, "mode": 0}
    try:
        api.call("PUT", f"/guilds/{gid}/onboarding", body)
        print("  onboarding on: new members pick Release Pings / Translator")
    except DiscordError as e:
        print(f"  ! onboarding couldn't be switched on ({e.code}): {e}")
        body["enabled"] = False
        try:
            api.call("PUT", f"/guilds/{gid}/onboarding", body)
            print("    saved it switched off; turn it on in Server Settings > Onboarding")
        except DiscordError as e2:
            print(f"    couldn't save it either ({e2.code}); set it up in Server Settings > Onboarding")


def post_messages(api, me_id, channel_ids, role_ids):
    for _, ch in all_channels():
        if "message" not in ch:
            continue
        cid = channel_ids.get(ch["name"])
        if not cid:
            continue
        recent = api.call("GET", f"/channels/{cid}/messages?limit=50")
        if any(m["author"]["id"] == me_id for m in recent):
            print(f"  #{ch['name']}: already posted, skipped")
            continue
        posted = []
        for part in load_message(ch["message"]):
            msg = api.call("POST", f"/channels/{cid}/messages", {
                "content": fill_placeholders(part, channel_ids, role_ids),
                "allowed_mentions": {"parse": []},
                "flags": 1 << 2,  # no link previews
            })
            posted.append(msg)
        try:
            api.call("PUT", f"/channels/{cid}/pins/{posted[0]['id']}")
        except DiscordError:
            pass
        print(f"  #{ch['name']}: posted {len(posted)} message(s)")


def set_icon_and_name(api, gid):
    icon = HERE / "server_icon.png"
    body = {"name": SERVER_NAME}
    if icon.exists():
        body["icon"] = "data:image/png;base64," + base64.b64encode(icon.read_bytes()).decode()
    api.call("PATCH", f"/guilds/{gid}", body)
    print(f"  name '{SERVER_NAME}'" + (", icon set (Sable)" if icon.exists() else ""))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true", help="check the layout and text; no Discord calls")
    ap.add_argument("--guild", help="server id, if the bot is in more than one")
    ap.add_argument("--no-community", action="store_true", help="don't turn on Community or onboarding")
    ap.add_argument("--keep-defaults", action="store_true", help="keep Discord's starter categories and voice channel")
    args = ap.parse_args()

    problems = check_text()
    if problems:
        print("Fix these first:")
        for p in problems:
            print("  - " + p)
        sys.exit(1)

    if args.dry_run:
        print("Layout and text are fine. This is what would be built:\n")
        print("Roles: " + ", ".join(r["name"] for r in ROLES))
        for cat in CATEGORIES:
            print(f"\n{cat['name'].upper()}")
            for ch in cat["channels"]:
                kind = {TEXT: "text", ANNOUNCEMENT: "announcement", FORUM: "forum"}[ch["type"]]
                acc = ch["access"] if isinstance(ch["access"], str) else "private: " + ", ".join(ch["access"])
                extra = f", {len(ch['tags'])} tags" if ch.get("tags") else ""
                extra += f", posts {ch['message']}" if ch.get("message") else ""
                print(f"  #{ch['name']:<14} {kind}, {acc}{extra}")
        return

    try:
        run(args)
    except DiscordError as e:
        if e.code == 401:
            sys.exit("\nDiscord didn't accept the token. On the bot's page in the Developer Portal, "
                     "click Reset Token, copy the new one, set DISCORD_BOT_TOKEN again and re-run.")
        if e.code == 0:
            sys.exit(f"\n{e}\nCheck your internet connection (or VPN/firewall) and re-run.")
        sys.exit(f"\nDiscord refused a step: {e}\nRe-running is safe; paste this message if it keeps happening.")


def token_from_env_file():
    """tools/discord/.env (gitignored): a line DISCORD_BOT_TOKEN=..."""
    path = HERE / ".env"
    if not path.exists():
        return ""
    for line in path.read_text(encoding="utf-8").splitlines():
        key, _, value = line.partition("=")
        if key.strip() == "DISCORD_BOT_TOKEN":
            return value
    return ""


def run(args):
    token = os.environ.get("DISCORD_BOT_TOKEN", "") or token_from_env_file()
    token = token.strip().strip('"').strip("'").strip()
    if token.lower().startswith("bot "):
        token = token[4:].strip()
    if not token:
        sys.exit("DISCORD_BOT_TOKEN isn't set in this window. Run this first, in the same PowerShell window:\n"
                 '  $env:DISCORD_BOT_TOKEN = "paste the token here"')
    if token.count(".") != 2:
        sys.exit("That doesn't look like a bot token (it should be three parts separated by dots). "
                 "Copy it from the Bot page in the Developer Portal (Reset Token), not the Application ID "
                 "or the Client Secret.")
    api = Discord(token)
    print("Connecting to Discord...", flush=True)
    me = api.call("GET", "/users/@me")
    print(f"  logged in as the bot {me['username']}", flush=True)
    guild = pick_guild(api, args.guild)
    gid = guild["id"]
    print(f"Setting up '{guild['name']}' ({gid})\n", flush=True)

    n_channels = len(api.call("GET", f"/guilds/{gid}/channels"))
    if n_channels > 25:
        sys.exit(f"This server already has {n_channels} channels. Run this on a new, empty server.")

    print("Server")
    set_icon_and_name(api, gid)
    community = "COMMUNITY" in guild.get("features", [])
    print("Roles")
    role_ids = setup_roles(api, gid, guild)
    print("Channels")
    channel_ids, _ = setup_channels(api, gid, role_ids, community)
    if not args.keep_defaults:
        remove_discord_defaults(api, gid, set(channel_ids.values()))
    if not args.no_community:
        print("Community")
        if enable_community(api, gid, guild, channel_ids) and not community:
            community = True
            # Now that it's allowed, make #announcements a real announcement channel.
            channel_ids, _ = setup_channels(api, gid, role_ids, community)
        if community:
            print("Onboarding")
            setup_onboarding(api, gid, role_ids, channel_ids)
    print("Messages")
    post_messages(api, me["id"], channel_ids, role_ids)
    print("\nDone. You can kick the bot now (right-click it > Kick); its messages stay.")


if __name__ == "__main__":
    main()
