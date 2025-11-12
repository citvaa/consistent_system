# Consistent System - Distributed Temperature Sensor Network

A .NET-based distributed temperature sensor monitoring system implementing Byzantine Fault Tolerance (BFT) through quorum-based consensus algorithms.

## Overview

Consistent System demonstrates a real-world distributed IoT scenario where multiple independent temperature sensors report measurements to a central coordinator. The system automatically detects inconsistencies, achieves consensus through voting mechanisms, and maintains data integrity across all nodes through automatic synchronization.

## Architecture

The system consists of four main components:

### Temperature Sensors (3 instances)
- **Ports**: 8000, 8001, 8002
- WCF services that continuously generate temperature measurements
- Each sensor maintains its own PostgreSQL database
- Thread-safe concurrent access using ReaderWriterLockSlim
- Simulated readings: 273.15K + random offset (0-30K)

### Temperature Unit Coordinator
- **Port**: 8003
- Central coordinator service implementing consensus logic
- Aggregates readings from all sensors in parallel
- Detects inconsistencies and triggers synchronization
- Maintains history of up to 1000 valid readings

### PostgreSQL Databases
- Three independent databases: `sensor0_db`, `sensor1_db`, `sensor2_db`
- Stores measurement history with timestamps
- Indexed for optimized query performance

## Key Features

### Consensus Algorithm
- **Quorum-based voting**: Requires 2 out of 3 sensors to agree
- **Precision validation**: Consensus value must be within 5K of the average
- Returns valid temperature or triggers synchronization if consensus fails

### Automatic Synchronization
- Triggered when quorum is not reached or precision threshold is violated
- Calculates average from historical valid readings
- Propagates synchronized value to all sensor nodes
- Auto-sync runs every 1 minute to ensure consistency

### Fault Tolerance
- System remains operational with up to 1 sensor failure
- Automatic detection and correction of inconsistent data
- Thread-safe operations across distributed nodes

## Technology Stack

- **Framework**: .NET Framework 4.7.2
- **Communication**: Windows Communication Foundation (WCF)
- **Database**: PostgreSQL 12+
- **Database Driver**: Npgsql 6.0.8
- **Language**: C#

## Prerequisites

- .NET Framework 4.7.2 or higher
- PostgreSQL 12+ running on localhost:5432
- NuGet package manager for dependency restoration

## Installation

### Quick Setup (Windows)

Run the automated setup script:

```batch
setup_all.bat
```

This will:
1. Create three PostgreSQL databases
2. Initialize database schemas with indexes
3. Configure the system for immediate use

### Manual Setup

1. **Create Databases**:
   ```bash
   psql -U postgres -f setup_database.sql
   ```

2. **Initialize Each Sensor Database**:
   ```bash
   psql -U postgres -d sensor0_db -f setup_sensor0_db.sql
   psql -U postgres -d sensor1_db -f setup_sensor1_db.sql
   psql -U postgres -d sensor2_db -f setup_sensor2_db.sql
   ```

3. **Configure Connection Strings**:
   Update `Web.config` with your PostgreSQL credentials:
   ```xml
   <connectionStrings>
     <add name="Sensor1" connectionString="Host=localhost;Port=5432;Database=sensor0_db;Username=postgres;Password=your_password"/>
     <add name="Sensor2" connectionString="Host=localhost;Port=5432;Database=sensor1_db;Username=postgres;Password=your_password"/>
     <add name="Sensor3" connectionString="Host=localhost;Port=5432;Database=sensor2_db;Username=postgres;Password=your_password"/>
   </connectionStrings>
   ```

4. **Restore NuGet Packages**:
   ```bash
   nuget restore
   ```

5. **Build and Run**:
   ```bash
   msbuild consistent_system.sln
   cd consistent_system\bin\Debug
   consistent_system.exe
   ```

## Usage

Once running, the system will:

1. Start three sensor services on ports 8000-8002
2. Start the coordinator service on port 8003
3. Begin collecting temperature readings every 2 seconds
4. Display color-coded console output:
   - Green: Successful consensus achieved
   - Red: Inconsistency detected, synchronization triggered
   - Yellow: Synchronization in progress

The application runs continuously until manually stopped (Ctrl+C).

## Configuration Parameters

Key parameters in `TemperatureUnit.svc.cs`:

- `QUORUM_SIZE = 2`: Minimum sensors required for consensus
- `PRECISION = 5`: Tolerance threshold in Kelvin
- `HISTORY_LIMIT = 1000`: Maximum readings to maintain
- `AUTO_SYNC_DELAY = 1 minute`: Automatic synchronization interval
- `N_SENSORS = 3`: Total number of sensor nodes

## Project Structure

```
Consistent_system/
├── consistent_system/
│   ├── Program.cs                    # Application entry point
│   ├── TemperatureSensor.svc.cs      # Sensor service implementation
│   ├── TemperatureUnit.svc.cs        # Coordinator with consensus logic
│   ├── SensorDatabase.cs             # PostgreSQL data access layer
│   ├── ITemperatureSensor.cs         # Sensor service contract
│   ├── ITemperatureUnit.cs           # Coordinator service contract
│   └── Web.config                    # Configuration and connection strings
├── setup_all.bat                     # Automated setup script
├── setup_database.sql                # Database creation script
└── setup_sensor*.sql                 # Individual sensor schema scripts
```

## How It Works

### Normal Operation Flow

1. Each sensor generates temperature readings every 1-10 seconds
2. Coordinator requests readings from all sensors in parallel
3. Consensus algorithm evaluates readings:
   - Groups readings by rounded values
   - Checks for quorum (2+ sensors agreeing)
   - Validates precision (within 5K of average)
4. Returns valid temperature or `NaN` if consensus fails

### Synchronization Flow

When inconsistency is detected:

1. Calculate average from historical valid readings
2. Send sync command to all sensor nodes
3. Each sensor stores the synchronized value
4. Consistency is restored across the system

## Use Cases

This system demonstrates:

- **Distributed Systems Consistency**: Byzantine Fault Tolerance patterns
- **Consensus Algorithms**: Quorum-based voting mechanisms
- **IoT Sensor Networks**: Multiple sensors with central coordination
- **Data Integrity**: Automatic detection and correction of anomalies
- **Fault Tolerance**: Resilience against individual sensor failures

## License

This project is an educational demonstration of distributed systems concepts.

## Contributing

This is an academic project. For questions or suggestions, please refer to the documentation or contact the project maintainers.
