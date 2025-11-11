-- ExamAutoGrader 数据库初始化脚本
-- 创建数据库和用户

CREATE DATABASE IF NOT EXISTS `examgrader` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 使用数据库
USE `examgrader`;

-- 设置时区
SET time_zone = '+00:00';

-- 创建必要的表将由EF Core迁移处理
-- 这个脚本主要用于初始用户和权限设置