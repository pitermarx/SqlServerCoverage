CREATE TABLE TestTable(id int, name NVARCHAR(10))
GO

CREATE PROC TestProcedureForCoverage(@value int)
AS BEGIN
    IF (@value = 1)
        SELECT 10
    ELSE
        SELECT 20
END
GO

CREATE FUNCTION GetNumber() RETURNS int
AS
BEGIN
    RETURN (1);
END;
GO

CREATE FUNCTION GetInlineTable() RETURNS TABLE
AS RETURN (SELECT id, name FROM TestTable WHERE ID > 1)
GO

CREATE FUNCTION GetTable()
RETURNS @tempTable TABLE (id int, name NVARCHAR(10))
AS
BEGIN
    INSERT @tempTable SELECT * FROM TestTable
    RETURN
END
GO

CREATE VIEW TestView
AS
SELECT * FROM TestTable WHERE id > 0
GO

CREATE TRIGGER TestTrigger
ON TestTable
AFTER INSERT
AS
    UPDATE TestTable
    SET name = CONCAT(name, 'Triggered')
    WHERE name NOT LIKE '%Triggered'
GO

EXEC dbo.TestProcedureForCoverage 2
GO

SELECT * from GetTable()
