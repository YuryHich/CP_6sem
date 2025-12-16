-- Миграция: Добавление полей для подтверждения email и восстановления пароля
-- Выполнить в базе данных library_management

-- Добавляем поля для подтверждения email
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS confirmation_token VARCHAR(255),
ADD COLUMN IF NOT EXISTS is_email_confirmed BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS password_reset_token VARCHAR(255),
ADD COLUMN IF NOT EXISTS reset_token_expiration TIMESTAMP WITH TIME ZONE;

-- Создаем индекс для быстрого поиска по токену подтверждения
CREATE INDEX IF NOT EXISTS idx_users_confirmation_token ON Users(confirmation_token) WHERE confirmation_token IS NOT NULL;

-- Создаем индекс для быстрого поиска по токену сброса пароля
CREATE INDEX IF NOT EXISTS idx_users_reset_token ON Users(password_reset_token) WHERE password_reset_token IS NOT NULL;

