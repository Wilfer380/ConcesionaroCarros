# Documentación del Proyecto

`SistemaDeInstalacion` es una aplicación de escritorio orientada a la administración de instaladores corporativos y al control de acceso de usuarios por rol.

Este directorio centraliza la documentación funcional y técnica del sistema actual. Todo el contenido describe exclusivamente el producto de instaladores empresariales que está activo hoy.

## Estructura

- `users/README.md`
  Manual funcional para usuarios finales y administradores operativos.
- `Developers/README.md`
  Guía técnica general para desarrolladores y personal de soporte.
- `Developers/BaseDeDatos.md`
  Descripción de la base SQLite, tablas activas, migraciones y consideraciones de mantenimiento.

## Alcance del sistema

La aplicación fue implementada para resolver estas necesidades:

- centralizar instaladores corporativos en un único punto de acceso
- controlar qué aplicativos puede ver o ejecutar cada usuario
- permitir que administradores creen usuarios y asignen aplicativos por persona
- separar el acceso administrativo del acceso a personas, es decir, usuarios con rol diferente a administradores
- mantener registro local de usuarios, administradores, instaladores y recuperaciones de contraseñas

## Módulos principales

- autenticación de usuarios normales, diferente a rol administrador
- autenticación de administradores
- registro de usuarios normales
- registro de administradores
- recuperación local de contraseñas
- módulo de instaladores
- gestión de usuarios
- asignación de aplicativos por usuario
- pruebas unitarias del núcleo funcional desde Visual Studio

## Archivos recomendados para comenzar

Si eres usuario funcional:

- abre `users/README.md`

Si eres desarrollador o personal de soporte técnico:

- abre `Developers/README.md`
- luego revisa `Developers/BaseDeDatos.md`

## Notas clave

- La base de datos activa del sistema es `WegInstaladores.db`.
- Si existen bases heredadas, el sistema intenta migrarlas automáticamente al nombre actual.
- La solución activa del proyecto es `SistemaDeInstalacion.sln`.
- Las pruebas del sistema están implementadas como pruebas unitarias `MSTest` y se ejecutan desde `Prueba` → `Explorador de pruebas` en Visual Studio.

