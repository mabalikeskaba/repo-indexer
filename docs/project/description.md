# Projektbeschreibung: repo-indexer

## Ziel des Projekts

Das Projekt "repo-indexer" soll einen Service bereitstellen, der in einem Docker-Container läuft. Dieser Service überwacht ein Root-Verzeichnis, in dem sich mehrere Repositories befinden. Ziel ist es, die Repositories zu indexieren, um den Kontext bei Agenten-Anfragen zu reduzieren.

## Technologie-Stack

- **Sprache / Framework:** .NET C#
- **Laufzeitumgebung:** Docker Container

## Funktionsweise

1. **Startparameter:**
   - Beim Start des Services wird das Root-Verzeichnis als Argument übergeben.

2. **Repo-Erkennung:**
   - Ein Unterordner wird als Repository erkannt, wenn er einen `.git`-Ordner enthält.

3. **Trigger der Indexierung:**
   - Der Service verwendet einen **FileWatcher**, der das Root-Verzeichnis rekursiv überwacht.
   - Bei jeder Änderung an einer `.cs`-Datei (Erstellen, Ändern, Löschen) wird die Indexierung des betroffenen Repositories automatisch ausgelöst.

4. **Indexierung:**
   - Der Service legt in jedem Repository-Root-Verzeichnis einen Ordner namens `.repo-indexer` an.
   - In diesem Ordner wird eine JSON-Datei namens `repo.json` erstellt und bei Änderungen aktualisiert.

5. **Inhalt der `repo.json`:**
   - Die Datei enthält eine Liste aller `.cs`-Dateien des jeweiligen Repositories.
   - Zu jeder Datei wird eine Liste der enthaltenen Klassen gespeichert.
   - Jede Klasse enthält:
     - Eine **Summary** (aus dem XML-Dokumentationskommentar `<summary>`)
     - Eine Liste der darin definierten **Methoden**

6. **Ausgeschlossene Pfade:**
   - Folgende Verzeichnisse und Dateien werden bei der Indexierung ignoriert:
     - `bin/`
     - `debug/`
     - `package.json`

## Schema der `repo.json`

```json
{
  "repoName": "my-repo",
  "generatedAt": "2026-02-22T10:00:00Z",
  "files": [
    {
      "path": "src/Services/MyService.cs",
      "classes": [
        {
          "name": "MyService",
          "summary": "Provides core functionality for processing data.",
          "methods": ["ProcessData", "GetResult"]
        }
      ]
    }
  ]
}
```

## Fehlerbehandlung

- Nicht-parsbare `.cs`-Dateien werden übersprungen und ein Fehler wird geloggt.
- Die Indexierung einer Datei schlägt nicht die gesamte Repo-Indexierung fehl.

## Nutzen der Indexierung

Der Hauptzweck der Indexierung besteht darin, bei Anfragen von Agenten nicht das gesamte Repository übertragen zu müssen. Stattdessen wird nur die `repo.json`-Datei verwendet, um den Kontext für die Anfrage bereitzustellen. Dadurch wird der Datenumfang pro Anfrage erheblich reduziert.
