-- Script de diagnóstico de permisos
-- Ejecutar en PostgreSQL para verificar permisos del usuario

-- 1. Verificar que existe el permiso 'admin.read'
SELECT 
    'PERMISO admin.read' as tipo,
    id, 
    module, 
    action, 
    description,
    created_at
FROM permisos 
WHERE module = 'admin' AND action = 'read';

-- 2. Verificar el usuario y su rol
SELECT 
    'USUARIO test@chikicut.com' as tipo,
    u.id as usuario_id,
    u.email,
    u.is_active,
    r.id as rol_id,
    r.name as rol_name,
    r.level,
    ur.created_at as asignado_en
FROM usuarios u
JOIN usuario_roles ur ON u.id = ur.usuario_id
JOIN roles r ON ur.rol_id = r.id
WHERE u.email = 'test@chikicut.com';

-- 3. Verificar permisos del rol
SELECT 
    'PERMISOS DEL ROL' as tipo,
    r.name as rol_name,
    p.module,
    p.action,
    p.description,
    rp.created_at as asignado_en
FROM roles r
JOIN rol_permisos rp ON r.id = rp.rol_id
JOIN permisos p ON rp.permiso_id = p.id
JOIN usuario_roles ur ON r.id = ur.rol_id
JOIN usuarios u ON ur.usuario_id = u.id
WHERE u.email = 'test@chikicut.com'
ORDER BY p.module, p.action;

-- 4. BUSCAR específicamente el permiso 'admin.read'
SELECT 
    'VERIFICACIÓN ADMIN.READ' as tipo,
    u.email,
    r.name as rol_name,
    p.module,
    p.action,
    CASE 
        WHEN p.id IS NOT NULL THEN 'SÍ TIENE PERMISO'
        ELSE 'NO TIENE PERMISO'
    END as tiene_permiso
FROM usuarios u
JOIN usuario_roles ur ON u.id = ur.usuario_id
JOIN roles r ON ur.rol_id = r.id
LEFT JOIN rol_permisos rp ON r.id = rp.rol_id
LEFT JOIN permisos p ON rp.permiso_id = p.id AND p.module = 'admin' AND p.action = 'read'
WHERE u.email = 'test@chikicut.com';

-- 5. Si NO existe el permiso, crearlo
INSERT INTO permisos (module, action, description, created_at) 
VALUES ('admin', 'read', 'Acceso al panel administrativo', NOW())
ON CONFLICT (module, action) DO NOTHING;

-- 6. Asignar el permiso al rol del usuario (si no lo tiene)
DO $$
DECLARE
    user_role_id INTEGER;
    admin_permission_id INTEGER;
BEGIN
    -- Obtener el rol del usuario
    SELECT r.id INTO user_role_id
    FROM usuarios u
    JOIN usuario_roles ur ON u.id = ur.usuario_id
    JOIN roles r ON ur.rol_id = r.id
    WHERE u.email = 'test@chikicut.com'
    LIMIT 1;
    
    -- Obtener el permiso admin.read
    SELECT id INTO admin_permission_id
    FROM permisos
    WHERE module = 'admin' AND action = 'read';
    
    -- Asignar si ambos existen
    IF user_role_id IS NOT NULL AND admin_permission_id IS NOT NULL THEN
        INSERT INTO rol_permisos (rol_id, permiso_id, created_at)
        VALUES (user_role_id, admin_permission_id, NOW())
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        
        RAISE NOTICE 'Permiso admin.read asignado al rol del usuario (ID: %)', user_role_id;
    ELSE
        RAISE NOTICE 'No se pudo asignar el permiso. Usuario: %, Permiso: %', user_role_id, admin_permission_id;
    END IF;
END $$;

-- 7. Verificación final
SELECT 
    'VERIFICACIÓN FINAL' as tipo,
    u.email,
    r.name as rol_name,
    COUNT(p.id) as total_permisos,
    CASE 
        WHEN COUNT(CASE WHEN p.module = 'admin' AND p.action = 'read' THEN 1 END) > 0 
        THEN 'SÍ TIENE admin.read'
        ELSE 'NO TIENE admin.read'
    END as tiene_admin_read
FROM usuarios u
JOIN usuario_roles ur ON u.id = ur.usuario_id
JOIN roles r ON ur.rol_id = r.id
LEFT JOIN rol_permisos rp ON r.id = rp.rol_id
LEFT JOIN permisos p ON rp.permiso_id = p.id
WHERE u.email = 'test@chikicut.com'
GROUP BY u.email, r.name;