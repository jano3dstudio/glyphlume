# GLYPHLUME starten

**GLYPHLUME.exe in diesem Ordner doppelklicken.** GitHub Desktop ist zum Starten nicht nötig.

Lokaler Lieferstand: 21.09.2026. Quellcode und geprüfte Windows-App liegen zusammen in JS_GitHub/glyphlume.

- `GLYPHLUME.exe`: fertige Windows-App; benötigt den vorhandenen Ordner `assets`.
- `dist/GLYPHLUME-Windows.zip`: portables Paket ohne persönliche Daten. Komplett entpacken, dann die EXE starten.
- `data`: lokal übernommene Icons, Libraries und Einstellungen; wird von Git ignoriert.
- `Icons`: angewendete ICO-Dateien; wird von Git ignoriert.
- `build.ps1`: baut die EXE aus den vorhandenen Quellen erneut.
- `docs/DELIVERY-2026-09-21.md`: technischer Prüfstand.

Ab jetzt diesen Startpfad verwenden. Die bisherige Installation unter JS_GPT_Projects/local-icon-generator bleibt als Rückfallstand erhalten. Bestehende Windows-Verknüpfungen oder eigene Logo-Pfade können noch dorthin zeigen; den alten Ordner deshalb nicht löschen. Neue Änderungen werden zwischen den beiden lokalen Datenordnern nicht automatisch abgeglichen.

Der lokale Ordnername JS_GitHub bewirkt keinen Upload. EXE, ZIP und persönliche Daten bleiben durch .gitignore außerhalb der Quellcode-Versionierung. In dieser Lieferung wurde kein GitHub-Release veröffentlicht.