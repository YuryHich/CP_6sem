# Быстрый старт

## Шаги для запуска проекта

### 1. Установите PostgreSQL
Убедитесь, что PostgreSQL установлен и запущен.

### 2. Создайте базу данных
```sql
CREATE DATABASE library_management;
```

### 3. Выполните SQL-скрипт
Подключитесь к базе данных и выполните файл `init.sql`:
```bash
psql -U postgres -d library_management -f init.sql
```

### 4. Настройте подключение
Отредактируйте `appsettings.json` и укажите правильные параметры подключения:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=library_management;Username=postgres;Password=ваш_пароль"
  }
}
```

### 5. Запустите приложение
```bash
dotnet restore
dotnet run
```

### 6. Откройте браузер
Перейдите по адресу: `http://localhost:5000` или `https://localhost:5001`

## Первые шаги

1. Зарегистрируйте нового пользователя на странице `/auth/register`
2. Войдите в систему на странице `/auth/login`
3. Просмотрите каталог книг на странице `/books`
4. Для доступа к админ-панели создайте пользователя с ролью `admin` вручную в базе данных

## Создание администратора

Для создания администратора выполните SQL-запрос:
```sql
-- Сначала создайте пользователя через регистрацию в приложении
-- Затем обновите роль в базе данных:
UPDATE Users 
SET role_id = (SELECT role_id FROM Roles WHERE role_name = 'admin')
WHERE username = 'ваш_username';
```

Или создайте пользователя напрямую в SQL (не забудьте хэшировать пароль через BCrypt):
```sql
-- Пароль: admin123
-- Хэш BCrypt: $2a$11$KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KI
-- (В реальном приложении используйте BCrypt.Net.BCrypt.HashPassword("admin123"))

INSERT INTO Users (user_id, username, email, password_hash, role_id, is_active)
VALUES (
    uuid_generate_v4(),
    'admin',
    'admin@library.com',
    '$2a$11$KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KIXL6KI',
    (SELECT role_id FROM Roles WHERE role_name = 'admin'),
    true
);
```

## Тестовые данные

После выполнения `init.sql` в базе данных уже есть:
- 3 тестовые книги (Война и мир, Преступление и наказание, 1984)
- 3 автора (Толстой, Достоевский, Оруэлл)
- 5 жанров
- По 5 копий каждой книги

## Возможные проблемы

### Ошибка подключения к БД
- Проверьте, что PostgreSQL запущен
- Убедитесь, что параметры подключения в `appsettings.json` правильные
- Проверьте, что база данных `library_management` создана

### Ошибка при выполнении SQL
- Убедитесь, что расширение `uuid-ossp` установлено: `CREATE EXTENSION IF NOT EXISTS "uuid-ossp";`
- Проверьте права доступа пользователя PostgreSQL

### Ошибка компиляции
- Убедитесь, что установлен .NET 8.0 SDK
- Выполните `dotnet restore` для восстановления пакетов NuGet

