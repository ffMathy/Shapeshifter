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
| How CI submits | `msstore publish` ([Microsoft Store Developer CLI](https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/overview)) once a Microsoft Entra application is set up (step 2); until then, CI builds the package and you upload it | `fastlane deliver`, with an App Store Connect API key |
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

The Store API signs in as a Microsoft Entra application that has the **Manager** role in your Partner Center account. The
**Microsoft Entra applications** tab where you add one only appears once a Microsoft Entra tenant is associated with the
account. Microsoft documents this for company accounts and says nothing about individual ones, but individual accounts
are doing it: publishers with free individual accounts have associated a tenant and submit from CI with `msstore`
(November 2026). The links below are the pages the `msstore` CLI itself opens.

1. **Associate a tenant.** Open [Account settings › Tenants](https://partner.microsoft.com/dashboard/account/TenantSetup)
   and either choose **Associate Microsoft Entra ID with your Partner Center account** and sign in with a work account of
   an existing tenant (not your personal Microsoft account), or choose **Create Microsoft Entra ID** to make a new, free
   tenant. Microsoft's guides:
   [associate an existing tenant](https://learn.microsoft.com/windows/apps/publish/partner-center/associate-existing-azure-ad-tenant-with-partner-center-account),
   [create a new tenant](https://learn.microsoft.com/windows/apps/publish/partner-center/create-new-azure-ad-tenant).
   Don't invite your personal Microsoft account into the tenant as a guest.
2. **Open the applications tab.** Sign out of Partner Center and back in, then open
   [Account settings › User management › Microsoft Entra applications](https://partner.microsoft.com/dashboard/account/v3/usermanagement#apps).
   It needs a **Manager** who is also global administrator of the tenant. If only the **Users** tab shows, add the
   tenant's admin (for example `you@yourtenant.onmicrosoft.com`) under
   [Users](https://partner.microsoft.com/dashboard/account/v3/usermanagement) with the **Manager** role, sign in to
   Partner Center as that account and look again; the change can take a while to show.
3. **Create the application from Partner Center.** Choose **Add Microsoft Entra application › Create Microsoft Entra
   application**, give it a name and any reply URL, and select **Manager (Windows)** under *Roles applicable to developer
   programs* ([Microsoft's guide](https://learn.microsoft.com/windows/apps/publish/partner-center/manage-azure-ad-applications-in-partner-center)).
   Apps registered in the Entra admin center and added afterwards have failed with "Identity cannot be authorized"
   ([msstore-cli #131](https://github.com/microsoft/msstore-cli/issues/131)); creating it here avoids that.
4. **Make a key.** Select the application, then **Add new key**. The **Tenant ID**, **Client ID** and **Key** go into the
   secrets `MSSTORE_TENANT_ID`, `MSSTORE_CLIENT_ID` and `MSSTORE_CLIENT_SECRET`. Keys expire after one or two years;
   renew the secret when they do.
5. **Copy the Seller ID** from [Account settings › Legal info](https://partner.microsoft.com/dashboard/account/v3/organization/legalinfo#mpn)
   into `MSSTORE_SELLER_ID`.

If the tenant can't be associated, or the tab still doesn't appear, open a support request from
[Partner Center](https://partner.microsoft.com/dashboard) (**?** › Help and support): Microsoft has published no rule
either way ([one such report](https://github.com/MicrosoftDocs/windows-dev-docs/issues/6153)). Until it works, leave the
four `MSSTORE_…` secrets unset: every release then builds the package, keeps it for 90 days as the run's
**microsoft-store** artifact and writes upload instructions in the run's summary, for uploading by hand (step 3).

### 3. Submissions

The Store API can only update an app whose first submission exists, so make that one in Partner Center:

1. Set the repository variable `STORE_DRAFT_ONLY` to `true`, then run **Actions › Store release** with the latest release
   tag (or let the next release run it).
   - With the API secrets, it uploads the package and leaves the submission as a draft.
   - Without them, download the run's **microsoft-store** artifact, unzip it, and in Partner Center start the submission
     and drop the `.msixupload` under **Packages**.
2. Fill in **Pricing and availability**, **Properties** (category, privacy policy URL), **Age ratings** and the **Store
   listing** (description, at least one screenshot, 1366×768 or larger). Under **Submission options**, explain the
   `runFullTrust` restricted capability: "Shapeshifter is a desktop app (packaged Win32/.NET) and needs full trust to watch the clipboard, recognise its paste gesture with a low-level keyboard hook and paste into other apps."
3. **Submit to the Store**.
4. When it is published, delete `STORE_DRAFT_ONLY` (or set it to `false`). With the API secrets, every later release is
   submitted and published without you; without them, upload the releases you want from their **microsoft-store**
   artifacts the same way (you don't have to upload every one).

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
| `MSSTORE_TENANT_ID` | secret | Entra tenant ID (leave the four `MSSTORE_…` secrets unset to upload by hand) |
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
