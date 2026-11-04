# ADR-0035: An opt-in check for new releases

- **Status:** Proposed. This would be DAORework's first network access, so it waits for the owner's decision.
- **Date:** 2026-11-04

## Context
A player finds out about a new DAORework release only by watching GitHub or Nexus (P3-06,
[#19](https://github.com/arastella625/DAORework/issues/19)). Everything DAORework has done so far is offline, and the
README, the website and the Nexus page say so. SDD §12 allows network access only with an ADR and an opt-in.

Some facts shape the answer:
- GitHub's public API answers an anonymous `GET /repos/{owner}/{repo}/releases/latest` with no account or key. An
  anonymous caller gets 60 requests an hour per address.
- Every release so far is marked a pre-release (`v0.3.0` to `v0.4.0`), and `/releases/latest` leaves those out. A check
  built on it would find nothing today.
- Nexus's API needs each player's own key, and its [acceptable use policy](https://help.nexusmods.com/article/114-api-acceptable-use-policy)
  bars using one without the player starting the action.
- A request tells the server the player's address and when they looked. No request is fully anonymous.

## Decision

### 1. Off until the player says yes
The check is off by default. Home asks once (*Tell you when a new DAORework is out?*, with *Turn on* and *No thanks*), and
Settings turns it on or off. Saying no means no request is ever made. It joins the question on Home for new downloads
([ADR-0034](0034-picking-up-new-downloads.md)) only after that one has been answered, so a first run asks one thing.

### 2. What is sent, where and how often
- **One request:** `GET https://api.github.com/repos/arastella625/DAORework/releases?per_page=5`, over `https` only, with
  redirects off. The list, since `/releases/latest` skips pre-releases.
- **Nothing about the player:** no account, key, profile, path, mod list or unique id. The `User-Agent` GitHub requires is
  `DAORework/<version>`, which says which version asks and nothing else.
- **At most once a day,** when the window opens. The time of the last check is kept in `preferences.json`. Nothing runs in
  the background, and nothing runs when DAORework is closed. The command line never checks by itself.
- **A short wait:** ten seconds, then it gives up. A failure says nothing on its own, and *Check now* in Settings says *Couldn't
  check*.

### 3. What is done with the answer
- Versions are compared in Core, as pure code with tests. Drafts are skipped. A pre-release counts while DAORework's own
  version is one.
- The response is treated as untrusted: only `tag_name` and `html_url` are read, the tag must parse as a version, and the
  link must be `https` on `github.com` under this repository, or the answer is ignored.
- Release notes from the response are never shown.

### 4. What the player sees
A quiet line on Home, *DAORework 0.5.0 is out*, with *Open the release*, through `OpenModPage`'s `https` rule
(P3-11), and a link to how to check the download (`docs/release/`). It is said once per version. Nothing is downloaded.

### 5. What never happens
- Nothing is downloaded or installed, and nothing is replaced. Installing an update needs its own ADR and threat model.
- No other network access rides on this. A new one needs its own ADR, as SDD §12 says.
- Logs give the outcome (*checked*, *failed*), never the response.

### 6. Where the code goes
- **Core:** `ReleaseVersion` and the comparison, pure.
- **Application:** a `CheckForUpdate` use case, and a port, `IReleaseFeed`.
- **Infrastructure:** `GitHubReleaseFeed`, the only class in the project that may use `HttpClient`. An architecture test
  holds that. No new package.
- **Words:** VOICE.md gains the two lines above. The README, the website and the Nexus description stop saying DAORework
  never goes online, in the same release, and say what the opt-in sends.

## Consequences
- A player who opts in hears about releases without watching anything.
- **"Never goes online" becomes "never goes online unless you turn this on".** The owner should weigh this: it is a claim
  players may have chosen DAORework for.
- GitHub sees the address of each player who opts in, once a day at most. DAORework itself collects nothing.
- `/security-review` applies to the task, since it is the first code to read an answer from the network.
- Marking releases as full releases later needs no change here.

## Alternatives considered
- **Nexus's API:** needs each player's key, for a question that needs no account.
- **A file on the project's website:** the same privacy, and it leaves the owner one more thing to update on each
  release, for no gain over GitHub.
- **Checking only when asked, with no opt-in question:** the most private, but a player who never presses it learns
  nothing, which is the problem at hand. *Check now* stays for that player.
- **Showing the release notes:** they would need rendering safely, and the link already reaches them.
- **Downloading the update for the player:** it needs the checksum, the attestation, and replacing running files. Its own
  ADR, if ever.
