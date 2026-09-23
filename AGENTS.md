<!-- workspace-navigation-20260921 -->
## Orientierung und bestehende Aufgaben

Vor der Weiterarbeit [PROJECT_MAP.json](PROJECT_MAP.json); die weiteren Regeln dieser Datei gelten weiterhin lesen.
Die Map nennt Quellen, Build, Git-Grenze, Pruefstand, Startweg und Modulplan.
Bestehende Quell-/Start-/Datenpfade wurden nicht verschoben. Historische Pfade
in frueheren Aufgaben bleiben auffindbar; diesen kanonischen Stand zuerst
abgleichen. Keine fremden Aenderungen verwerfen, keine pauschalen Commits oder
Uploads, keine alten Lieferkopien ueber neuere Quellen schreiben.
<!-- /workspace-navigation-20260921 -->

# Entwicklungsregeln

## Gemeinsame Modulbauweise vorbereiten · 21.09.2026

Vor Arbeiten an gemeinsamen UI-Bausteinen `app-kit.plan.json` lesen und
`./Check-AppKit.ps1` ausfuehren. Der Plan verweist auf die zentrale Kit-Quelle;
Ablauf dort in `ADOPTION.md`. Second Brain bleibt die Referenz zur Abstimmung.
Diese Vorbereitung aktiviert keine neuen Module. Bestehende aktive `kit.ref.json`
und ansonsten bisherige Pins gelten bis zur gezielten Migration weiter.
Fenster, Buttons, Schriftgroessen, Rundungen, Looks, Dialoggriffe und Arbeitsanzeige
zentral weiterentwickeln; Produktlogik, Nutzerdaten und Host-Regeln erhalten.
Ein erfolgreicher Vorbereitungscheck ist keine Build-, UI- oder Designfreigabe.

## Entwickler-Einstieg · 21.09.2026

[DEVELOPMENT.md](DEVELOPMENT.md) beschreibt Voraussetzungen, konkrete Build-/Testbefehle,
Datenablage, Modulgrenzen und offene Punkte. Vor Weiterarbeit zuerst dort lesen;
vorhandene Produktregeln und fachliche Nachweise bleiben massgeblich.
