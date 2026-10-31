# Code signing with Azure Artifact Signing: optional setup for the owner

*Status: set up on 2026-10-31 with a Public Trust profile; releases are signed from 0.3.1.*

Signing is **optional** ([ADR-0018](../adr/0018-mod-safety.md) §4). Without it, a `v*` tag publishes the release
unsigned, with its `.sha256` and a GitHub build provenance attestation, and Windows SmartScreen warns on first run.
With it, the release job also signs `DAORework.exe` and `dao.exe`. If only some of the setup below is in place, the run
stops before publishing, so a half-finished setup is never mistaken for a signed release.

**Before you start:** an individual's certificate carries your legal name as Microsoft verified it, and may include
where you live. Anyone can read it in the file's Properties > Digital Signatures. If you do not want that on every
download, leave signing off; ways to sign under another name (a business of your own, or the Microsoft Store) need
their own decision first.

Nothing here goes into the repository as a secret: GitHub proves to Azure which workflow is asking (OpenID Connect),
and Azure only signs for the GitHub environment named `release`.

## What it costs and what it shows

- Azure Artifact Signing, Basic tier: about **US$9.99 a month**, up to 5,000 signatures (a release uses two). Identity
  validation for an individual is available in the United States and Canada.
- Signed executables show **your legal name as the publisher** (as validated by Microsoft) instead of "Unknown
  publisher", in SmartScreen, in the UAC prompt and in the file's Properties > Digital Signatures.
- Signing is available to an individual in the United States and Canada. SignPath Foundation's free signing is not an
  option: it requires an OSI-approved license, and DAORework's ([ADR-0020](../adr/0020-license-noncommercial.md)) is
  not one.
- Signing does not make SmartScreen silent on day one. SmartScreen also weighs reputation, which a signed publisher
  builds up as people download and run the files without problems. The certificates Azure issues last a few days and
  each signature is timestamped, so signed releases stay valid after the certificate expires; there is no yearly
  renewal.

## 1. Azure: the signing account

1. Sign in to the [Azure portal](https://portal.azure.com) and have a subscription (pay-as-you-go is enough).
2. Subscriptions > your subscription > Resource providers: register **Microsoft.CodeSigning**.
3. Create an **Artifact Signing account** (search "Artifact Signing"): a new resource group (for example
   `daorework-signing`), a name (for example `daorework`), a region, and the Basic tier. Note the account's
   **endpoint** on its Overview page; it matches the region, for example `https://eus.codesigning.azure.net/` for
   East US.

## 2. Azure: prove who you are

1. On the signing account, Access control (IAM) > Add role assignment: give **yourself** the role
   **Artifact Signing Identity Verifier**.
2. Identity validation > New identity > **Public** > **Individual**. Fill in your legal name and address exactly as on
   your ID, then complete the verification Microsoft sends you (a government ID and a selfie). It can take from hours
   to a few days. The status must read **Completed** before the next step.

## 3. Azure: the certificate profile

Certificate profiles > Create > **Public Trust**, named for example `daorework`, using the identity validation from
step 2. The name you choose is the `ARTIFACT_SIGNING_PROFILE` value below.

## 4. Azure: let the release workflow sign

1. Microsoft Entra ID > App registrations > New registration, named for example `daorework-release-signing`
   (single tenant, no redirect URI). From its Overview, note the **Application (client) ID** and the **Directory
   (tenant) ID**.
2. In that app: Certificates & secrets > **Federated credentials** > Add credential > *GitHub Actions deploying Azure
   resources*: organization `arastella625`, repository `DAORework`, entity type **Environment**, environment name
   `release`. This is what lets the workflow sign without a password: Azure accepts only runs in that environment.
3. On the certificate profile from step 3 (or the whole signing account), Access control (IAM) > Add role assignment:
   give the app the role **Artifact Signing Certificate Profile Signer**.

## 5. GitHub: the `release` environment

In the repository: Settings > Environments > New environment, named exactly `release`.

- **Deployment branches and tags:** *Selected branches and tags*, add the tag rule `v*` and the branch `main` (the
  branch is for re-running a release by hand from Actions > Release > Run workflow).
- **Required reviewers:** add yourself, so every signing waits for your click. On a private repository this needs a
  paid GitHub plan; once the repository is public it is free.
- **Environment variables** (variables, not secrets; none of these is a password):

| Name | Value |
|---|---|
| `AZURE_CLIENT_ID` | the app's Application (client) ID |
| `AZURE_TENANT_ID` | the Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | your subscription's ID |
| `ARTIFACT_SIGNING_ENDPOINT` | the endpoint from step 1, for example `https://eus.codesigning.azure.net/` |
| `ARTIFACT_SIGNING_ACCOUNT` | the signing account's name |
| `ARTIFACT_SIGNING_PROFILE` | the certificate profile's name |

## 6. Releasing

Push the tag as before (`git tag -a vX.Y.Z -m "DAORework X.Y.Z"`, `git push origin vX.Y.Z`). The workflow builds and smoke-tests the
executables, then the **Sign, attest and publish the release** job signs `DAORework.exe` and `dao.exe`, checks that Windows
reports both signatures as valid, runs the signed `dao.exe`, zips them with the `.sha256`, and publishes the release.
If signing fails, nothing is published; fix the setup and re-run with Actions > Release > Run workflow, with the tag.
To turn signing off again, delete the six variables.

To check a download yourself: right-click `DAORework.exe` > Properties > Digital Signatures, or in PowerShell
`Get-AuthenticodeSignature .\DAORework.exe`.
