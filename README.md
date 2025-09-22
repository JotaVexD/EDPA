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
- **Presence of planetary rings**

## Features

### Current Features
- **System Analysis** - Scoring of systems for piracy viability
- **Customizable Search Radius** - Search within 1-50 LY of any reference system
- **Weighted Scoring System** - Configurable weights for different economic and political factors

### Planed Features
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
- TODO

## Disclaimer
This tool is designed for PvE (Player vs Environment) piracy only. I do not condone harassment of other players or exploitation of game mechanics. Always follow Frontier Developments' terms of service and the Elite Dangerous community guidelines.

## License
This project is licensed under the MIT License - see the LICENSE file for details.


<details>
  <summary>Software Screenshots</summary>
  
  ![Exemple Search](https://github.com/user-attachments/assets/54ef5079-075a-466d-8387-68947133e760)
  ![Exemple System Details](https://github.com/user-attachments/assets/23b7e4a3-efdd-4b9f-9125-5bda55169394)
  ![Exemple Saved Systems](https://github.com/user-attachments/assets/b4adeb99-19a8-4fa3-9774-eb9783c7b668)
  
</details>
