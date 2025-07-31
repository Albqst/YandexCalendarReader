# 📅 Yandex Calendar Reader

![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)
![PostgreSQL](https://img.shields.io/badge/Postgres-16-blue)
![Grafana](https://img.shields.io/badge/Grafana-Visualization-orange)
![Status](https://img.shields.io/badge/status-in--development-yellow)

> Проект для чтения событий из [Yandex Calendar (CalDAV)](https://yandex.ru/support/calendar/) с сохранением данных в PostgreSQL и отображением их в Grafana.

---

## Описание

**YandexCalendarReader** — это ASP.NET Core Web API, который:
-  Авторизуется через OAuth в Яндекс.Календаре
-  Считывает события через CalDAV
- ️ Сохраняет данные в PostgreSQL
-  Предоставляет REST-эндпоинт `/events` для получения событий
-  Поддерживает визуализацию через Grafana

---

## Быстрый старт

### Зависимости

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- Учетная запись в Яндекс с активированным календарем

### ⚙️ Запуск с Docker

```bash

docker compose up -d
