# Gra w Statki Online (C# / WPF) – Architektura Klient-Serwer

W pełni funkcjonalna, sieciowa gra planszowa w stylu klasycznych Statków, zaimplementowana jako aplikacja desktopowa. Projekt skupia się na asynchronicznej komunikacji sieciowej, architekturze klient-serwer oraz programowaniu zorientowanym obiektowo.

## Główne funkcjonalności
* **Tryb wieloosobowy (Sieć):** Rozgrywka dwóch graczy na żywo wykorzystująca połączenie TCP/IP.
* **Matchmaking:** Automatyczne parowanie graczy oczekujących na serwerze.
* **System kont i autoryzacja:** Ekran logowania i rejestracji połączony z bazą danych PostgreSQL (możliwość gry również jako Gość).
* **Logika i walidacja:** Złożony system rozmieszczania statków z walidacją zasad (np. brak statków w formie kwadratów) oraz mechanizm odliczania czasu tury (45 sekund).
* **Zapisywanie wyników:** Automatyczna aktualizacja statystyk i wyników graczy w bazie po zakończeniu meczu.

## Dokumentacja i Kod
* Pełna dokumentacja projektu (architektura, funkcjonalności) znajduje się w pliku **Dokumentacja.pdf** w głównym katalogu repozytorium.
* Kod źródłowy jest szczegółowo opisany za pomocą znaczników XML.

## Technologie
* **Język:** C# (.NET)
* **Framework UI:** WPF (Windows Presentation Foundation) z wykorzystaniem `ContentControl` do nawigacji.
* **Komunikacja sieciowa:** TCP/IP (`System.Net.Sockets`), komunikacja asynchroniczna.
* **Baza danych:** PostgreSQL

## Architektura projektu
Projekt został podzielony na niezależne moduły:
1. **Serwer**: Zarządza połączeniami TCP, utrzymuje stan gry i paruje graczy.
2. **Klient (WPF)**: Moduł interfejsu podzielony na logiczne ekrany (Login, MainMenu, GameSearch, GameFound, GameScreen).
3. **Moduł bazy danych (`GameSave`, `Login`)**: Odpowiada za bezpieczne połączenie z PostgreSQL i utrwalanie danych.

## Instrukcja uruchomienia (Lokalnie)
1. Upewnij się, że posiadasz aktywną **lokalną** instancję bazy danych **PostgreSQL**.
2. W pierwszej kolejności uruchom projekt Serwera.
3. Uruchom projekt Klienta (można otworzyć wiele instancji, aby symulować kilku graczy).
4. Zaloguj się, zarejestruj lub zagraj jako gość, a następnie kliknij "Znajdź grę".

# Uwagi techniczne
**Projekt wymaga skonfigurowania ConnectionString w pliku konfiguracyjnym serwera.**
**Do poprawnego działania w 100% niezbędna jest lokalna instancja bazy danych (szczegóły w PDF).**
