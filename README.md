# GLYPHLUME

A small Windows desktop app for creating, collecting and exporting icons.

![App preview](docs/app.png)

## Features
- Prompt-based icon generation through the installed Codex CLI and an existing ChatGPT/Codex subscription.
- Real Windows ICO export with 16, 24, 32, 48, 64, 128 and 256 px entries; choosing a maximum includes smaller offered sizes.
- PNG/JPG/BMP/ICO import with a centered square crop and ICO export without signing in.
- Named icon libraries, multiple selection and batch copying to new or existing libraries, portable set ZIP export/import, and existing images as AI references.
- Explicit shortcut selection before applying an icon.
- Customizable colors, fonts, logo and layout details; German and English UI.

## Build and run
Windows with .NET Framework 4.x and the included Framework C# compiler is required.

```powershell
.\build.ps1
.\GLYPHLUME.exe
```

Keep `assets/Glyphlume.png` beside the app in the `assets` folder. Settings and generated work are saved locally in `data`; applied icons are stored in `Icons`. The app folder must be writable.

For AI generation, install Codex with its CLI and sign in with an account that supports image generation. The app does not bundle Codex or credentials. Generation requires internet and uses the account's available quota. Other AI providers are not integrated.

## Local checks

```powershell
.\GLYPHLUME.exe --self-test
```

This validates ICO structure, library ZIP roundtrips, reference state, localization and look settings. It creates isolated fixtures in `test-output` and does not submit a live image generation request. `--generation-test` is a separate online operation and is not part of ordinary validation.

## Data boundaries
This repository contains reviewed source and product assets. Personal generations, prompts, libraries, settings, login data, diagnostics, backups and compiled development binaries are excluded. No Open Source license is granted with this private repository.

Created by [Jona Fynn Schlegelmilch](https://www.linkedin.com/in/jonaschlegelmilch/).

Name screening and its limits: [BRAND-CHECK.md](BRAND-CHECK.md).
