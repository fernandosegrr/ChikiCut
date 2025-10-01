-- =============================================
-- SCRIPT: Actualizar permisos de roles existentes
-- Descripción: Agrega permisos de servicios y productos a roles creados antes de esta funcionalidad
-- Autor: Sistema ChikiCut
-- Fecha: 2025-01-16
-- =============================================

-- 1. VERIFICAR ROLES EXISTENTES
SELECT 'ROLES EXISTENTES ANTES DE LA ACTUALIZACIÓN' as verificacion;

SELECT 
    id,
    nombre,
    descripcion,
    is_active,
    created_at
FROM app.rol 
WHERE is_active = true
ORDER BY nombre;

-- 2. MOSTRAR PERMISOS ACTUALES (ANTES)
SELECT 'PERMISOS ACTUALES DE CADA ROL (ANTES)' as verificacion;

SELECT 
    r.nombre,
    r.permisos
FROM app.rol r
WHERE r.is_active = true
ORDER BY r.nombre;

-- 3. FUNCIÓN PARA AGREGAR MÓDULOS FALTANTES A PERMISOS JSON
CREATE OR REPLACE FUNCTION app.agregar_modulos_faltantes(
    permisos_existentes TEXT,
    es_admin_total BOOLEAN DEFAULT FALSE
) RETURNS TEXT AS $$
DECLARE
    permisos_json JSONB;
    resultado_json JSONB;
BEGIN
    -- Parsear JSON existente o crear vacío si es nulo/inválido
    BEGIN
        permisos_json := COALESCE(permisos_existentes::JSONB, '{}'::JSONB);
    EXCEPTION WHEN OTHERS THEN
        permisos_json := '{}'::JSONB;
    END;
    
    resultado_json := permisos_json;
    
    -- Agregar módulo SERVICIOS si no existe
    IF NOT resultado_json ? 'servicios' THEN
        IF es_admin_total THEN
            resultado_json := resultado_json || '{"servicios": {"Create": true, "Read": true, "Update": true, "Delete": true}}'::JSONB;
        ELSE
            resultado_json := resultado_json || '{"servicios": {"Create": false, "Read": true, "Update": false, "Delete": false}}'::JSONB;
        END IF;
    END IF;
    
    -- Agregar módulo PRODUCTOS si no existe
    IF NOT resultado_json ? 'productos' THEN
        IF es_admin_total THEN
            resultado_json := resultado_json || '{"productos": {"Create": true, "Read": true, "Update": true, "Delete": true}}'::JSONB;
        ELSE
            resultado_json := resultado_json || '{"productos": {"Create": false, "Read": true, "Update": false, "Delete": false}}'::JSONB;
        END IF;
    END IF;
    
    RETURN resultado_json::TEXT;
END;
$$ LANGUAGE plpgsql;

-- 4. ACTUALIZAR ROLES EXISTENTES CON LÓGICA INTELIGENTE

-- Actualizar roles que parecen ser administrativos (tienen muchos permisos de create/update/delete)
UPDATE app.rol 
SET 
    permisos = app.agregar_modulos_faltantes(permisos, TRUE),
    updated_at = NOW()
WHERE is_active = true
  AND (
    -- Roles que tienen permisos administrativos en múltiples módulos
    (permisos::JSONB -> 'usuarios' ->> 'Create')::BOOLEAN = true
    OR (permisos::JSONB -> 'empleados' ->> 'Create')::BOOLEAN = true
    OR (permisos::JSONB -> 'sucursales' ->> 'Create')::BOOLEAN = true
    OR LOWER(nombre) LIKE '%admin%'
    OR LOWER(nombre) LIKE '%gerente%'
    OR LOWER(nombre) LIKE '%super%'
  );

-- Actualizar roles que parecen ser de consulta/operativos (solo tienen read en la mayoría)
UPDATE app.rol 
SET 
    permisos = app.agregar_modulos_faltantes(permisos, FALSE),
    updated_at = NOW()
WHERE is_active = true
  AND NOT (
    -- Excluir los roles que ya se actualizaron como administrativos
    (permisos::JSONB -> 'usuarios' ->> 'Create')::BOOLEAN = true
    OR (permisos::JSONB -> 'empleados' ->> 'Create')::BOOLEAN = true
    OR (permisos::JSONB -> 'sucursales' ->> 'Create')::BOOLEAN = true
    OR LOWER(nombre) LIKE '%admin%'
    OR LOWER(nombre) LIKE '%gerente%'
    OR LOWER(nombre) LIKE '%super%'
  );

-- 5. VERIFICAR RESULTADOS DESPUÉS DE LA ACTUALIZACIÓN
SELECT 'PERMISOS DESPUÉS DE LA ACTUALIZACIÓN' as verificacion;

SELECT 
    r.nombre,
    r.permisos,
    CASE 
        WHEN r.permisos::JSONB ? 'servicios' THEN 'SÍ' 
        ELSE 'NO' 
    END as tiene_servicios,
    CASE 
        WHEN r.permisos::JSONB ? 'productos' THEN 'SÍ' 
        ELSE 'NO' 
    END as tiene_productos,
    CASE 
        WHEN (r.permisos::JSONB -> 'servicios' ->> 'Create')::BOOLEAN = true THEN 'Admin' 
        WHEN (r.permisos::JSONB -> 'servicios' ->> 'Read')::BOOLEAN = true THEN 'Lectura' 
        ELSE 'Sin acceso' 
    END as nivel_servicios,
    CASE 
        WHEN (r.permisos::JSONB -> 'productos' ->> 'Create')::BOOLEAN = true THEN 'Admin' 
        WHEN (r.permisos::JSONB -> 'productos' ->> 'Read')::BOOLEAN = true THEN 'Lectura' 
        ELSE 'Sin acceso' 
    END as nivel_productos
FROM app.rol r
WHERE r.is_active = true
ORDER BY r.nombre;

-- 6. ACTUALIZACIÓN ESPECÍFICA PARA ROLES CONOCIDOS (OPCIONAL)
-- Si tienes roles específicos que quieres configurar manualmente:

-- Ejemplo: Asegurar que rol "Super Administrador" tenga todos los permisos
UPDATE app.rol 
SET permisos = '{
    "sucursales": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "empleados": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "puestos": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "proveedores": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "servicios": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "productos": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "conceptosgasto": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "usuarios": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "roles": {"Create": true, "Read": true, "Update": true, "Delete": true},
    "reportes": {"Create": true, "Read": true, "Update": true, "Delete": true}
}'::TEXT,
updated_at = NOW()
WHERE is_active = true 
  AND (LOWER(nombre) LIKE '%super%' OR LOWER(nombre) LIKE '%administrador%')
  AND LOWER(nombre) NOT LIKE '%empresa%'; -- Excluir "Administrador de Empresa"

-- Ejemplo: Asegurar que "Administrador de Empresa" tenga casi todos los permisos
UPDATE app.rol 
SET permisos = '{
    "sucursales": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "empleados": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "puestos": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "proveedores": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "servicios": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "productos": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "conceptosgasto": {"Create": true, "Read": true, "Update": false, "Delete": true},
    "usuarios": {"Create": true, "Read": true, "Update": true, "Delete": false},
    "roles": {"Create": false, "Read": true, "Update": false, "Delete": false},
    "reportes": {"Create": true, "Read": true, "Update": true, "Delete": true}
}'::TEXT,
updated_at = NOW()
WHERE is_active = true 
  AND LOWER(nombre) LIKE '%administrador%' 
  AND LOWER(nombre) LIKE '%empresa%';

-- 7. RESUMEN FINAL
SELECT 'RESUMEN DE LA ACTUALIZACIÓN' as resultado;

SELECT 
    COUNT(*) as total_roles_activos,
    COUNT(CASE WHEN permisos::JSONB ? 'servicios' THEN 1 END) as roles_con_servicios,
    COUNT(CASE WHEN permisos::JSONB ? 'productos' THEN 1 END) as roles_con_productos,
    COUNT(CASE WHEN (permisos::JSONB -> 'servicios' ->> 'Create')::BOOLEAN = true THEN 1 END) as roles_admin_servicios,
    COUNT(CASE WHEN (permisos::JSONB -> 'productos' ->> 'Create')::BOOLEAN = true THEN 1 END) as roles_admin_productos
FROM app.rol
WHERE is_active = true;

-- Limpiar función temporal
DROP FUNCTION IF EXISTS app.agregar_modulos_faltantes(TEXT, BOOLEAN);

SELECT '=== ACTUALIZACIÓN DE PERMISOS COMPLETADA ===' as resultado;

-- INSTRUCCIONES ADICIONALES:
SELECT 'INSTRUCCIONES POST-ACTUALIZACIÓN' as info;
SELECT 'Los usuarios necesitarán cerrar sesión y volver a iniciar para que los nuevos permisos tomen efecto.' as instruccion_1;
SELECT 'Verifica que tu usuario actual tenga un rol con permisos de servicios y productos.' as instruccion_2;
SELECT 'Si necesitas ajustar permisos específicos, usa la interfaz web de gestión de roles.' as instruccion_3;