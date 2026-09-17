# GLYPHLUME – Landingpage

Responsive statische Produktseite mit Deutsch/Englisch, echter App-Vorschau,
interaktiver ICO-Größenauswahl, FAQ, beiden Trailerformaten und Windows-ZIP.

## Branding-Status

GLYPHLUME wurde am 17.09.2026 nach Register-Vorprüfung als Produktname gewählt.
Umfang und Grenzen stehen in ../BRAND-CHECK.md. Logo, App-Screenshot, beide
Trailer und der portable Windows-Download verwenden diesen Namen.
Die Seite ist eine lokale Vorschau und wurde nicht veröffentlicht.

## Lokal starten

Dieses Verzeichnis mit einem statischen HTTP-Server öffnen, beispielsweise:

```powershell
python -m http.server 8767 --bind 127.0.0.1
```

Dann http://127.0.0.1:8767 öffnen. Kein Build, keine npm-Abhängigkeiten,
keine externen Schriftdateien, kein Tracking und kein Backend erforderlich.

## Dateien und Veröffentlichung

Alle Medien und Downloads werden relativ eingebunden. Zum späteren Hosting
den vollständigen Inhalt dieses Verzeichnisses übernehmen. Das Download-ZIP
enthält ausschließlich die portable App, keine privaten Libraries oder Zugangsdaten.
ZIPs sind im Repository standardmäßig ignoriert; die Veröffentlichung des
App-Downloads muss bei der endgültigen Paketierung explizit berücksichtigt werden.

Die Seite ist noch nicht online. Eine Domain wurde nicht registriert.
