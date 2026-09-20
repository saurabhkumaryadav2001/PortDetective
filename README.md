# 🔍 Port Detective

A Windows desktop developer utility that helps you find which process or application is using a TCP port on your computer.

Developers often face errors like:

> "Port 5000 is already in use."

Port Detective makes it easy to identify the process using the port and provides useful information such as the Process ID, process name, executable path, and connection details.

## ✨ Features

- 🔍 Find which process is using a specific port
- 📊 Scan a range of TCP ports
- 🆔 Display Process ID (PID)
- 📁 Display executable path
- 🔎 Search by port, PID, or process name
- 🔄 Refresh active ports
- 🛑 Stop a selected process with confirmation
- 📋 Copy PID, port, and process information
- 📂 Open process file location
- ⚡ Quick access to common developer ports
- 📝 Application logging
- ⚙️ Configurable settings
- 🔐 Safe process termination with confirmation
- 🚫 Handles permission and process errors gracefully

## 🛠️ Technologies

- C#
- .NET 8
- WPF
- MVVM
- Dependency Injection
- Windows APIs
- System.Diagnostics
- System.Net.NetworkInformation

## 🖥️ Supported Ports

Port Detective provides quick access to commonly used development ports:

```text
3000  - React / Node.js
4200  - Angular
5000  - .NET / Flask
5173  - Vite
5432  - PostgreSQL
6379  - Redis
8000  - Development servers
8080  - Java / Spring Boot
1433  - SQL Server
3306  - MySQL
