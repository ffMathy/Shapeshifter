# Publishing Shapeshifter to the Microsoft Store and the Mac App Store

Every release that CI publishes on GitHub (not prereleases) is also sent to both stores by
[`.github/workflows/store.yml`](../../.github/workflows/store.yml), which `ci.yml` calls after its `release` job. You can
also run **Actions › Store release › Run workflow** with a released tag such as `v7.1.3` to send (or resend) that
version.

Each store's job is skipped until its repository variables are set (`MSSTORE_PRODUCT_ID` for the Microsoft Store,
`APPLE_TEAM_ID` for the Mac App Store), so merging this setup changes nothing until you finish the steps below.

| | Microsoft Store | Mac App Store |
|---|---|---|
| Account | [Partner Center](https://partner.microsoft.com/dashboard/registration), free for individuals and companies | [Apple Developer Program](https://developer.apple.com/programs/enroll/), 99 USD a year |
| What CI builds | `.msixupload` with an x64 and an arm64 MSIX (unsigned; the Store signs it) | `.pkg` of a sandboxed, Apple-silicon-only `.app`, signed for the App Store |
| How CI submits | `msstore publish` ([Microsoft Store Developer CLI](https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/overview)) | `fastlane deliver`, with an App Store Connect API key |
| After submitting | Certification (hours to a few days), then the Store publishes it | App Review (usually a day or two), then Apple releases it |

Store copies never install or update themselves from GitHub (`StoreInstall` detects an MSIX package identity on Windows
and the App Sandbox on macOS); the store updates them.

## Before the first Mac App Store submission

The Mac App Store requires the App Sandbox, and two parts of Shapeshifter's macOS build have not been tried in it:

1. **The paste gesture.** Shapeshifter watches the keyboard system-wide (SharpHook's event tap), swallows ⌘V while the
   picker is open and then posts the paste. Apple's DTS
   [says](https://developer.apple.com/forums/thread/820594) that posting events (the `PostEvent` privilege) and
   listening to them (`ListenEvent`, "Input Monitoring") work in the sandbox, but APIs gated by the full Accessibility
   privilege do not. Whether a tap that *swallows* keys works sandboxed has to be tested on a Mac, and App Review has
   rejected clipboard managers that post ⌘V under guideline 2.4.5 (same thread). Upload a build with
   `STORE_DRAFT_ONLY=true`, try it through TestFlight, and explain the gesture in the App Review notes.
2. **Start at login.** A sandboxed app cannot write `~/Library/LaunchAgents`, so store copies disable that setting.
   `SMAppService.mainApp` (macOS 13 and later) is the sandbox-friendly replacement.

The Windows build needs no such changes: full-trust MSIX packages keep the keyboard hook and paste simulation, and the
package's startup task starts Shapeshifter at login.

## Microsoft Store

### 1. Reserve the app

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard) and open **Apps and games › New product ›
   MSIX or PWA app**. Reserve the name **Shapeshifter**.
2. Open the app's **Product management › Product identity** and note:
   - **Store ID** (12 characters, starts with `9`): repository variable `MSSTORE_PRODUCT_ID`
   - **Package/Identity/Name**: `MSSTORE_IDENTITY_NAME`
   - **Package/Identity/Publisher** (`CN=…`): `MSSTORE_PUBLISHER`
   - **Package/Properties/PublisherDisplayName**: `MSSTORE_PUBLISHER_DISPLAY_NAME`

### 2. Give CI access to Partner Center

1. In Partner Center open **Account settings › User management › Microsoft Entra applications** and add (or create) an
   application with the **Manager** role. Its **Tenant ID** and **Client ID** go into the secrets `MSSTORE_TENANT_ID` and
   `MSSTORE_CLIENT_ID`.
2. On that application, **Add new key** and store the key in `MSSTORE_CLIENT_SECRET`. Keys expire after one or two years;
   renew the secret when they do.
3. Your **Seller ID** is under **Account settings › Legal info › Developer** (or the account's profile): `MSSTORE_SELLER_ID`.

### 3. The first submission (by hand)

The Store API can only update an app whose first submission exists, so make that one in Partner Center:

1. Set the repository variables and secrets from the steps above, and the variable `STORE_DRAFT_ONLY` to `true`.
2. Run **Actions › Store release** with the latest release tag. It uploads the package and leaves the submission as a
   draft. (Or download the `microsoft-store` artifact and upload its `.msixupload` under **Packages** yourself.)
3. Finish the draft in Partner Center: **Pricing and availability**, **Properties** (category, privacy policy URL),
   **Age ratings** and the **Store listing** (description, at least one screenshot, 1366×768 or larger).
   Under **Submission options**, explain the `runFullTrust` restricted capability: "Shapeshifter is a desktop app (packaged
   Win32/.NET) and needs full trust to watch the clipboard, recognise its paste gesture with a low-level keyboard hook and paste into other apps."
4. Submit it. When it is published, delete `STORE_DRAFT_ONLY` (or set it to `false`): from then on every release is
   submitted and published without you.

## Mac App Store

### 1. Identifiers, certificates and the provisioning profile

All in [Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources):

1. **Identifiers › +** › App IDs › App, platform macOS, explicit bundle ID **`app.shapeshifter.Shapeshifter`** (the one in
   [`macos/Info.plist`](macos/Info.plist)). No capabilities are needed.
2. **Certificates › +**: create an **Apple Distribution** certificate and a **Mac Installer Distribution** certificate
   (each from a certificate signing request made with Keychain Access › Certificate Assistant). Download both and open
   them so they land in your login keychain with their private keys.
3. In Keychain Access select both certificates (under **My Certificates**), **File › Export Items…** as one `.p12` with a
   password. Then:
   ```bash
   base64 -i Certificates.p12 | pbcopy   # secret MAS_CERTIFICATES_P12; the password goes into MAS_CERTIFICATES_PASSWORD
   ```
4. **Profiles › +** › Distribution › **Mac App Store Connect**, App ID `app.shapeshifter.Shapeshifter`, the Apple Distribution certificate.
   Download it, then:
   ```bash
   base64 -i Shapeshifter_Mac_App_Store.provisionprofile | pbcopy   # secret MAS_PROVISIONING_PROFILE
   ```
   Profiles expire after a year; renew this secret when it does.
5. Your **Team ID** (Membership details, 10 characters) goes into the variable `APPLE_TEAM_ID`.

The two certificates and the API key below are per team, so both Shapeshifter and Valafind use the same ones; the
provisioning profile is per app.

### 2. The app record and the API key

1. In [App Store Connect](https://appstoreconnect.apple.com/apps) choose **+ › New App**, platform macOS, bundle ID
   `app.shapeshifter.Shapeshifter`, any SKU.
2. **Users and Access › Integrations › App Store Connect API › Team Keys › +**, access **App Manager**. Note the **Key ID**
   (variable `APP_STORE_CONNECT_KEY_ID`) and the **Issuer ID** above the table (variable `APP_STORE_CONNECT_ISSUER_ID`),
   and download the `.p8` file (possible only once). Its contents, from `-----BEGIN PRIVATE KEY-----` to the end line,
   are the secret `APP_STORE_CONNECT_PRIVATE_KEY`.

### 3. The first submission

1. With `STORE_DRAFT_ONLY=true`, run **Actions › Store release**: it uploads the build and creates the version without
   submitting it.
2. In App Store Connect fill in everything the first version needs: description, keywords, support URL, privacy policy
   URL, **App Privacy** answers, **Pricing and Availability**, the age rating and Mac screenshots (1280×800, 1440×900,
   2560×1600 or 2880×1800). In **App Review Information**, explain what the app does and which permissions it asks for
   (Input Monitoring and Accessibility, to show the picker while ⌘V is held and paste the chosen item; uploads to catbox.moe happen only when the user picks that action).
3. Select the uploaded build and **Add for Review**. After the first approval, set `STORE_DRAFT_ONLY` to `false`: every
   later release is uploaded, given release notes that link to its GitHub release, submitted, and released when approved.

## All secrets and variables

Set them under **Settings › Secrets and variables › Actions** of this repository, or with the GitHub CLI:

```bash
gh secret set MSSTORE_CLIENT_SECRET --repo ffMathy/Shapeshifter
```

```bash
gh variable set MSSTORE_PRODUCT_ID --repo ffMathy/Shapeshifter --body 9NXXXXXXXXXX
```

| Name | Kind | Value |
|---|---|---|
| `MSSTORE_PRODUCT_ID` | variable | Store ID (`9N…`); turns the Microsoft Store job on |
| `MSSTORE_IDENTITY_NAME` | variable | Package/Identity/Name |
| `MSSTORE_PUBLISHER` | variable | Package/Identity/Publisher (`CN=…`) |
| `MSSTORE_PUBLISHER_DISPLAY_NAME` | variable | Package/Properties/PublisherDisplayName |
| `MSSTORE_TENANT_ID` | secret | Entra tenant ID |
| `MSSTORE_SELLER_ID` | secret | Partner Center seller ID |
| `MSSTORE_CLIENT_ID` | secret | Entra application (client) ID |
| `MSSTORE_CLIENT_SECRET` | secret | Entra application key |
| `APPLE_TEAM_ID` | variable | Apple team ID; turns the Mac App Store job on |
| `APP_STORE_CONNECT_KEY_ID` | variable | API key ID |
| `APP_STORE_CONNECT_ISSUER_ID` | variable | API issuer ID |
| `APP_STORE_CONNECT_PRIVATE_KEY` | secret | Contents of `AuthKey_<key id>.p8` |
| `MAS_CERTIFICATES_P12` | secret | Base64 of the `.p12` with both certificates and their keys |
| `MAS_CERTIFICATES_PASSWORD` | secret | That `.p12`'s password |
| `MAS_PROVISIONING_PROFILE` | secret | Base64 of the Mac App Store Connect profile |
| `STORE_DRAFT_ONLY` | variable | `true` to upload without submitting (first submissions) |

## Files

- [`windows/AppxManifest.xml`](windows/AppxManifest.xml): the MSIX manifest. Besides the app it declares a startup task
  (start at login; packaged apps cannot use the `Run` registry key), which users switch in **Settings › Apps › Startup**.
- [`windows/make-msix.ps1`](windows/make-msix.ps1): packs `dotnet publish` output into the `.msixupload`.
- [`macos/Info.plist`](macos/Info.plist), [`macos/Entitlements.plist`](macos/Entitlements.plist): the App Store bundle's
  property list and its sandbox entitlements.
- [`macos/make-pkg.sh`](macos/make-pkg.sh): builds, signs and packages the `.app`.
- `windows/Images`, `macos/AppIcon.png`: generated by [`make-images.py`](make-images.py) from the app icon.
