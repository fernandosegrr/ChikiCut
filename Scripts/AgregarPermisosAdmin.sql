-- Script para agregar permisos de administración
-- Ejecutar en PostgreSQL

-- Agregar permiso para el módulo admin si no existe
INSERT INTO permisos (module, action, description, created_at) 
VALUES ('admin', 'read', 'Acceso al panel administrativo', NOW())
ON CONFLICT (module, action) DO NOTHING;

-- Obtener el ID del rol Administrador
DO $$
DECLARE
    admin_role_id INTEGER;
    admin_permission_id INTEGER;
BEGIN
    -- Buscar el rol de Administrador
    SELECT id INTO admin_role_id 
    FROM roles 
    WHERE LOWER(name) LIKE '%administrador%' OR LOWER(name) LIKE '%admin%'
    LIMIT 1;
    
    -- Buscar el permiso admin.read
    SELECT id INTO admin_permission_id
    FROM permisos
    WHERE module = 'admin' AND action = 'read';
    
    -- Solo proceder si encontramos ambos
    IF admin_role_id IS NOT NULL AND admin_permission_id IS NOT NULL THEN
        -- Asignar el permiso al rol si no existe
        INSERT INTO rol_permisos (rol_id, permiso_id, created_at)
        VALUES (admin_role_id, admin_permission_id, NOW())
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        
        RAISE NOTICE 'Permiso admin.read asignado al rol Administrador (ID: %)', admin_role_id;
    ELSE
        RAISE NOTICE 'No se encontró el rol Administrador o el permiso admin.read';
    END IF;
END $$;

-- Verificar que se aplicó correctamente
SELECT 
    r.name as rol_name,
    p.module,
    p.action,
    p.description
FROM roles r
JOIN rol_permisos rp ON r.id = rp.rol_id
JOIN permisos p ON rp.permiso_id = p.id
WHERE p.module = 'admin'
ORDER BY r.name, p.module, p.action;