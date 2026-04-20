# System

`SistemaDeInstalacion` is a desktop application designed to centralize corporate executables, control access by role, and manage which user can view or run each application.

Its purpose is to reduce installer sprawl, organize daily operation, and provide clear routes for end users, administrators, and developers.

## Application purpose

The application exists to solve these needs:

1. centralize authorized executables in a single catalog;
2. control permissions by user and role;
3. separate administrative access from normal operational access;
4. maintain functional and documentary continuity of the system.

## Current functional scope

Today the system covers:

- shortcut access and application startup;
- standard user login;
- administrative login;
- user registration;
- administrator registration;
- password recovery;
- installer catalog;
- user management;
- application assignment;
- internal documentation by profile;
- visual settings and theme switching, with the functional reference centralized in [User, Settings](help://users/user#settings);
- functional and support logs.

## General product map

```text
SistemaDeInstalacion
|
+- Open from desktop shortcut
|  |
|  \- Initial screen
|      |
|      +-> Standard registration
|      +-> Standard login
|      +-> Administrator registration
|      \-> Administrative login
|
+- User operation
|  +- Filtered catalog
|  +- Help
|  \- Run assigned applications
|
\- Administrative operation
   +- Full catalog
   +- User management
   +- Application assignment
   +- Help
   \- Restricted logs
```

## Usage profiles

### User

The end user opens the application from the shortcut, can register if the account does not yet exist, sign in through the standard login, review assigned installers, and run the applications visible for the account.

### Administrators

The administrator opens the application from the same shortcut, but uses the administrative route. From there the administrator can register, sign in with admin access, manage users, manage installers, and assign applications.

### Developer

The developer maintains the WPF application, business logic, persistence, packaging, logs, and technical documentation. This guide is more technical and code-oriented.

## Recommended functional route

For functional operation, the correct order is:

```text
Desktop shortcut
        |
        v
Initial screen
        |
        +--> Registration
        |
        \--> Sign in
                 |
                 +--> End user
                 \--> Administrator
```

This order matters because it avoids starting directly from internal modules without first explaining how a person actually enters the system.

## Settings

The `Settings` option groups the visual preferences of the application, especially the available theme changes.

The full and current functional reference remains in [User, Settings](help://users/user#settings).

## Official documentation structure

The documentation is distributed like this:

| Canonical document | Document | Main usage |
|---|---|---|
| `help://sistema` | `System` | general product overview |
| `help://users/user` | `User` | operational guide for end users |
| `help://administradores/administradores` | `Administrators` | operational guide for administration |
| `help://developers/developer` | `Developer` | technical guide for development continuity |
| `help://developers/base-de-datos` | `Database` | structure and operation of persistence |

## Recommended starting point

If you are going to operate the system:

- start from the desktop shortcut and the initial screen;
- then review registration if you do not yet have an account;
- afterwards review the login route that matches your profile.

According to the role:

- review [User](help://users/user) if your profile is end user;
- review [Administrators](help://administradores/administradores) if your profile is administrative.

If you are going to maintain or evolve the product:

- start with [Developer](help://developers/developer);
- continue with [Database](help://developers/base-de-datos).

## Complementary links

- [User guide](help://users/user)
- [Administrator guide](help://administradores/administradores)
- [Technical guide for developers](help://developers/developer)
- [Database guide](help://developers/base-de-datos)
