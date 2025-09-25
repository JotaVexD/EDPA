# EDPA - Elite Dangerous Piracy Analytics

<div align="center">

[![EDPA Logo](https://img.shields.io/badge/Elite-Dangerous-orange)](https://www.elitedangerous.com/en-US)
![Version](https://img.shields.io/badge/Version-Development--Preview-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/en-us/)
[![WPF](https://img.shields.io/badge/4.0.3-WPF-UI?color=%23088da5)](https://github.com/lepoco/wpfui)

*A analytics tool for PvE piracy targeting miners in Elite Dangerous*

*This is a tool mainly for my personal use that I decided to make public*

</div>

## Project Status

> [!CAUTION]
> **Development Preview** 
> - This is currently in active development and not yet a production/use-ready. 
> - Features and code are being added and refined. (Code still trash and very desorganized)


## Objective

EDPA is designed to assist Commanders engaged in PvE piracy by analyzing systems within a specified range of a reference system. The tool uses a scoring system to identify the most profitable piracy targets based on:

- **Economic factors** (Industrial, Refinery, High Tech economies)
- **Government types** (Anarchy, Feudal, Corporate, etc.)
- **Security levels** (Anarchy, Low, Medium, High)
- **Population demographics**
- **Faction states** (Boom, Civil Unrest, War, etc.)
- **Market demand** for valuable commodities

## Features

### Current Features
- **System Analysis** - Scoring of systems for piracy viability
- **Customizable Search Radius** - Search within 1-50 LY of any reference system
- **Weighted Scoring System** - Configurable weights for different economic and political factors

### Planed Features
- **Presence of planetary rings**
- **Additional data sources** (Inara, EDDB integration)
- **More sophisticated scoring algorithms**
- **Route planning for multiple targets**
- **Real-time market data integration**
- **Combat zone analysis features**

## Using

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime
- EDSM API Key ([Get one here](https://www.edsm.net/en/settings/api))

## Data Privacy & Security
- **Local Storage:** All data is stored locally on your machine
- **Encrypted API Keys:** EDSM API keys are encrypted using Windows DPAPI
- **No Data Collection:** The application does not collect or transmit any personal data
- **Cache Management:** Automatic cache cleanup with manual control options

### First-Time Setup
> [!WARNING]
> **Many thinks are still beyond my control**
> - Memory management is something I haven't considered yet.

- Go to [Latest Releases](https://github.com/JotaVexD/EDPA/releases/latest)
- Download the last EDPA.exe
- Open the App and go to [EDSM](https://www.edsm.net/en/settings/api)
- Get your API and Save on the **Settings**
- **Use**
- Any problem or feedack open a [Issue](https://github.com/JotaVexD/EDPA/issues)

## Disclaimer
This tool is designed for PvE (Player vs Environment) piracy only. I do not condone harassment of other players or exploitation of game mechanics. Always follow Frontier Developments' terms of service and the Elite Dangerous community guidelines.

## License
This project is licensed under the MIT License - see the LICENSE file for details.


<details>
  <summary>Software Screenshots</summary>
  
  ![Exemple Search](https://github.com/user-attachments/assets/2d9fb43c-8405-4fb7-af43-aa75a7d49a85)
  ![Exemple System Details](https://github.com/user-attachments/assets/76535df5-8178-4dbe-b1c8-53fa4b1e11df)
  ![Exemple Saved Systems](https://github.com/user-attachments/assets/7d97c508-21a4-4941-aa3b-ea1c46d8ad89)
  ![Exemple Spansh](https://github.com/user-attachments/assets/21735c13-3a39-4a61-9011-8f170bd8aff0)
  
</details>
