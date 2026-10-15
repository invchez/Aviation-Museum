# App Store Connect — Manual Worksheets for AVIA Museum

fastlane's `deliver` cannot set the App Privacy labels or the Age Rating
questionnaire. Enter these by hand in App Store Connect using the answers
below, which were derived from an inspection of the actual code.

---

## 1. App Privacy ("privacy nutrition label")

Where: App Store Connect → Your App → **App Privacy** → Get Started.

### What the app actually does with data (code findings)

- **No login, no accounts, no user identity anywhere.**
- **No third-party SDKs**: no Firebase, no analytics, no ads, no crash
  reporting, no location, no camera, no microphone. The only plugins are
  UI/editor utilities (Joystick Pack, EditorAttributes) with no network code.
- **One network feature**: questions the user types to the AI museum guide are
  sent over HTTPS to a Cloudflare Worker
  (`spring-poetry-3bc5.alexli010972.workers.dev`), which returns an answer and
  a text-to-speech MP3. No device identifiers, names, or emails are attached.
  The audio file is cached locally and overwritten each time.
- Tutorial progress is stored on-device only (PlayerPrefs) — not "collected."

### Recommended answers

Question: "Do you or your third-party partners collect data from this app?"
→ **Yes** (the typed AI questions leave the device), then declare exactly one
data type:

| Data type | Collected? | Linked to identity? | Used for tracking? | Purpose |
|---|---|---|---|---|
| **User Content → Other User Content** (questions typed to the AI guide) | Yes | **No** | **No** | App Functionality |

Every other category (Contact Info, Location, Identifiers, Usage Data,
Diagnostics, etc.): **Not collected**.

> Stricter alternative: if the Cloudflare Worker/LLM provider does not retain
> the questions beyond serving the reply, Apple's "ephemeral processing"
> carve-out would allow **"Data Not Collected."** Choose this only if you can
> verify the worker's retention behavior; the declaration above is the safe
> default.

---

## 2. Age Rating questionnaire

Where: App Store Connect → Your App → App Information → **Age Rating** → Edit.

The app is a virtual museum: static aircraft exhibits, informational plaques,
an AI Q&A guide. No gameplay violence, no combat, nothing is destroyed or
shot. Answer every frequency question **None/No**:

| Question | Answer |
|---|---|
| Cartoon or Fantasy Violence | None |
| Realistic Violence | None |
| Prolonged Graphic or Sadistic Realistic Violence | None |
| Profanity or Crude Humor | None |
| Mature/Suggestive Themes | None |
| Horror/Fear Themes | None |
| Medical/Treatment Information | None |
| Alcohol, Tobacco, or Drug Use or References | None |
| Simulated Gambling | None |
| Sexual Content or Nudity | None |
| Graphic Sexual Content and Nudity | None |
| Unrestricted Web Access | No |
| Gambling and Contests | No |

Newer versions of the questionnaire also ask about **in-app AI/generative
content**: answer that the app **includes an AI chat feature limited to
aviation topics** (the guide is prompted per-aircraft). If the form asks
whether AI output is moderated/topic-restricted, answer Yes — the guide only
answers about the museum's aircraft. Expected resulting rating: **4+**
(a generative-AI question may raise it to 9+/12+ depending on the
questionnaire version — accept whatever it computes).

---

## 3. Pricing & Availability (one manual check)

`deliver` sets the price to **Free (tier 0)** automatically, but territory
availability is not settable via the API. New apps default to **all countries
and regions** — verify under Distribution → Pricing and Availability that all
territories are selected.

---

## 4. Privacy Policy URL

You said you'll enter this yourself: App Information → Privacy Policy URL.
(Deliberately not managed by fastlane in this setup.)
