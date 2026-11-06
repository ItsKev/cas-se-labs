# Lab 00: Aufgabe und Arbeitsumgebung

## Schwerpunkte

Die beiden Cloud-Module des CAS Software Engineering in the Age of AI setzen zwei Schwerpunkte: Infrastructure as Code und die Nutzung von AI. Dazu kommen die Grundlagen aus den Modultiteln.

- Modul 1 «Cloud Data Access & Processing», Sa 07.11.2026, online
- Modul 2 «Cloud-Computing, Serverless & Hyper Scaler Platforms», Fr 11.12.2026, Präsenz (I.S1A_202)

## Lernziele

Modul 1

- Cloud-Service-Modelle und Shared Responsibility einordnen
- den Standort der Daten begründet wählen und die dafür massgebenden Faktoren benennen
- für einen Anwendungsfall den passenden Speicher wählen (Objekt, Datei, relational, Dokumentdatenbank) und Tiers, Redundanz und Kosten abwägen
- Infrastruktur mit Terraform deklarieren, planen und anwenden
- eine Anwendung als Container betreiben, die auf Objektspeicher zugreift

Modul 2

- zwischen VM, Container und Serverless-Funktion begründet entscheiden und die Unterschiede in Skalierung, Kosten und Lock-in benennen
- eine ereignisgesteuerte Verarbeitung (Upload, Event, Funktion) aufbauen und robust machen: Retries, Idempotenz, Dead-Letter
- ein LLM per API einbinden, um aus Dokumenten strukturierte Metadaten zu gewinnen
- Änderungen an der Infrastruktur mit Terraform nachvollziehbar und wiederholbar ausrollen

## Anwendungsfall

Aus diesen Zielen folgt ein durchgehender Anwendungsfall: eine Plattform, die Dateien speichert und mit AI automatisch verarbeitet. Modul 1 setzt das Speichern und Abrufen um (Ausbaustufe 1), Modul 2 die automatische Verarbeitung (Ausbaustufe 2), für beide Szenarien dieses Labs, Rechnungen und Fotos.

Das Zielbild: Eine Rechnung wird hochgeladen. Kurz darauf stehen Lieferant, Beträge und MWST im System, und was das AI-Modell nicht sicher lesen konnte, ist markiert. Ein Foto wird hochgeladen und bekommt Beschreibung, Schlagwörter und Aufnahmeort. Die Verarbeitung startet automatisch, ohne manuellen Eingriff. Die Plattform dahinter ist als Code beschrieben und lässt sich löschen und identisch neu aufbauen. Offen ist, was dafür im Hintergrund laufen muss.

Lab 00 hat zwei Teile. Der erste beschreibt die Aufgabe, also die Architektur hinter diesem Ablauf. Dazu entsteht vor Modul 1 ein eigener Architektur-Entwurf. Der zweite richtet Repo, Entwicklungsumgebung und Azure-Anbindung ein, damit am Kurstag keine Installation nötig ist.

Eine Musterlösung gibt es nicht. Die Lösung wird in den beiden Modulen umgesetzt und mit den Entwürfen verglichen.

## Umfang

- Anforderungen der Plattform und zwei Szenarien
- Architektur-Entwurf mit Annahmen und offenen Fragen
- eigenes Repo und Entwicklungsumgebung (Codespace oder Dev Container)
- Test der Werkzeugkette
- Azure-Anmeldung

Das Ende des Labs enthält die Definition of Done. Die detaillierte Einrichtung der Entwicklungsumgebung steht im Anhang.

## Voraussetzungen

| Was | Hinweis |
|---|---|
| Persönliches GitHub-Konto | bereits eingerichtet |
| Azure-Abo | bereits eingerichtet, Rolle Owner, angelegt mit einem privaten Microsoft-Konto (nicht mit der HSLU-Adresse) |
| Gerät und Browser | Windows, macOS oder Linux mit aktuellem Browser. Für Variante A ist sonst nichts zu installieren, für Variante B nur VS Code mit der Erweiterung «GitHub Codespaces» |
| Papier oder Zeichenprogramm | für den Entwurf |

Das private Konto ergibt einen eigenen Mandanten (Tenant) mit der Rolle Owner. Ein Testabo bietet 200 USD Guthaben für 30 Tage. Zwischen Modul 1 und Modul 2 liegen 34 Tage, deshalb ist vor Modul 2 die Umstellung auf Pay-as-you-go nötig (Stand 01.11.2026, [Azure free account FAQ](https://azure.microsoft.com/pricing/free-trial-faq/)).

## Begriffe

- **Repo:** Git-Repository auf GitHub, der Ordner mit Code und Änderungshistorie.
- **Fork:** eigene Kopie eines Repos im eigenen GitHub-Konto, die mit dem Original verbunden bleibt. **Sync fork** holt spätere Änderungen des Originals in die Kopie.
- **Dev Container:** Entwicklungsumgebung als Container, beschrieben in `devcontainer.json` und damit für alle identisch.
- **Codespace:** Dev Container, der bei GitHub in der Cloud läuft, im Browser oder in VS Code.
- **Mandant (Tenant):** Verzeichnis in Azure für Konten und Rollen. Das Abo gehört zu einem Mandanten.
- **Terraform:** legt Cloud-Ressourcen anhand von Code-Dateien an und entfernt sie wieder (Infrastructure as Code). Einführung: [Terraform](https://developer.hashicorp.com/terraform/intro), [Terraform auf Azure](https://learn.microsoft.com/azure/developer/terraform/overview).
- **Deployment:** Anwendung oder Infrastruktur in der Cloud bereitstellen oder aktualisieren.
- **Code-along:** Schritte der Demo auf dem eigenen Rechner gleichzeitig nachvollziehen.
- **Demo-Modus:** Ein Lab ohne eigene Änderungen am Code durchgehen und der Demo folgen. Der Lösungsstand `solution/` ist dabei der Arbeitsordner. Beim selbstständigen Durcharbeiten (Übung) dient der Startstand `start/` als Ausgangspunkt.

---

## Die Aufgabe

### Ausgangslage

Die Aufgabe umfasst zwei Szenarien. Plattform und Ausbaustufe 1 sind identisch, das Ziel unterscheidet sich.

**Szenario A, Rechnungswesen**

- Ist-Zustand: Die Kreditorenbuchhaltung, zuständig für das Bezahlen von Lieferanten, erhält täglich Lieferantenrechnungen (PDFs). Lieferant, Rechnungsnummer, Datum, Beträge, MWST und Positionen werden von Hand abgetippt.
- Problem: Das kostet Zeit, und Tippfehler fallen erst spät auf.
- Ziel: Rechnungsverarbeitung. Die Felder werden automatisch ausgelesen, unsichere Felder gehen zur Kontrolle an eine Person.
- Offene Frage: Was geschieht mit Feldern, bei denen sich das Modell nicht sicher ist?

**Szenario B, Marketing**

- Ist-Zustand: Das Marketing sammelt Fotos von Anlässen, Produkten und Standorten. Sie liegen verstreut auf Laufwerken und in Chats, ohne einheitliche Benennung.
- Problem: Die Suche nach einem bestimmten Bild erfolgt durch Blättern. Aufnahmedatum und Ort sind oft nicht mehr bekannt, Schlagwörter fehlen.
- Ziel: Foto-Tagging. Jedes Foto erhält automatisch Beschreibung, Schlagwörter, Aufnahmedatum, Kamera und Standort.

Rahmen für beide Szenarien: Ein kleines Team verantwortet die Plattform. Der Betriebsaufwand soll minimal bleiben: möglichst wenig selbst installieren, aktualisieren und überwachen. Entscheidend ist die Time to Market, die Plattform steht in Tagen statt in Wochen. Der Standort der Datenhaltung ist offen und Teil der Aufgabe. Weitere Anforderungen stehen unter «Nicht-funktionale Anforderungen».

### Ausbaustufe 1: speichern und abrufen

Gilt für beide Szenarien.

- **F1** Mitarbeitende laden Dateien (PDF, Word, JPG, PNG) über eine Weboberfläche hoch, auch per Drag & Drop.
- **F2** Die Oberfläche listet alle Dateien mit Name, Grösse, Zeitpunkt des Hochladens und Status.
- **F3** Jede Datei ist im Original herunterladbar.
- **F4** Angenommen werden nur die genannten Dateitypen bis 10 MB. Alles andere wird mit verständlicher Fehlermeldung abgewiesen und nicht gespeichert.

### Ausbaustufe 2: automatisch verarbeiten mit AI

Das ist das Endziel. F5 bis F9 gelten für beide Szenarien.

- **F5** Jeder Upload löst automatisch eine Verarbeitung aus. Der Upload selbst wird dadurch nicht langsamer.
- **F6** Ein AI-Modell, das über eine Schnittstelle zur Verfügung steht, ordnet die Datei ein: Kategorie, Titel, kurze Zusammenfassung, Schlagwörter.
- **F7** Ergebnis und Status («hochgeladen», «fertig», «fehlgeschlagen») erscheinen in der Liste, ohne Änderung an der Oberfläche.
- **F8** Fehlgeschlagene Verarbeitungen gehen nicht verloren. Sie sind nachvollziehbar und wiederholbar.
- **F9** Änderungen an der Plattform sind als Code beschrieben und werden nachvollziehbar und wiederholbar ausgerollt (Deployment), nicht von Hand im Portal.
- **F10** Nur Szenario A: Aus der Rechnung werden Lieferant, Rechnungsnummer, Datum, Beträge, MWST und Positionen ausgelesen. Pro Feld ist die Sicherheit des Ergebnisses ausgewiesen, unsichere Felder sind markiert.
- **F11** Nur Szenario B: Aus dem Foto werden Aufnahmedatum, Kamera und Standort ausgelesen, dazu eine Beschreibung und erkannte Objekte. Der Standort ist auf einer Karte aufrufbar.

### Nicht-funktionale Anforderungen

- **N1** Time to Market ist wichtig: Die Plattform ist in Tagen verfügbar, nicht in Wochen.
- **N2** Die Plattform verursacht ohne Nutzung minimale Kosten.
- **N3** Die Plattform ist als Code beschrieben (Infrastructure as Code) und lässt sich jederzeit identisch neu aufbauen.
- **N4** Die Datenhaltung ist geklärt: Der Standort für Speicherung und Verarbeitung ist festgelegt und begründet. Welche Faktoren die Wahl bestimmen, ist Teil der Aufgabe.
- **N5** Zu jedem Fehler sind Zeitpunkt, betroffene Datei, Komponente und Fehlermeldung in den Logs abrufbar. Die Logs aller Komponenten sind an einer Stelle durchsuchbar.

### Rahmenbedingungen

- Technologie: Die Lösung im Kurs läuft auf Microsoft Azure. Der Entwurf darf jede Cloud oder on-premise verwenden, etwa AWS, Google Cloud oder eigene Server mit Open-Source-Komponenten. Der Vergleich im Unterricht ordnet die Bausteine der Anbieter einander zu. Eine Festlegung auf eine Technologie ist nicht nötig, allgemeine Begriffe wie Datenbank, Warteschlange oder Container sind zulässig.
- Anwendung: C# (.NET). Die Oberfläche ist eine einzelne Webseite mit etwas JavaScript.
- Nicht im Scope: Benutzeranmeldung, Berechtigungen für Endnutzer, ausgefeilte Sicherheitskonzepte und Hochverfügbarkeit über mehrere Regionen (Themen anderer Module).

Weiterführend zur Aufgabe:

- [Azure Well-Architected Framework](https://learn.microsoft.com/azure/well-architected/): Kosten, Betrieb, Zuverlässigkeit, Sicherheit, Leistung
- [Entscheidungshilfe Datenspeicher](https://learn.microsoft.com/azure/architecture/guide/technology-choices/data-store-overview): Speichertypen im Vergleich
- [Azure-Regionen](https://learn.microsoft.com/azure/reliability/regions-overview) und [Availability Zones](https://learn.microsoft.com/azure/reliability/availability-zones-overview): Standort der Daten
- [Shared Responsibility in der Cloud](https://learn.microsoft.com/azure/security/fundamentals/shared-responsibility): Aufteilung der Verantwortung
- [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/): Muster für Entwürfe

## Der Architektur-Entwurf

Zweck: Der Architektur-Entwurf zeigt, wie sich die Anforderungen F1 bis F11 und N1 bis N5 umsetzen lassen. Grundlage ist das eigene Wissen.

Inhalt: Der Entwurf beantwortet diese Fragen für die Plattform und zeigt, wo sich die beiden Szenarien unterscheiden.

- Wo läuft der Code, also die Anwendung und die Verarbeitung nach dem Upload?
- Wo liegen die Originaldateien, wo Metadaten und Status?
- Was löst die Verarbeitung aus, und wie kommt das AI-Modell ins Spiel? Welche Komponente ruft es auf, und womit weist sie sich aus?
- Wie kommt das System in Betrieb, und wie ist es beschrieben, damit es sich identisch neu aufbauen lässt?
- Was kostet die Plattform, wenn niemand sie benutzt?
- Wo liegen die Daten geografisch, und welche Faktoren haben zur Wahl dieses Standorts geführt?
- Was passiert, wenn die Verarbeitung fehlschlägt oder das Modell unsicher ist?

Format:

- eine Seite: fotografierte Handskizze, Whiteboard, draw.io, Excalidraw oder PowerPoint
- Komponenten als Kästchen und Pfeile mit kurzer Beschriftung, ein Pfeil steht für «ruft auf» oder «Datenfluss»
- Annahmen angegeben, zum Beispiel «etwa 100 Dateien pro Tag»
- offene Fragen aufgeführt
- Varianten nebeneinander zulässig

Umfang: Nicht alle Fragen müssen beantwortet sein, wenige Komponenten und offene Fragen genügen. Mit mehr Erfahrung kommen Varianten mit Abwägungen dazu: Kosten, Betrieb, Abhängigkeit vom Anbieter, Skalierung, Fehlerbehandlung.

Verwendung: Der Entwurf wird nicht bewertet. Im Einstieg von Modul 1 werden zwei bis drei Entwürfe kurz gezeigt, am Ende von Modul 2 folgt der Vergleich mit der umgesetzten Lösung. Eine Abgabe ist nicht nötig. Für die Besprechung im Einstieg genügt es, den Entwurf zur Hand zu haben.

## Die beiden Unterrichtstage

Im Unterricht wird jeder Schritt in einer Demo gezeigt. Mitbauen ist direkt (Code-along) oder später mit den Anleitungen der Labs möglich.

Jedes Lab hat einen Startstand (`start/`) für die Übung und einen Lösungsstand (`solution/`) für den Demo-Modus, damit der Einstieg bei jedem Lab möglich ist. Die Labs werden im Unterricht freigegeben.

---

## Kurzfassung (mit Erfahrung in Codespaces, Terraform und Azure CLI)

Vier Schritte, Details in den folgenden Abschnitten und im Anhang.

1. Das Kurs-Repo `cas-se-labs` im eigenen Konto forken (der Fork ist öffentlich).
2. Umgebung öffnen: Codespace auf `main` (im Browser oder in VS Code), oder lokal **Reopen in Container**.
3. `bash labs/00-setup/check.sh` ausführen, Ergebnis «Local environment is ready.».
4. `az login --use-device-code`, danach `export ARM_SUBSCRIPTION_ID=$(az account show --query id -o tsv)`.

## Die Umgebung im Überblick

![Zusammenspiel von Gerät, GitHub, Dev Container und Azure](assets/zusammenspiel.svg)

- Eigenes Gerät: Browser oder VS Code, mehr nicht.
- GitHub: Kurs-Repo und eigener Fork, in dem gearbeitet wird.
- Dev Container: Linux-Umgebung mit .NET, Terraform, Azure CLI und Docker.
- Docker Compose im Dev Container: lokale Test-Container mit der Anwendung und Ersatz für Cloud-Dienste, damit Tests ohne Azure-Konto möglich sind.
- Azure: nur, was Terraform anlegt.

Änderungen an der Cloud laufen über den Code, nicht über das Portal. Das Portal dient der Ansicht. Die ganze Plattform lässt sich so mit einem Befehl wieder entfernen.

## Varianten der Entwicklungsumgebung

Die Umgebung ist in einer einzigen Datei beschrieben (`.devcontainer/devcontainer.json`) und läuft an drei Orten.

| | Variante A: Codespace im Browser | Variante B: Codespace in VS Code | Variante C: lokaler Dev Container |
|---|---|---|---|
| Läuft auf | GitHub-Cloud | GitHub-Cloud | eigenem Rechner |
| Zu installieren | nichts | VS Code, Erweiterung «GitHub Codespaces» | Docker (Docker Desktop oder Docker Engine), Git, VS Code, Erweiterung «Dev Containers» |
| Kosten | Gratis-Kontingent von GitHub, danach kostenpflichtig | wie A | keine |
| Internet | dauernd | dauernd | nur für Downloads und Azure |
| Unterstützung im Kurs | voll | voll | ohne Gewähr |
| Einrichtung | Anhang A | Anhang B | Anhang C |

Alle drei laufen unter Windows, macOS und Linux. Bei A und B ist das Betriebssystem unerheblich, weil der Codespace ein Linux-Rechner in der Cloud ist, auch auf einem Apple-Silicon-Mac. Standard ist Variante A. Die Demo läuft in Variante A: Sie braucht keine Installation und ist auf allen Betriebssystemen gleich, die Werkzeuge sind im Codespace bereits eingerichtet, und Fehler durch lokale Docker-Installationen entfallen. Das Gratis-Kontingent von GitHub und eine dauernde Internetverbindung sind die Voraussetzungen.

Variante C setzt Docker auf dem eigenen Rechner voraus. VS Code startet den Dev Container über Docker Desktop. Ist Docker Desktop nicht gestartet, lässt sich der Dev Container nicht öffnen. Das ist die häufigste Fehlerquelle dieser Variante (Docker nicht gestartet, WSL 2 fehlt, Virtualisierung gesperrt, Firmenrichtlinie). Läuft Docker nicht, bleibt der Wechsel zu Variante A oder B.

Weiterführend zu den Varianten:

- [Development Containers](https://containers.dev/): Spezifikation von `devcontainer.json`
- [Dev Containers in VS Code](https://code.visualstudio.com/docs/devcontainers/containers): Variante C
- [GitHub Codespaces, Überblick](https://docs.github.com/codespaces/overview): Variante A und B
- [Abrechnung von Codespaces](https://docs.github.com/billing/managing-billing-for-your-products/managing-billing-for-github-codespaces/about-billing-for-github-codespaces): Gratis-Kontingent

---

## Eigenes Repo durch Fork erzeugen

Ein Fork ist eine eigene Kopie des Kurs-Repos im eigenen GitHub-Konto. Er bleibt mit dem Original verbunden. Dort entsteht die ganze Arbeit der beiden Module, und spätere Labs holt **Sync fork** nach.

**Ablauf**

1. Das Kurs-Repo `https://github.com/marcoamhof/cas-se-labs` öffnen (Link in ILIAS und Mail).
2. Oben rechts **Fork → Create a new fork** wählen.
3. Owner: eigenes Konto. Repository name: `cas-se-labs` beibehalten.
4. **Create fork** bestätigen.

**Erwartetes Ergebnis**

Das Repo `github.com/<name>/cas-se-labs` besteht (`<name>` ist der GitHub-Benutzername) und enthält die Ordner `labs/` und `.devcontainer/`. Unter dem Repo-Namen steht «forked from marcoamhof/cas-se-labs». Der Fork ist öffentlich.

**Was schiefgehen kann**

- Der Knopf **Fork** lässt sich nicht bedienen: Die Anmeldung bei GitHub fehlt. Das Kurs-Repo ist ohne Anmeldung lesbar, das Forken verlangt sie.
- Es besteht bereits ein Fork des Kurs-Repos im eigenen Konto, etwa aus einer früheren Durchführung: GitHub verweist auf den vorhandenen Fork. Dieser genügt, **Sync fork** bringt ihn auf den aktuellen Stand.
- Der Fork ist öffentlich: Ein Fork eines öffentlichen Repos ist öffentlich, die Sichtbarkeit lässt sich nicht ändern. Alles, was committet wird, ist für alle lesbar. Schlüssel, Connection Strings und Tokens gehören nie ins Repo, siehe Hinweise am Ende.

**Spätere Labs übernehmen**

Der Fork zeigt den Stand zum Zeitpunkt der Erzeugung. Labs, die später im Kurs-Repo erscheinen, holt **Sync fork** nach:

1. Eigene Änderungen committen und pushen.
2. Auf GitHub die Hauptseite des eigenen Forks öffnen.
3. Oberhalb der Dateiliste **Sync fork** wählen und **Update branch** bestätigen.
4. Im Codespace oder Dev Container den neuen Stand holen:

   ```bash
   git pull --no-rebase --no-edit
   ```

Kollidieren eigene Änderungen mit dem Original, lässt sich der Fork nicht mit **Update branch** aktualisieren. Im Terminal meldet `git pull` in diesem Fall `CONFLICT` und nennt die betroffene Datei. `git merge --abort` stellt den vorherigen Stand wieder her. Die eigene Arbeit bleibt unangetastet, wenn nur der Ordner des neuen Labs geholt wird. Das leisten diese Befehle im Terminal des Dev Containers:

```bash
git remote add upstream https://github.com/marcoamhof/cas-se-labs.git   # nur beim ersten Mal
git fetch upstream
git checkout upstream/main -- labs/01-storage
git commit -m "Take over lab 01"
```

- `labs/01-storage` steht für den Ordner des jeweiligen Labs.
- Der einfache Befehl `git pull` bricht mit «Need to specify how to reconcile divergent branches» ab, wenn eigene Commits und neue Commits auf GitHub nebeneinander bestehen. `--no-rebase` legt den Zusammenschluss per Merge fest, `--no-edit` übernimmt die vorgeschlagene Merge-Nachricht ohne Editor.
- `origin` zeigt auf den eigenen Fork, `upstream` auf das Kurs-Repo.
- Der Befehl holt nur diesen Ordner. Eigene Arbeit in anderen Ordnern bleibt unverändert.
- Dateien im geholten Lab-Ordner werden überschrieben. Eigene Änderungen dort sind vorher zu committen.
- `git remote add` meldet «remote upstream already exists», wenn der Eintrag schon besteht. Der Schritt entfällt dann.

Weiterführend: [Über Forks](https://docs.github.com/pull-requests/collaborating-with-pull-requests/working-with-forks/about-forks) und [Fork synchronisieren](https://docs.github.com/pull-requests/collaborating-with-pull-requests/working-with-forks/syncing-a-fork).

## Entwicklungsumgebung starten

Der erste Start baut den Dev Container auf, spätere Starts sind deutlich schneller. Die detaillierte Einrichtung steht im Anhang.

**Ablauf**

- Variante A: Im Repo **Code → Codespaces → Create codespace on main** wählen (Anhang A).
- Variante B: In VS Code die Erweiterung «GitHub Codespaces» installieren und den Codespace verbinden (Anhang B).
- Variante C: Docker, Git, VS Code und die Erweiterung «Dev Containers» installieren, Repo klonen, **Reopen in Container** wählen (Anhang C).

**Erwartetes Ergebnis**

Die Umgebung ist aufgebaut, und ein Terminal zeigt unter «== tool versions ==» die Versionen von .NET, Terraform, Azure CLI, Docker und Functions Core Tools.

**Was schiefgehen kann**

Die Fehlerbilder je Variante stehen im jeweiligen Anhang.

## Werkzeuge prüfen

Das Skript prüft die Werkzeuge und startet zwei kleine Tests, einen Docker-Container und ein .NET-Programm. Beide haben mit der Aufgabe nichts zu tun, sie zeigen nur, dass die Werkzeugkette läuft.

**Ablauf**

Im Terminal des Dev Containers (**Terminal → New Terminal**) ausführen:

```bash
bash labs/00-setup/check.sh
```

**Erwartetes Ergebnis**

```
Local tools:
  [ok]   dotnet       10.0.xxx
  [ok]   terraform    Terraform v1.xx.x
  [ok]   az           2.xx.x
  [ok]   docker       daemon 29.x.x
  [ok]   compose      2.xx.x

Functional tests:
  [ok]   docker-run   container ran and network works
  [ok]   dotnet-run   program compiled and ran
...
Local environment is ready.
```

Der Azure-Block darunter zeigt noch `[todo]`, die Anmeldung folgt im nächsten Abschnitt.

**Was schiefgehen kann**

- `[FAIL] docker`: Die Umgebung wurde ohne Docker-Daemon gestartet. **F1 → Rebuild Container** behebt das.
- `[FAIL] docker-run`: Das Netzwerk lässt den Download des Testimages nicht zu, etwa in einem Firmen- oder Hotel-WLAN oder hinter einem Proxy. Ein anderes Netz hilft. Im Codespace kommt das selten vor.
- `[FAIL] dotnet-run` oder ein anderes Werkzeug: Der Aufbau war unvollständig. Meist hilft **Rebuild Container**, sonst eine Mail mit der Fehlerzeile.

## Bei Azure anmelden

Die Anmeldung legt nichts in Azure an. Sie legt für die Azure CLI und Terraform fest, mit welchem Konto und in welchem Abo sie später arbeiten, und ist vor dem ersten `terraform apply` im Unterricht nötig. Im Dev Container läuft sie am einfachsten über einen Gerätecode.

**Ablauf**

```bash
az login --use-device-code
export ARM_SUBSCRIPTION_ID=$(az account show --query id -o tsv)
bash labs/00-setup/check.sh
```

`az login` zeigt einen Link (`microsoft.com/devicelogin`) und einen Code. Link im Browser öffnen, Code eingeben und mit dem Konto anmelden, unter dem das Abo angelegt wurde.

**Erwartetes Ergebnis**

Im Azure-Block von `check.sh` stehen `[ok] signed in` mit dem Namen des Abos und `[ok] ARM_SUBSCRIPTION_ID is set`.

**Was schiefgehen kann**

- Das Konto gehört zu mehreren Mandanten: `az login --use-device-code --tenant <Mandanten-ID>` wählt den richtigen.
- Es bestehen mehrere Abos: `az account list -o table` zeigt sie, `az account set --subscription "<Name>"` wählt eines. Danach `ARM_SUBSCRIPTION_ID` neu setzen.
- `ARM_SUBSCRIPTION_ID` fehlt in einem neuen Terminal: Die Variable gilt nur in dem Terminal, in dem sie gesetzt wurde.
- Der Code ist abgelaufen: Er gilt nur wenige Minuten. Ein neuer Aufruf von `az login --use-device-code` liefert einen neuen.
- «No subscriptions found»: Das Abo ist noch nicht aktiv, oder die Anmeldung erfolgte mit dem falschen Konto. Unter **Subscriptions** im Portal ist sichtbar, welches Abo zum Konto gehört.

Weiterführend:

- [Anmeldung bei der Azure CLI](https://learn.microsoft.com/cli/azure/authenticate-azure-cli): Anmeldearten, Gerätecode
- [Terraform bei Azure authentifizieren](https://learn.microsoft.com/azure/developer/terraform/authenticate-to-azure): `ARM_SUBSCRIPTION_ID` und Azure CLI
- [Mandanten und Abonnements](https://learn.microsoft.com/azure/azure-resource-manager/management/overview): Resource Manager, Ressourcengruppen
- [Was ist Microsoft Entra ID?](https://learn.microsoft.com/entra/fundamentals/whatis): Konten und Verzeichnis
- [Kosten im Testabo vermeiden](https://learn.microsoft.com/azure/cost-management-billing/manage/avoid-charges-free-account)

## Aufräumen

- Nach jedem Modul wird alles entfernt, was angelegt wurde. Der Befehl dafür, `terraform destroy`, wird im Unterricht gezeigt. Fixkosten laufen bei Leerlauf weiter, bis die Ressourcen weg sind.
- Codespace stoppen, wenn nicht gearbeitet wird: **F1 → Codespaces: Stop Current Codespace**. Ungenutzt stoppt er nach 30 Minuten von selbst und bleibt bestehen.
- Nach Abschluss des Labs den Codespace löschen: github.com/codespaces → Menü des Codespace → **Delete**. Vor dem Löschen sind Änderungen zu committen und zu pushen, sonst gehen sie verloren. Gepushter Code bleibt im Fork.

## Definition of Done

- [ ] Der Architektur-Entwurf (eine Seite) liegt vor, mit Annahmen und offenen Fragen. Er deckt mindestens Ausbaustufe 1 ab. Ausbaustufe 2 und die Unterschiede der beiden Szenarien kommen hinzu, soweit sie bekannt sind.
- [ ] Der Entwurf nennt den Standort der Daten und die Faktoren, die zur Wahl geführt haben.
- [ ] Der Fork `cas-se-labs` besteht im eigenen Konto.
- [ ] Die Entwicklungsumgebung startet (Variante A, B oder C).
- [ ] `bash labs/00-setup/check.sh` meldet «Local environment is ready.».
- [ ] `az login` ist erfolgt, `ARM_SUBSCRIPTION_ID` ist gesetzt, der Azure-Block von `check.sh` zeigt `[ok]`.

Bei Problemen genügt eine Mail mit der gewählten Variante (A, B oder C), dem Abschnitt und der letzten Fehlermeldung, vor dem Kurstag.

## Nächste Schritte

Modul 1 beginnt mit dem Einstieg: kurze Besprechung der Entwürfe, danach Umsetzung von Ausbaustufe 1.

## Hinweise

- Zugangsdaten, API-Schlüssel, `*.tfstate`, `*.tfvars` und `local.settings.json` gehören nie in Git. Die `.gitignore` fängt das meiste ab, ersetzt aber nicht die Aufmerksamkeit. Der Fork ist öffentlich, jede Datei darin ist für alle lesbar.
- Sicherheit steht in diesen Modulen nicht im Mittelpunkt. Die Plattform verzichtet auf Benutzeranmeldung und abgeschottete Netzwerke, eine Vereinfachung für den Kurs. Wo das eine Rolle spielt, weisen die Labs darauf hin. Für den Betrieb in der Praxis gelten andere Regeln, dazu besteht das Modul «Application Security».
- Regionen, Preise und Modellverfügbarkeit ändern sich. Alle Zahlen in diesem Lab haben den Stand 01.11.2026.

---

## Anhang: Entwicklungsumgebung einrichten

Voraussetzung für alle Varianten ist der Fork aus dem Abschnitt «Eigenes Repo durch Fork erzeugen». Alle drei Varianten starten denselben Dev Container aus `.devcontainer/devcontainer.json`.

### Anhang A: Variante A, Codespace im Browser

Voraussetzungen: GitHub-Konto (angemeldet), Browser, Repo `<name>/cas-se-labs`.

**Ablauf**

1. `github.com/<name>/cas-se-labs` öffnen.
2. Den grünen Knopf **Code** wählen.
3. Den Tab **Codespaces** öffnen.
4. **Create codespace on main** wählen. GitHub legt eine virtuelle Maschine an und baut den Dev Container nach `.devcontainer/devcontainer.json` (.NET 10 als Basis, dazu Docker, Terraform, Azure CLI und Functions Core Tools).
5. Warten, bis VS Code im Browser erscheint und das Terminal die Ausgabe von `post-create.sh` zeigt.
6. Fehlt das Terminal: **Terminal → New Terminal**.

Für spätere Sitzungen: `github.com/codespaces` öffnen und den bestehenden Codespace wählen. Ein neuer Codespace ist nicht nötig.

**Erwartetes Ergebnis**

VS Code läuft im Browser, der Explorer zeigt die Ordner `labs/` und `.devcontainer/`. Das Terminal zeigt unter «== tool versions ==» die Versionen der Werkzeuge.

**Was schiefgehen kann**

- Der Aufbau bricht ab: **F1 → Codespaces: View Creation Log** zeigt die Ursache in der letzten Zeile. Oft genügt **F1 → Rebuild Container**.
- Meldung zum Kontingent: Persönliche Konten haben ein monatliches Gratis-Kontingent, einsehbar unter github.com/settings/billing. Stoppen des Codespace nach jeder Sitzung verringert den Verbrauch.
- Der Browser blockiert Fenster oder Cookies von GitHub: Ausnahme für `github.com` und `github.dev` zulassen oder ein anderes Browserprofil verwenden.

### Anhang B: Variante B, Codespace in VS Code

Voraussetzungen: GitHub-Konto, Repo `<name>/cas-se-labs`, VS Code auf dem eigenen Gerät.

**Ablauf**

1. VS Code installieren (code.visualstudio.com). Windows: `winget install -e --id Microsoft.VisualStudioCode`. macOS: `brew install --cask visual-studio-code`.
2. Die Erweiterung **GitHub Codespaces** installieren: Ansicht **Extensions** (Ctrl+Shift+X, auf dem Mac Cmd+Shift+X), nach «GitHub Codespaces» suchen, **Install** wählen. Alternativ im Terminal `code --install-extension GitHub.codespaces`.
3. Bei der ersten Verbindung meldet sich VS Code bei GitHub an. Die Anmeldung läuft über den Browser, VS Code führt durch den Ablauf.
4. Codespace erzeugen: **F1 → Codespaces: Create New Codespace**, Repo `<name>/cas-se-labs` und Branch `main` wählen. Alternativ den Codespace wie in Anhang A im Browser erzeugen.
5. Verbinden: **F1 → Codespaces: Connect to Codespace** und den Codespace wählen. Alternativ im Browser über das Menü des Codespace **Open in VS Code**.

**Erwartetes Ergebnis**

VS Code läuft auf dem eigenen Gerät, links unten steht «Codespaces: …». Die Werkzeuge laufen in der Cloud, nicht auf dem Gerät. Das Terminal zeigt unter «== tool versions ==» die Versionen der Werkzeuge.

**Was schiefgehen kann**

- Die GitHub-Anmeldung schlägt fehl oder der Browser öffnet sich nicht: Variante A im Browser verwenden, sie braucht keine Anmeldung in VS Code.
- Ein Firmen-Proxy oder eine Firewall blockiert die Verbindung: Variante A im Browser oder ein anderes Netz verwenden.
- Der Aufbau bricht ab oder das Kontingent ist erschöpft: siehe Anhang A.

### Anhang C: Variante C, lokaler Dev Container

Voraussetzungen: eine funktionierende Container-Engine (Docker Desktop oder Docker Engine, ohne sie startet der Dev Container nicht), einige GB freier Speicherplatz, Recht zum Installieren von Software, Docker-Lizenzbedingungen geprüft (Docker Desktop ist für grössere Firmen kostenpflichtig, die Firmenregeln gelten). Getestet ist der Aufbau des Dev Containers auf x86-64. Für Apple Silicon (arm64) bieten alle Bestandteile arm64-Fassungen: die Basis-Images (.NET, Azurite) und die Installation von Docker, Terraform, Azure CLI und Functions Core Tools. Ein vollständiger Aufbau auf einem Apple-Silicon-Mac wurde nicht durchgeführt.

#### C.1 Windows

1. WSL 2 aktivieren: PowerShell als Administrator öffnen, `wsl --install` ausführen, Rechner neu starten.
2. Docker Desktop installieren: `winget install -e --id Docker.DockerDesktop` oder Installer von docker.com. Docker Desktop starten und warten, bis es «Engine running» anzeigt.
3. Git installieren: `winget install -e --id Git.Git`.
4. VS Code installieren: `winget install -e --id Microsoft.VisualStudioCode`.
5. Neues Terminal öffnen und `docker run hello-world` ausführen. Der Test meldet «Hello from Docker!».
6. Das Repo in einem normalen Ordner ablegen, zum Beispiel `C:\dev\`, nicht in OneDrive und nicht in einem synchronisierten Ordner.

#### C.2 macOS

1. Docker Desktop installieren (Installer von docker.com, passend zu Apple Silicon oder Intel, oder `brew install --cask docker`) und starten. Alternativen sind OrbStack oder Colima (`brew install colima docker`, danach `colima start`).
2. Git installieren: `xcode-select --install` oder `brew install git`.
3. VS Code installieren: `brew install --cask visual-studio-code`.
4. `docker run hello-world` ausführen. Der Test meldet «Hello from Docker!».

#### C.3 Linux

1. Docker Engine nach der Anleitung für die eigene Distribution installieren (docs.docker.com/engine/install).
2. Den eigenen Benutzer in die Gruppe `docker` aufnehmen: `sudo usermod -aG docker $USER`, danach neu anmelden.
3. Git über den Paketmanager installieren, VS Code von code.visualstudio.com.
4. `docker run hello-world` ausführen. Der Test meldet «Hello from Docker!».

#### C.4 Dev Container öffnen (alle Systeme)

1. Die Erweiterung **Dev Containers** installieren: Ansicht **Extensions**, nach «Dev Containers» suchen, **Install** wählen. Alternativ `code --install-extension ms-vscode-remote.remote-containers`.
2. Das Repo klonen: `git clone https://github.com/<name>/cas-se-labs.git`. Das Klonen braucht keine Anmeldung. Das spätere Pushen verlangt die Anmeldung bei GitHub, sie läuft über den Browser.
3. Den Ordner in VS Code öffnen: `code cas-se-labs`.
4. VS Code erkennt `.devcontainer/devcontainer.json` und zeigt unten rechts den Hinweis «Folder contains a Dev Container configuration file. Reopen folder to develop in a container». **Reopen in Container** baut die Umgebung. Der erste Aufbau lädt mehrere hundert MB bis einige GB.
5. Fehlt der Hinweis: **F1 → Dev Containers: Reopen in Container**.

#### C.5 Docker starten und prüfen (vor C.4)

Vor dem Öffnen des Dev Containers muss der Docker-Daemon laufen. Die Prüfung gelingt in jedem Terminal auf dem eigenen Rechner:

1. `docker info` ausführen. Die Ausgabe enthält einen Abschnitt «Server» mit einer Versionsnummer. Dann läuft Docker, und es geht weiter mit C.4.
2. Erscheint stattdessen `failed to connect to the docker API` (Windows) oder `Cannot connect to the Docker daemon` (macOS, Linux), läuft der Daemon nicht. Docker starten:
   - Windows (PowerShell): `Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"`
   - macOS: `open -a Docker`
   - Linux ohne Docker Desktop: `sudo systemctl start docker`
3. Warten, bis der Daemon antwortet. Unter Windows (PowerShell):

   ```powershell
   while (-not (docker info 2>$null | Select-String "Server Version")) { Start-Sleep 3 }; "Docker läuft"
   ```

   Auf macOS und Linux genügt es, `docker info` zu wiederholen, bis «Server» erscheint.
4. `docker run hello-world` ausführen. Der Test meldet «Hello from Docker!».

Bleibt Docker Desktop auf «Starting» oder startet nicht: WSL 2 prüfen (`wsl --status`), Docker Desktop neu starten, den Rechner neu starten. Auf Firmenrechnern kann eine Richtlinie die Virtualisierung blockieren. Dann Variante A oder B verwenden.

Der Hinweis erscheint nur lokal. Im Codespace ist die Umgebung bereits ein Dev Container. **Don't Show Again** blendet den Hinweis aus, der Befehl unter **F1** bleibt.

**Erwartetes Ergebnis**

Links unten steht «Dev Container: cas-se cloud labs», das Terminal ist ein Linux-Terminal im Container und zeigt unter «== tool versions ==» die Versionen der Werkzeuge. Zurück ins lokale Fenster führt **F1 → Dev Containers: Reopen Folder Locally**.

**Was schiefgehen kann**

- «Cannot connect to the Docker daemon» oder «failed to connect to the docker API»: Docker läuft nicht oder ist noch nicht fertig gestartet. Der Status muss «Running» bzw. «Engine running» lauten. Schrittfolge zum Starten und Prüfen in C.5.
- Der Aufbau bricht ab: **F1 → Dev Containers: Show Container Log** zeigt die Ursache in der letzten Zeile. Oft genügt **F1 → Rebuild Container**.
- Der Aufbau ist unter Windows sehr langsam: Das Repo liegt vermutlich in OneDrive oder auf einem Netzlaufwerk. Ein Ordner wie `C:\dev\` behebt das.
- Der Download der Images scheitert: Ein Firmen-Proxy oder eine Firewall blockiert den Zugriff auf die Registry. Ein anderes Netz oder Variante A im Browser verwenden.
- Linux: «permission denied» bei Docker: Der Benutzer fehlt in der Gruppe `docker`, siehe C.3, danach neu anmelden.
- Zu wenig Speicher auf der Festplatte: Alte Images mit `docker system prune` entfernen, nachdem geprüft ist, dass keine eigenen Container darunter fallen.
