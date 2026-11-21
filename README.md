# LeckerBroot – Phase 1

Web-App (Blazor WebAssembly, C#) für Rezepte und Rezeptentwicklung. Läuft im Browser am PC
und später auf dem iPad als App auf dem Home-Bildschirm.

## Aufbau

```
LeckerBroot.slnx                 Projektmappe (in Visual Studio öffnen)
src/LeckerBroot.Core/            Rechenlogik – unabhängig von der Oberfläche
    Models.cs                    Zutat, Stufe, Rezept, Einstellungen
    Guidelines.cs                Richtwerte (Brotart, Versäuerung, TA, Salz) + deutsche Namen
    RecipeCalculator.cs          Kennzahlen: Gesamtmehl, TA, Versäuerung, Bäcker-%, Hinweise
    RecipeDesigner.cs            Rezept-Assistent Schritte 1–6 → Grammangaben je Stufe
    SourdoughGuide.cs            Führungsvorlagen, Geschmackstendenz mild↔kräftig, Reifezeit-Faustwert
    SeedData.cs                  Startzutaten + Beispielrezept
src/LeckerBroot.App/             Oberfläche
    Pages/                       Start, Rezepte, Rezeptansicht, Bearbeiten, Assistent, Zutaten, Sicherung
    Components/                  Stufen-Tabelle, Kennzahlen, Zutatenauswahl
    Services/AppStore.cs         Speichern im Browser (localStorage), Export/Import
    wwwroot/                     index.html, CSS, Icons, Offline-Unterstützung (Service Worker)
```

## Starten am PC

Voraussetzung: **Visual Studio 2026** (Workload „ASP.NET und Webentwicklung“) – bringt .NET 10 mit.
Alternativ das .NET 10 SDK und Visual Studio Code.

1. `LeckerBroot.slnx` öffnen.
2. `LeckerBroot.App` als Startprojekt festlegen.
3. F5 → der Browser öffnet `http://localhost:5080`.

Oder im Terminal:

```
cd src/LeckerBroot.App
dotnet run
```

Beim ersten Start legt die App die Grundzutaten und das Beispielrezept „Roggenmischbrot“ an.

## Wichtige Rechenregeln

- Alle Mengen intern als Gramm einer Referenzmenge; Bäcker-% = Gramm ÷ Gesamtmehl × 100.
- Teig gesamt = Anzahl × Stückgewicht ÷ (1 − Backverlust).
- Gesamtmehl = Teig ÷ (TA/100 + Σ Zusätze in % + Anstellgut-Anteil + Quellstück).
- **Versäuerung bezieht sich auf das Gesamtmehl.**
- Anstellgut zählt standardmäßig *nicht* zu Gesamtmehl/TA (umschaltbar unter „Zutaten“).
- Wasser im Quellstück zählt nicht zur TA, Wasser im Brühstück schon.
- Zweiter Sauerteig (optional, z. B. Weizensauerteig): eigene Versäuerung in % vom Gesamtmehl; die Gesamtversäuerung ist die Summe beider.
- Mehrstufige Sauerteige: Anteile beziehen sich auf das gesamte Sauerteigmehl, das Anstellgut kommt in die erste Stufe.
- Geschmacksprofil auf zwei Achsen: Säuremenge = Σ Versäuerung × Getreidefaktor (Roggen 1, Dinkel 0,7, Weizen 0,6) × Mehltyp-Faktor (Typ 405 = 0,8 … Vollkorn 1,2) × Reife; Charakter (mild ↔ herb) aus Temperatur, TA, Anstellgut, Zeit, gewichtet nach Säureanteil. Schätzwerte.
- Reifezeit-Faustwert (nur 18–30 °C): Stunden bei 25 °C aus der Anstellgut-Menge, je 10 °C wärmer etwa halbe Zeit.

## Phase 2a: Planung

- Assistent Schritte 7–9: Temperaturen (Schüttwasser), Ablauf, Backprofil – mit Richtwerten je Brotart.
- Rezeptansicht: Ablauf & Backen, Knopf „Backtag planen“. Ältere Rezepte: „Richtwerte übernehmen“.
- Backtag planen (/planen/…): rückwärts von „Brot aus dem Ofen um“; mehrstufige Sauerteige nacheinander,
  andere Vorstufen parallel; Ofen-Einschaltzeit; Kalenderdatei (.ics) mit Erinnerung je Schritt.

## Fertige Rezepte und Kastenformen

- Rezepte sind „in Entwicklung“ (frei bearbeitbar) oder „fertig“ (nur Menge). Umschalten in der Rezeptansicht;
  fertige Rezepte: „Kopie weiterentwickeln“ oder „Zurück in Entwicklung“.
- Kastenformen unter „Formen“: Innenmaße oder ausgelitertes Volumen. Menge = g Teig je Liter × Volumen.
  Richtwert je Brotart (Weizen 400 … Roggen 600 g/l); „passt genau – merken“ stellt den Wert pro Rezept ein.

## Daten

Alles liegt im Browser des jeweiligen Geräts. Unter **Sicherung** gibt es eine JSON-Datei
zum Aufbewahren und zum Übertragen PC → iPad.

## Auf das iPad (GitHub Pages)

1. Auf github.com ein neues Repository anlegen, z. B. `leckerbroot` (öffentlich – GitHub Pages ist für private
   Repositories nur in bezahlten Konten verfügbar).
2. „uploading an existing file“ → den **Inhalt** dieses Ordners hineinziehen (inkl. Ordner `.github`) → Commit.
3. Settings → Pages → Source: **GitHub Actions**.
4. Unter „Actions“ läuft „Auf GitHub Pages veröffentlichen“ (ca. 2–4 min). Danach ist die App erreichbar unter
   `https://<dein-name>.github.io/leckerbroot/`.
5. Auf dem iPad in Safari öffnen → Teilen → „Zum Home-Bildschirm“.

Bei jedem neuen Upload baut GitHub die App automatisch neu. Daten bleiben auf dem iPad erhalten.
Rezepte vom PC: dort unter „Sicherung“ exportieren, auf dem iPad importieren.
