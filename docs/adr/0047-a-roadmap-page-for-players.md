# ADR-0047: A roadmap page for players on the website

- **Status:** Accepted (by the owner, 2026-11-11)
- **Date:** 2026-11-11

## Context
On 2026-11-11 the owner set out the road to 1.0 ([ROADMAP.md](../ROADMAP.md#the-road-to-10)) and asked for a design
players can see. The roadmap itself is written for whoever builds DAORework: task IDs, exit gates, ADR links. Players
ask a different question: what's coming, and when will it do the thing I need. [ADR-0021](0021-project-website.md)
limits the website to one page.

## Decision
- The website gets a **second page**, `site/public/roadmap.html`, served at `/roadmap`. It follows ADR-0021's rules:
  plain HTML and CSS, no scripts, no cookies, nothing from other sites, the same stylesheet and Content Security
  Policy.
- It says, in players' words ([VOICE.md](../VOICE.md)): what 1.0 will mean, each release on the road with what it
  brings a player, what comes after 1.0, and how to help (testing on a GOG or EA App copy first).
- **No dates and no task IDs.** Each release reads *Out now*, *Next*, *Planned*, *The goal* or *Later*. The page says
  plans can change and that it changes with each release.
- **ROADMAP.md stays the source.** A PR that changes *The road to 1.0* changes the page in the same PR, and each release
  moves its card to *Out now*. A plan that is only proposed (an ADR not yet accepted) is worded as *where it's safe* or
  *if*, never as a promise.
- The main page and the Nexus description link to it.

## Consequences
- Players can see where DAORework is going without reading the developers' roadmap, and the page asks for the testers
  1.0 needs (P2.9-06).
- The page is one more thing to update at each release; [website.md](../release/website.md)'s release step says so.
- ADR-0021's *one page* becomes two. Everything else in ADR-0021 stands.

## Alternatives considered
- *A section on the main page:* the road needs room, and the main page is about what DAORework does today.
- *Linking to ROADMAP.md on GitHub:* full of task IDs and exit gates, written for the people building it.
- *A section in the Nexus description:* it would be pushed with every wording change and repeat the website; a link is
  enough.
- *GitHub Projects or milestones:* track tasks, not what a player gets, and need a GitHub visit.
