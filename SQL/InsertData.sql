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

INSERT INTO dbo.OpcDatapoints (
    AirHeaterSystemId,
    DatapointCode,
    DisplayName,
    OpcNodeId,
    DatapointType,
    Unit,
    SensorId
)
VALUES
(
    1,
    'AH1_TEMP_SP',
    'Air Heater 1, Operator Setpoint',
    'ns=2;s=Temperature Setpoint',
    'Setpoint',
    'C',
    NULL
),
(
    1,
    'AH1_TEMP_PV',
    'Air Heater 1, Sensor 1',
    'ns=2;s=Temperature Process Value',
    'ProcessValue',
    'C',
    1
);
GO