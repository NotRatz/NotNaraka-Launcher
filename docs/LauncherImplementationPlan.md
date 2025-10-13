# NarakaTweaks Launcher Implementation Plan

This plan expands on the earlier high-level roadmap and breaks the work into
trackable workstreams that culminate in a full-featured NarakaTweaks launcher
executable. The launcher must mirror the community PSO2 launcher experience
while incorporating the existing automation in `RatzTweaks.ps1` and the current
WinUI detector.

## 1. Foundation & Bootstrap

1. **Project setup**
   - Create a dedicated WinUI 3 (or WPF, if WinUI blockers persist) launcher
     project inside the solution alongside the existing detector project.
   - Share common assets (icons, background, localized strings) via a
     `Launcher.Shared` library to avoid duplication with the detector UI.

2. **Bootstrapper service**
   - Port the self-update, elevation, and asset validation logic from
     `RatzTweaks.ps1` into a C# bootstrap service that runs before any UI opens.
   - Maintain the existing download safeguards (hash validation, retry limits,
     CDN fallback) and produce actionable error surfaces for users when assets
     fail to fetch.
   - Persist launcher configuration (install paths, chosen client, tweak
     presets) under `%ProgramData%/NarakaTweaks` with appropriate ACLs.

3. **Updater packaging**
   - Extend `build.ps1`/`publish.ps1` to produce a self-contained launcher
     installer bundle (MSIX or Squirrel) that includes the bootstrapper payloads
     and utility binaries currently shipped with the PowerShell script.

## 2. Core Tweak Execution Engine

1. **Engine abstraction**
   - Translate the tweak routines in `RatzTweaks.ps1` into C# services that
     expose idempotent operations (apply, validate, revert) using the .NET
     registry and file APIs.
   - Encode tweak metadata (category, description, risk level, dependencies) in
     a declarative format so the UI can render toggles dynamically and maintain
     parity with the script.

2. **Sequencing & telemetry**
   - Preserve the script’s gating flow (Discord OAuth, strike system, lockout
     timers) by reusing the existing webhooks and registry caches.
   - Emit structured logs for each tweak step to support debugging and
     centralized telemetry.

3. **Rollbacks & backups**
   - Implement automatic backups for any touched files/registry keys and expose
     a rollback surface in the UI for recovering from failed tweaks.

## 3. QualitySettings Editor

1. **Data model**
   - Parse `QualitySettings.txt` into a strongly-typed model. Leverage the
     existing `QualitySettingsData.txt` for defaults/reference values.
   - Implement diff-aware serialization so manual edits by users are preserved
     when the launcher saves changes.

2. **UI/UX**
   - Build a dedicated page mirroring the PSO2 launcher layout with grouped
     settings (graphics, performance, controls, accessibility) and contextual
     help tooltips.
   - Provide preset profiles (Performance, Balanced, Quality) that map to
     pre-defined value sets for quick application.

3. **Validation & sync**
   - Add validation rules (type bounds, compatible options) and surface inline
     feedback before saving.
   - Support importing/exporting configuration snippets to facilitate community
     sharing.

## 4. Boot Configuration Optimization

1. **Boot analyzer**
   - Analyze the existing `boot.config` to identify toggles that improve load
     times and stability.
   - Surface recommendations in the launcher with one-click apply/revert
     options.

2. **Monitoring**
   - After applying boot tweaks, monitor launch times and stability signals via
     optional telemetry (opt-in) to validate effectiveness.

## 5. Client Distribution Management

1. **Client catalog**
   - Build metadata describing each supported client (Official Global, Chinese,
     Steam, etc.) including download URLs, install steps, and patching
     requirements.

2. **Download manager**
   - Integrate a resumable downloader with checksum verification and throttling
     controls.
   - Allow users to select the target client, pick install directory, and
     trigger post-download patching scripts.

3. **Switching & detection**
   - Detect existing installations and allow switching between clients by
     updating launcher paths and tweak contexts.

## 6. Cheat Detection & Notification

1. **Service integration**
   - Reuse or refactor the logic in `CheatDetectorGUI.cs` to run as a background
     service within the launcher, exposing status to the UI.

2. **Notification channels**
   - Provide in-app alerts, system tray notifications, and Discord webhook
     updates when suspicious activity is detected.
   - Ensure the detection service can run headless when the launcher minimizes
     to tray during gameplay.

3. **Reporting workflow**
   - Allow users to submit logs/screenshots directly to moderation channels from
     the launcher, bundling relevant diagnostics securely.

## 7. Launcher UI & Navigation

1. **Navigation shell**
   - Model the navigation flow after the PSO2 community launcher:
     `Start → Authentication → Core Tweaks → GPU Tweaks → Optional Tweaks →
     Settings Editor → Client Downloads → About/Finish`.
   - Preserve progress indicators, animated transitions, and theming consistent
     with `index.html`.

2. **State management**
   - Centralize state (selected tweaks, client choice, detection status) in a
     shared view-model so steps remain synchronized.

3. **Accessibility & localization**
   - Audit controls for keyboard navigation, screen reader support, and color
     contrast; prepare for localization by externalizing strings.

## 8. Testing & Release

1. **Automated tests**
   - Add unit/integration tests for tweak engines, settings parser, and download
     manager. Use mocked registry/file systems where appropriate.

2. **Manual validation**
   - Draft QA matrices covering client download permutations, tweak application
     success/failure, cheat detection edge cases, and OAuth lockouts.

3. **Release checklist**
   - Document the packaging pipeline, update the README with install/usage
     instructions, and prepare release notes aligned with community expectations.

---

This document should guide task assignments and sprint planning as we progress
from the legacy PowerShell tooling to a polished, maintainable launcher
experience.
