# GLYPHLUME · Weiterentwicklung

Stand: 21.09.2026. Diese Anleitung wurde gegen lokale Quellen und Build-Scripts
abgeglichen. Sie ist kein neuer Nachweis einer Installation auf einem fremden PC.
Produktpruefungen und visuelle Abnahme stehen in den unten verlinkten Belegen.

## 1. Einstieg

App-Hauptordner relativ zu dieser Datei: `.`. Git-Repository: dieser
Ordner. Quellen relativ zu dieser Datei: `.`. Zuerst
[AGENTS.md](<AGENTS.md>) und [PROJECT_MAP.json](PROJECT_MAP.json) lesen. Bestehende
Pfade, Daten und uncommitted Aenderungen erhalten. **Alle folgenden Befehle
aus dem Quellordner `.` ausfuehren**, nicht aus einem beliebigen cwd.

## 2. Voraussetzungen

Windows und .NET Framework 4.x samt csc.exe; kein WebView2 erforderlich. assets/Glyphlume.png muss neben der Kandidaten-EXE im assets-Ordner liegen. Codex-Anmeldung ist nur fuer die optionalen KI-Funktionen erforderlich.

## 3. Bauen und starten

Fuer Kandidaten einen neuen Ausgabeordner verwenden; nicht ueber die laufende
oder ausgelieferte EXE bauen. Kein Build-Befehl hier startet einen Upload.

```powershell
$devOut = Join-Path ([IO.Path]::GetTempPath()) ("jano-glyphlume-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $devOut | Out-Null
$candidate = Join-Path $devOut 'candidate.exe'
& .\build.ps1 -OutputPath $candidate
Copy-Item -LiteralPath .\assets -Destination (Join-Path $devOut "assets") -Recurse
```

Startweg des bestehenden Lieferstands: [GLYPHLUME.exe](<GLYPHLUME.exe>).
Den Kandidaten erst nach den passenden Tests uebernehmen. Paketorte und
Liefer-Scripts sind in PROJECT_MAP.json und der bisherigen README benannt.

## 4. Aufbau und gemeinsame Module

IconStudio.cs: Bedienung und Icon-Verarbeitung; Libraries.cs: Bibliotheken; Styles.cs und WindowChrome.cs: native Gestaltung/Fenster; Localization.cs: Sprache; Authentication.cs: optionale Agent-Anbindung.

Modulstand und Migrationsgrenzen: [app-kit.plan.json](<app-kit.plan.json>).
Second Brain bleibt die Referenz fuer die Abstimmung gemeinsamer Bausteine.
Eine Modulvorbereitung bedeutet keine aktive Uebernahme oder Designfreigabe.

## 5. Pruefen

```powershell
# Nach dem Build: Kandidaten-EXE aus dem neuen Ausgabeordner
& $candidate --self-test
```

--self-test prueft isolierte lokale Dateien, keine Bildgenerierung. --generation-test ist ein gesonderter Online-Auftrag.

Erwartung: Die genannten Tests laufen ohne Fehler/mit Exitcode 0 durch.
Fehlende Laufzeiten, Browser oder Fixtures als fehlende Voraussetzung melden,
nicht als bestandenen Test. GUI-/Host-/Netztests bleiben gesonderte Schritte.
Testausgaben duerfen nur synthetische Inhalte enthalten. Nach einer UI-Aenderung
die tatsaechliche Kandidaten-App inklusive Fensterbedienung pruefen.

## 6. Daten und Konfiguration

data/ und Icons/ liegen neben der betriebenen App. Sie enthalten persoenliche Bibliotheken, Ergebnisse und angewendete Icons und bleiben aus Git ausgeschlossen. Fuer Tests die frisch gebaute Kandidaten-EXE im neuen Ausgabeordner verwenden, niemals private data/ mitkopieren.

Echte Zugangsdaten nicht in .env-Beispiele, Logs, Screenshots oder Git aufnehmen.
Es gibt durch diese Dokumentationspflege keine neue globale .env-Konfiguration.
Bestehende Profile vor einer beauftragten Migration sichern; keine Migration
allein zum Einrichten des Entwicklungsplatzes ausfuehren.

## 7. Stand, offene Punkte und Zusammenarbeit

Native WinForms-App mit eigener Look-Logik. Ein gemeinsamer nativer Look-Adapter muss noch entwickelt werden; das WebView2-Looks-Modul passt nicht direkt. Der manuelle Import-/ICO-/Bibliotheksweg bleibt ohne KI nutzbar.

Massgebliche bestehende Quellen (keine zweite Statuschronik):

- [README.md](<README.md>)
- [00_START_HERE.md](<00_START_HERE.md>)
- [REPOSITORY-NOTES.md](<REPOSITORY-NOTES.md>)
- [docs/DELIVERY-2026-09-21.md](<docs/DELIVERY-2026-09-21.md>)

Keine Projektlizenz am Repository-Einstieg gefunden. Diese Anleitung vergibt keine Nutzungsrechte; Lizenzentscheidung vor externer Weitergabe mit Jona klaeren. Bestehende Drittanbieterhinweise gelten weiterhin.

Arbeitsablauf fuer kleine Aenderungen, Nachweise und Uebergaben:
[CONTRIBUTING_APPS.md](<../jano-app-kit/CONTRIBUTING_APPS.md>).
Fuer neue Entwickler zuerst die risikoarmen Checks ausfuehren, dann eine kleine
Aenderung im eigenen Arbeitsstand. Abschluss mit geaenderten Dateien,
ausgefuehrten Tests, offen gebliebenen Pruefungen und genauem Kandidatenpfad.
