-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1:3306
-- Время создания: Ноя 20 2026 г., 11:02
-- Версия сервера: 5.7.39-log
-- Версия PHP: 7.2.34

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База данных: `RosPiv`
--

-- --------------------------------------------------------

--
-- Структура таблицы `Users`
--

CREATE TABLE `Users` (
  `id` int(11) NOT NULL,
  `login` varchar(50) NOT NULL,
  `password_hash` varchar(100) NOT NULL,
  `role` varchar(20) NOT NULL,
  `full_name` varchar(150) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Дамп данных таблицы `Users`
--

INSERT INTO `Users` (`id`, `login`, `password_hash`, `role`, `full_name`) VALUES
(1, 'admin', '$2a$11$bcynfES5pu86Xa5VHXnxueHxTc3wCSJg6rH/q7q3VUJuAvYs6.NDy', 'administrator', 'Иванов Иван Иванович'),
(2, 'director', '$2b$11$X.H92esikCAM47iiUYq32OmIv.3O69mXoNT/ODqigKu3oOFFBOUvK', 'director', 'Смирнов Алексей Владимирович'),
(3, 'manager', '$2b$11$X.H92esikCAM47iiUYq32OmIv.3O69mXoNT/ODqigKu3oOFFBOUvK', 'manager', 'Петрова Анна Сергеевна'),
(4, 'manager2', '$2b$11$X.H92esikCAM47iiUYq32OmIv.3O69mXoNT/ODqigKu3oOFFBOUvK', 'manager', 'Соколов Дмитрий Андреевич'),
(5, 'manager3', '$2b$11$X.H92esikCAM47iiUYq32OmIv.3O69mXoNT/ODqigKu3oOFFBOUvK', 'manager', 'Кузнецова Мария Викторовна'),
(6, 'storekeeper', '$2b$11$X.H92esikCAM47iiUYq32OmIv.3O69mXoNT/ODqigKu3oOFFBOUvK', 'manager', 'Волков Павел Дмитриевич');

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `Users`
--
ALTER TABLE `Users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `login` (`login`);

--
-- AUTO_INCREMENT для сохранённых таблиц
--

--
-- AUTO_INCREMENT для таблицы `Users`
--
ALTER TABLE `Users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
