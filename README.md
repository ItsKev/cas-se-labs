# CAS Software Engineering in the Age of AI: Cloud-Labs

Kursrepo zu den beiden Cloud-Modulen «Cloud Data Access & Processing» (Modul 1) und «Cloud-Computing, Serverless & Hyper Scaler Platforms» (Modul 2).

## Einstieg

Dieses Repo wird geforkt. **Fork → Create a new fork** erzeugt eine eigene Kopie im eigenen GitHub-Konto, in der gearbeitet wird. Dieses Repo selbst bleibt unverändert. Spätere Labs holt **Sync fork** in den eigenen Fork, danach im Codespace `git pull --no-rebase --no-edit`.

Die Anleitung beginnt mit [labs/00-setup/lab-00.md](labs/00-setup/lab-00.md).

## Inhalt

| Ordner | Inhalt |
|---|---|
| `labs/` | ein Ordner pro Lab mit `lab-NN.md`, ab Lab 01 mit `start/` (Stand vor dem Lab) und `solution/` (Stand danach) |
| `.devcontainer/` | Entwicklungsumgebung (Dev Container) für Codespaces und lokal |

## Labs

| Lab | Titel | Inhalt |
|---|---|---|
| 00 | Aufgabe und Arbeitsumgebung | Aufgabe, Architektur-Entwurf, Fork, Entwicklungsumgebung, Azure-Anmeldung |
| 01 | Speicher wählen und Daten ablegen | Speichertypen, Blob Storage, Dokumenten-API lokal gegen Azurite |
| 02 | Infrastruktur als Code mit Terraform | Storage Account, Registry, Managed Identity und Rollen als Terraform-Code |
| 03 | Container bauen und betreiben | Image bauen, in die Registry laden, als Container App betreiben |

Modul 1 umfasst die Labs 01 bis 03 in dieser Reihenfolge. Die Labs 01 bis 03 lassen sich als Übung oder im Demo-Modus durcharbeiten. Welcher Ordner jeweils gilt, nennt jedes Lab zu Beginn.

## Hinweise

- Ein Fork eines öffentlichen Repos ist öffentlich. Zugangsdaten, Schlüssel, `*.tfstate`, `*.tfvars` und `local.settings.json` gehören nie ins Repo.
- Der Inhalt dieses Repos wird aus einem Arbeitsrepo veröffentlicht. Änderungen, die direkt hier erfolgen, werden überschrieben.
