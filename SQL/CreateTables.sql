USE LIBRARY;
GO

IF OBJECT_ID('dbo.CurrentMeasurement', 'U') IS NOT NULL
    DROP TABLE dbo.CurrentMeasurement;

IF OBJECT_ID('dbo.Measurement', 'U') IS NOT NULL
    DROP TABLE dbo.Measurement;

IF OBJECT_ID('dbo.Sensors', 'U') IS NOT NULL
    DROP TABLE dbo.Sensors;

IF OBJECT_ID('dbo.SensorType', 'U') IS NOT NULL
    DROP TABLE dbo.SensorType;

IF OBJECT_ID('dbo.AirHeaterSystem', 'U') IS NOT NULL
    DROP TABLE dbo.AirHeaterSystem;
GO

IF OBJECT_ID('dbo.OpcDatapoints', 'U') IS NOT NULL
    DROP TABLE dbo.OpcDatapoints;

CREATE TABLE dbo.AirHeaterSystem (
    AirHeaterSystemId INT PRIMARY KEY IDENTITY(1,1),
    SystemName VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE dbo.SensorType (
    SensorTypeID INT PRIMARY KEY IDENTITY(1,1),
    SensorTypeName VARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Sensors (
    SensorId INT PRIMARY KEY IDENTITY(1,1),

    AirHeaterSystemId INT NOT NULL,
    SensorName VARCHAR(100) NOT NULL,
    SensorTypeId INT NOT NULL,

    DaqPort VARCHAR(100) NULL,
    Unit VARCHAR(50) NULL,
    Location VARCHAR(100) NULL,

    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Sensors_AirHeaterSystem
        FOREIGN KEY (AirHeaterSystemId)
        REFERENCES dbo.AirHeaterSystem(AirHeaterSystemId),

    CONSTRAINT FK_Sensors_SensorType
        FOREIGN KEY (SensorTypeId)
        REFERENCES dbo.SensorType(SensorTypeID)
);
GO

CREATE TABLE dbo.OpcDatapoints (
    OpcDatapointId INT PRIMARY KEY IDENTITY(1,1),

    AirHeaterSystemId INT NOT NULL,

    DisplayName VARCHAR(100) NOT NULL,
    OpcNodeId VARCHAR(300) NOT NULL,

    DatapointType VARCHAR(50) NOT NULL,
    Unit VARCHAR(50) NULL,

    -- Optional link to physical sensor
    SensorId INT NULL,

    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_OpcDatapoints_AirHeaterSystem
        FOREIGN KEY (AirHeaterSystemId)
        REFERENCES dbo.AirHeaterSystem(AirHeaterSystemId),

    CONSTRAINT FK_OpcDatapoints_Sensors
        FOREIGN KEY (SensorId)
        REFERENCES dbo.Sensors(SensorId),

    CONSTRAINT UQ_OpcDatapoints_OpcNodeId
        UNIQUE (OpcNodeId)
);
GO

CREATE UNIQUE INDEX UX_Sensors_DaqPort
ON dbo.Sensors(DaqPort)
WHERE DaqPort IS NOT NULL;
GO


CREATE TABLE dbo.CurrentMeasurement (
    OpcDatapointId INT PRIMARY KEY,

    MeasurementValue FLOAT NULL,
    MeasurementText VARCHAR(255) NULL,

    StatusCode VARCHAR(100) NULL,

    SourceTimestamp DATETIME2 NULL,
    ServerTimestamp DATETIME2 NULL,

    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_CurrentMeasurement_OpcDatapoints
        FOREIGN KEY (OpcDatapointId)
        REFERENCES dbo.OpcDatapoints(OpcDatapointId)
);
GO

CREATE TABLE dbo.Measurement (
    MeasurementId BIGINT PRIMARY KEY IDENTITY(1,1),

    OpcDatapointId INT NOT NULL,

    MeasurementValue FLOAT NULL,
    MeasurementText VARCHAR(255) NULL,

    StatusCode VARCHAR(100) NULL,

    SourceTimestamp DATETIME2 NULL,
    ServerTimestamp DATETIME2 NULL,

    MeasurementTimeStamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Measurement_OpcDatapoints
        FOREIGN KEY (OpcDatapointId)
        REFERENCES dbo.OpcDatapoints(OpcDatapointId)
);
GO

CREATE INDEX IX_Measurement_OpcDatapointId
ON dbo.Measurement(OpcDatapointId);
GO

CREATE INDEX IX_Measurement_SourceTimestamp
ON dbo.Measurement(SourceTimestamp);
GO

CREATE INDEX IX_Measurement_MeasurementTimeStamp
ON dbo.Measurement(MeasurementTimeStamp);
GO

CREATE INDEX IX_Measurement_OpcDatapoint_Time
ON dbo.Measurement(OpcDatapointId, SourceTimestamp);
GO