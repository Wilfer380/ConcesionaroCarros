# Documentacion del Proyecto

`SistemaDeInstalacion` es una aplicacion de escritorio orientada a la administracion de instaladores corporativos y al control de acceso de usuarios por rol.

Este directorio centraliza la documentacion funcional y tecnica del sistema actual. Todo el contenido describe exclusivamente el producto de instaladores empresariales que esta activo hoy.

## Estructura

- `users/README.md`
  Manual funcional para usuarios finales y administradores operativos.
- `Developers/README.md`
  Guia tecnica general para desarrolladores y personal de soporte.
- `Developers/BaseDeDatos.md`
  Descripcion de la base SQLite, tablas activas, migraciones y consideraciones de mantenimiento.

## Alcance del sistema

La aplicacion fue implementada para resolver estas necesidades:

- centralizar instaladores corporativos en un unico punto de acceso
- controlar que aplicativos puede ver o ejecutar cada usuario
- permitir que administradores creen usuarios y asignen aplicativos por persona
- separar el acceso administrativo del acceso normal
- mantener registro local de usuarios, administradores, instaladores y recuperaciones de contrasena

## Modulos principales

- autenticacion de usuarios normales
- autenticacion de administradores
- registro de usuarios normales
- registro de administradores
- recuperacion local de contrasena
- modulo de instaladores
- gestion de usuarios
- asignacion de aplicativos por usuario

## Archivos recomendados para comenzar

Si eres usuario funcional:

- abre `users/README.md`

Si eres desarrollador o personal de soporte tecnico:

- abre `Developers/README.md`
- luego revisa `Developers/BaseDeDatos.md`

## Notas clave

- La base de datos activa del sistema es `WegInstaladores.db`.
- Si existen bases heredadas, el sistema intenta migrarlas automaticamente al nombre actual.
- La solucion activa del proyecto es `SistemaDeInstalacion.sln`.
- Existen namespaces internos heredados que aun usan `ConcesionaroCarros`; esto no impide operar la aplicacion, pero debe tenerse en cuenta para futuras refactorizaciones.
