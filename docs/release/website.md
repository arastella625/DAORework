# The project website: setup for the owner

Live at <https://daorework.dilly-world.net/> (set up 2026-10-31).

The website is in `site/` ([ADR-0021](../adr/0021-project-website.md)). Cloudflare builds and serves it from `main`;
this is the one-time setup in your Cloudflare account. It costs nothing on Cloudflare's free plan.

## 1. Create the site from the repository

1. In the [Cloudflare dashboard](https://dash.cloudflare.com), open **Workers & Pages** and choose **Create
   application**.
2. Choose **GitHub** (this makes a Worker, which is what Cloudflare now recommends; not the Pages tab). Sign in to
   GitHub when asked, give Cloudflare access to `arastella625/DAORework` only, and pick it. This works while the
   repository is private.
3. Keep Cloudflare's default build settings: the `wrangler.jsonc` at the top of the repository tells it everything.

| Setting | Value |
|---|---|
| Project name | `daorework` (must match `name` in `wrangler.jsonc`) |
| Production branch | `main` |
| Root directory | empty (the repository root) |
| Build command | empty |
| Deploy command | `npx wrangler deploy` (the default) |

4. **Save and deploy.** After a minute the site is live at `daorework.<your account>.workers.dev`. Open it and check
   the pictures show. Builds of other branches and pull requests make preview versions and leave the live site alone.

## 2. Put it on your domain

In the new Worker: **Settings** > **Domains & Routes** > **Add** > **Custom domain**, and enter a subdomain of your
domain, for example `daorework.yourdomain.com`. Cloudflare adds the DNS record and the HTTPS certificate itself.

Optionally, turn off the `workers.dev` address on the same screen, so the site has one address.

## 3. Optional: a contact address that hides your own

**Email** > **Email Routing** on your domain: add an address such as `daorework@yourdomain.com` that forwards to your
own inbox. People see only the domain address. You can then add it to `site/public/.well-known/security.txt` as a
second `Contact: mailto:...` line.

## 4. Use it

- **Nexus pictures:** the Nexus description loads them from `https://daorework.dilly-world.net/images/`.
- **After each release:** put the zip's SHA-256 (from its `.sha256` file) and the version in the *Check your download*
  section of `site/public/index.html`, and merge. Add the Nexus page's address to the *Download* section once it
  exists.
- **Private vulnerability reports:** `security.txt` points to GitHub's private reporting. Once the repository is
  public, turn it on: **Settings** > **Code security** > **Private vulnerability reporting**.

By default every push to `main` rebuilds the site. To rebuild only when it changes, set **Settings** > **Builds** >
**Build watch paths** to include `site/*` and `wrangler.jsonc`.
