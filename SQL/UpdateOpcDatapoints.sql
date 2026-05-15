UPDATE dbo.OpcDatapoints
SET OpcNodeId = 'ns=2;s=Tempreature Process Value'
WHERE OpcNodeId = 'ns=2;s=Tempreature Feedback'