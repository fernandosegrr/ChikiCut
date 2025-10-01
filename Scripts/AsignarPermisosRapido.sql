-- =============================================
-- SCRIPT RÁPIDO: Asignar permisos de servicios y productos a rol específico
-- =============================================

-- 1. VER ROLES DISPONIBLES
SELECT 
    id,
    nombre,
    descripcion,
    is_active
FROM app.rol 
WHERE is_active = true
ORDER BY nombre;

-- 2. ACTUALIZAR ROL ESPECÍFICO CON TODOS LOS PERMISOS
-- IMPORTANTE: Cambia 'Super Administrador' por el nombre exacto de tu rol

UPDATE app.rol 
SET permisos = '{
    "usuarios": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "empleados": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "sucursales": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "puestos": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "proveedores": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "servicios": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "productos": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "conceptosgasto": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "roles": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "reportes": {"Create": true, "Read": true, "Update": true, "Delete": true}
}'::TEXT,
updated_at = NOW()
WHERE nombre = 'Super Administrador' 
  AND is_active = true;

-- 3. VERIFICAR QUE SE APLICÓ CORRECTAMENTE
SELECT 
    nombre,
    (permisos::JSONB -> 'servicios' ->> 'Read')::BOOLEAN as servicios_ver,
    (permisos::JSONB -> 'servicios' ->> 'Create')::BOOLEAN as servicios_crear,
    (permisos::JSONB -> 'productos' ->> 'Read')::BOOLEAN as productos_ver,
    (permisos::JSONB -> 'productos' ->> 'Create')::BOOLEAN as productos_crear,
    updated_at
FROM app.rol 
WHERE nombre = 'Super Administrador' 
  AND is_active = true;

-- 4. CONTAR PERMISOS TOTALES
SELECT 
    nombre,
    (
        (CASE WHEN (permisos::JSONB -> 'usuarios' ->> 'Read')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'usuarios' ->> 'Create')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'usuarios' ->> 'Update')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'usuarios' ->> 'Delete')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'empleados' ->> 'Read')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'empleados' ->> 'Create')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'empleados' ->> 'Update')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'empleados' ->> 'Delete')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'sucursales' ->> 'Read')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'sucursales' ->> 'Create')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'sucursales' ->> 'Update')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'sucursales' ->> 'Delete')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'servicios' ->> 'Read')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'servicios' ->> 'Create')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'servicios' ->> 'Update')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'servicios' ->> 'Delete')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'productos' ->> 'Read')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'productos' ->> 'Create')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'productos' ->> 'Update')::BOOLEAN THEN 1 ELSE 0 END) +
        (CASE WHEN (permisos::JSONB -> 'productos' ->> 'Delete')::BOOLEAN THEN 1 ELSE 0 END)
    ) as total_permisos_activos
FROM app.rol 
WHERE nombre = 'Super Administrador' 
  AND is_active = true;

-- 5. TAMBIÉN ACTUALIZAR ADMINISTRADOR Si EXISTE
UPDATE app.rol 
SET permisos = '{
    "usuarios": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "empleados": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "sucursales": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "puestos": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "proveedores": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "servicios": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "productos": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "conceptosgasto": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "roles": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "reportes": {"Create": true, "Read": true, "Update": true, "Delete": true}
}'::TEXT,
updated_at = NOW()
WHERE LOWER(nombre) LIKE '%administrador%' 
  AND is_active = true;

SELECT '=== PERMISOS ACTUALIZADOS CORRECTAMENTE ===' as resultado;