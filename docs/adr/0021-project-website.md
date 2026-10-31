# ADR-0021: A static project website on the owner's domain, served by Cloudflare

- **Status:** Accepted
- **Date:** 2026-10-31

## Context
Releases are not code-signed (ADR-0018 §4), so players need other ways to trust a download: the checksum and the
build attestation. A checksum is only worth checking against a copy published somewhere the download is not. The
Nexus page also needs its pictures hosted somewhere public while the repository is still private. The owner has a
domain on Cloudflare and asked to use it.

## Decision
- The project has a **static website** in `site/`: one page (what DAORework does, screenshots, download, how to check
  a download, how it is built, the license), a 404 page, `/.well-known/security.txt` and `robots.txt`.
- It is served by **Cloudflare Workers static assets** (Cloudflare's recommended way for new static sites), with no
  Worker script and no build step: `wrangler.jsonc` at the repository root points at `site/public`, so Cloudflare's
  default build settings deploy it from `main` on every push.
- The Nexus pictures live in `site/public/images` (their only copy; their captions are in
  `docs/release/nexus/captions.txt`). The Cinzel font and its license are copied into `site/public/fonts`, the one file
  kept twice, because the site cannot reach into the app's folder without a build step.
- The owner connects it to a subdomain of her domain in the Cloudflare dashboard
  ([docs/release/website.md](../release/website.md)); no Cloudflare credential is in the repository or in GitHub.
- **No scripts, no cookies, no trackers, nothing from other sites.** `_headers` sets a Content Security Policy that
  only allows the site's own images, styles and font.
- After each release, its SHA-256 goes on the page (a one-line docs change), so the checksum can be compared across
  two independent hosts.
- The pictures are the same files as the Nexus media, so the Nexus description can link to them on the site.
- The site is not part of the app: DAORework itself still never goes online (SDD §12).

## Consequences
- Players get a page on a stable address that is not a download mirror, with the checksum to compare.
- The checksum on the page is updated by hand after each release; CONTRIBUTING's release steps say so.
- Links from the site to GitHub only work once the repository is public.
- The site is plain HTML and CSS: no framework, no build step, nothing to keep up to date. If the app's heading font
  ever changes, the copy in `site/public/fonts` is changed with it.

## Alternatives considered
- *GitHub Pages:* needs a public repository (or a paid plan) and would sit on the same host as the downloads.
- *Cloudflare Pages:* still works, but Cloudflare now points new static sites to Workers static assets.
- *A site generator (Hugo, Astro):* tooling to maintain for a single page.
- *Hosting the pictures only (R2 bucket):* fixes Nexus but gives no independent place for the checksum.
