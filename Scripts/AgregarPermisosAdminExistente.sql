-- Script para agregar permisos de "admin" al rol Administrador
-- Ejecutar en PostgreSQL

-- 1. Verificar el rol actual del usuario test@chikicut.com
SELECT 
    u.email,
    r.nombre as rol_name,
    r.permisos
FROM app.usuario u
JOIN app.rol r ON u.rol_id = r.id
WHERE u.email = 'test@chikicut.com';

-- 2. Actualizar permisos del rol "Administrador" para incluir módulo "admin"
UPDATE app.rol 
SET permisos = jsonb_set(
    permisos::jsonb, 
    '{admin}', 
    '{"read": true, "access": true}'::jsonb
)
WHERE LOWER(nombre) LIKE '%administrador%';

-- 3. Verificar que se aplicó correctamente
SELECT 
    nombre,
    permisos::jsonb -> 'admin' as admin_permisos,
    permisos
FROM app.rol 
WHERE LOWER(nombre) LIKE '%administrador%';

-- 4. Script alternativo: Si el JSON no existe o está mal formado, recrearlo completo
-- (Descomenta si es necesario)
/*
UPDATE app.rol 
SET permisos = '{
  "admin": {"read": true, "access": true},
  "sucursales": {"create": true, "read": true, "update": true, "delete": false},
  "empleados": {"create": true, "read": true, "update": true, "delete": false, "view_salary": true},
  "puestos": {"create": true, "read": true, "update": true, "delete": false},
  "proveedores": {"create": true, "read": true, "update": true, "delete": false},
  "servicios": {"create": true, "read": true, "update": true, "delete": false, "assign": true},
  "productos": {"create": true, "read": true, "update": true, "delete": false, "assign": true, "manage_inventory": true},
  "usuarios": {"create": true, "read": true, "update": true, "delete": false, "manage_roles": false},
  "roles": {"create": false, "read": true, "update": false, "delete": false},
  "reportes": {"access": true, "export": true, "financial": true},
  "configuracion": {"access": false, "backup": false, "system_settings": false}
}'::jsonb
WHERE LOWER(nombre) LIKE '%administrador%';
*/

-- 5. Verificación final
SELECT 
    u.email,
    r.nombre as rol_name,
    r.permisos::jsonb -> 'admin' as admin_permisos,
    CASE 
        WHEN (r.permisos::jsonb -> 'admin' -> 'read')::boolean = true 
        THEN 'SÍ TIENE admin.read'
        ELSE 'NO TIENE admin.read'
    END as resultado
FROM app.usuario u
JOIN app.rol r ON u.rol_id = r.id
WHERE u.email = 'test@chikicut.com';