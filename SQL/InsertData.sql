USE LIBRARY;
GO

INSERT INTO dbo.AirHeaterSystem (SystemName, Description)
VALUES
('Air Heater 1', 'PoC air heater system');
GO

INSERT INTO dbo.SensorType (SensorTypeName)
VALUES
('Temperature');
GO

-- Physical sensors only
INSERT INTO dbo.Sensors (
    AirHeaterSystemId,
    SensorName,
    SensorTypeId,
    DaqPort,
    Unit,
    Location
)
VALUES
(1, 'Temperature Sensor 1', 1, 'DAQ1_AI0', 'C', 'Inlet'),
(1, 'Temperature Sensor 2', 1, 'DAQ1_AI1', 'C', 'Outlet');
GO

-- OPC UA datapoints / control system tags
INSERT INTO dbo.OpcDatapoints (
    AirHeaterSystemId,
    DisplayName,
    OpcNodeId,
    DatapointType,
    Unit,
    SensorId
)
VALUES
(1, 'Temperature Setpoint', 'ns=2;s=Tempreature Setpoint', 'Setpoint', 'C', NULL),

(1, 'Temperature Process Value', 'ns=2;s=Tempreature Feedback', 'Feedback', 'C', 1);
GO