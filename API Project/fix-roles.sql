-- Fix role names in database
UPDATE UserRoles SET RoleName = 'WarehouseOperator' WHERE RoleName = 'WarehouseOperative';
UPDATE Users SET Role = 'WarehouseOperator' WHERE Role = 'WarehouseOperative'; 