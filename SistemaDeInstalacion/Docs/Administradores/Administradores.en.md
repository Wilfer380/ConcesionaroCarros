# Administrators

This guide explains the functional journey of the administrator inside the system. Unlike the end user, the administrator can enter through two different routes:

- through the standard login, when the goal is only to operate as a standard user;
- through the administrative login, when the goal is to use management functions.

The purpose of this guide is to clearly separate both routes from the start, showing what the administrator shares with the normal user and what belongs only to administrative mode.

## Main explanation

The administrator does not always work in administrative mode. The administrator can also open the application, register, sign in, and operate as a normal user if entering through the standard route.

That means there are two different scenarios:

1. **Administrator through standard login**  
   The administrator enters like any end user. In this mode, the administrator can see assigned applications, review help, and sign out, but does not access management modules.

2. **Administrator through administrative login**  
   The administrator enters through the special administration route. In this mode, modules such as `User Management`, installer administration, application assignment, and, when allowed, the logs module are enabled.

## Objective of this guide

With this guide, the administrator will be able to:

- understand when to use the standard login and when to use the admin login;
- correctly follow the process from the desktop shortcut;
- register if an admin account does not yet exist;
- sign in through the correct route according to the task;
- manage installers;
- manage users;
- assign applications;
- review help and support;
- validate functional changes.

## What the administrator should understand first

Before the visual walkthrough, the administrator must keep these rules in mind:

1. access always starts from the same desktop shortcut;
2. entering through the standard login does not activate administrative permissions, even if the person has an administrative role in the system;
3. if the administrative account does not exist yet, registration must first be completed through the administrative route;
4. only after a successful admin login are management modules enabled;
5. every administrative change affects other users and must be reviewed carefully.

## General administrative flow

```text
Desktop shortcut
        |
        v
Initial screen
        |
        +--> Standard login
        |        |
        |        v
        |   Standard user operation
        |
        +--> Administrative registration
        |        |
        |        v
        |   Return to admin login
        |
        \--> Administrative login
                 |
                 v
        Admin main view
                 |
                 +--> Installers
                 +--> User Management
                 +--> Help
                 \--> Logs according to permissions
```

## Step 1. Open the application from the desktop shortcut

The administrator starts from the same main shortcut of the system. From that screen the administrator decides whether to continue through the standard user route or through the administrative route.

![Shortcut access for administrators](image-1.png)

## Step 2. Difference between standard login and admin login

This is one of the most important rules of the system:

- the standard login is for standard operation, where users enter as normal operational users, even if the same person also has an administrative role;
- the administrative login activates permissions that are not available in the standard user route;
- a user with an administrative role does not obtain full access when entering through the standard login;
- to see the admin main view and all management functions, the person must enter through the admin route.

## Step 2.1. Access through standard user login

If the administrator enters through the normal window, the access, registration, recovery, and sign-in flow is the same as the user guide.

### Result of standard login for an administrator

After entering through the standard login:

- the administrator sees the same general view as the standard user;
- management modules are not visible;
- the logs module is not visible;
- the experience is equivalent to the one described in the `User` guide.

[Open the user guide from the shortcut](help://users/user#step-1-open-the-application)

## Route 2. Administrator operating through administrative login

This is the route the administrator must use when functional changes need to be made inside the system. To continue with the process, the person must first enter the admin login and then continue with registration or sign-in if needed.

## Step 3. Administrative registration

If the administrative account does not exist yet, it must first be created from the admin route using the registration option.

### Step 3.1. Open the Register option in admin login

From the administrative login screen, click the `Register` option.

![Admin registration access](image-4.png)

### Step 3.2. Complete the administrative registration form

In the administrative registration you must complete:

- corporate email;
- administrative role;
- standard password;
- administrative password.

This registration is not the same as the normal one. Here the system creates or updates the base user and also registers the administrative data needed to operate in admin mode.

### Step 3.3. Save the administrative registration

Click the registration button and wait for confirmation.

If the process is successful:

- the base user is created or updated;
- the administrative account is registered;
- the system returns to the admin login with the data ready to sign in.

![Administrative registration confirmation](image-5.png)

## Step 4. Sign in through administrative login

Once the shortcut is open and, if necessary, registration is complete, the next step is to sign in through the admin login.

### Step 4.1. Open the administrative login

From the initial screen, enter the administrative access option.

### Step 4.2. Enter the administrative user

In this login you must use the `UsuarioSistema` or administrative identifier defined for the account.

### Step 4.3. Enter the administrative password

Enter the corresponding administrative password. This password is not validated in the same way as the standard login password.

### Step 4.4. Confirm access

Click the access button. If everything is correct, the system opens the main view with administrator mode enabled.

![Successful administrative login](image-6.png)

## Step 5. Administrative login validations

The system now differentiates the admin login errors more clearly.

### When the administrative user does not exist

The system indicates that the administrative user is not registered.

### When the administrative password is incorrect

The system indicates that the administrative password is incorrect.

![Administrative login validation errors](image-7.png)

## Step 6. Administrator main view

When the admin login is successful, the application enters administrator mode.

Normally the administrator sees:

- side panel;
- `Installers` module;
- `User Management` module;
- `Help` option;
- `Settings` option to review or change the visual theme;
- `Logs` option only if the profile passes the special support validation;
- `Sign out` option.

![Administrator main view](image-8.png)

## Step 7. Installer management

From this module, the catalog of executables is controlled.

[Open the user guide from "What happens when login is correct"](help://users/user#what-happens-when-login-is-correct)

### How to add an installer

1. enter the `Search Installers` module;
2. open the new installer form;
3. select the executable;
4. complete name, description, and category;
5. save the record.

![Installers module](image-9.png)

![New installer form](image-10.png)

![Installer save confirmation](image-10.png)

### How to edit or delete an installer

To edit:

1. locate the correct card;
2. click the pencil icon, which is `Edit`;
3. update the information;
4. save the changes.

To delete:

1. locate the correct card again;
2. click the trash icon, which is `Delete`;
3. confirm the action and remove the record completely.

![Edit or delete installer](image-11.png)

### How to install a registered application

When the installer is already registered, the administrator can also test the installation process from the corresponding card.

1. locate the card of the application to install;
2. click the `Install` button;
3. wait until Windows opens the execution or installation window;
4. confirm execution if Windows requests it;
5. continue the installation wizard until it finishes;
6. validate that the application opens correctly at the end.

[Open the installation process in the user guide](help://users/user#how-to-run-an-assigned-application)

## Step 8. User management

This module is only available in administrator mode. From here, accounts and permissions are managed.

![User management module](image-12.png)

### How to add a user

1. open `User Management`;
2. click the option to add a user;
3. complete first names, last names, email, phone, password, and role;
4. save the information.

![New user form](image-13.png)

### How to edit or delete a user

To edit:

1. locate the record;
2. click `Edit`;
3. update the information;
4. save.

To delete:

1. locate the record;
2. click `Delete`;
3. confirm the action.

![Edit or delete user](image-14.png)

## Step 9. Application assignment

Inside `User Management`, the administrator can assign or remove applications from a user.

The general flow is:

1. select the user;
2. open the assignment panel;
3. check or uncheck applications;
4. save the assignment.

![Application assignment panel](image-15.png)

## Step 10. Using the Help tab

The `Help` tab allows the administrator to review functional documentation inside the system without leaving the application.

From this view, the administrator can normally:

- navigate through documentation folders;
- open administrative and functional guides;
- review complementary information;
- locate the available support channel.

### Step 10.1. Open the Help tab

From the side panel, click the `Help` option.

![Access to admin help](image-16.png)

### Step 10.2. Review the folders and available documents

In the help view, the administrator sees the folders and documents available for the profile.

### Step 10.3. Open a document and review its content

Select the required document and review the information in the main panel.

![Administrator help content](image-18.png)

## Step 11. Settings

The `Settings` screen allows the administrator to adjust the visual preferences of the application without leaving the system.

The canonical functional reference of this screen remains in [User, Settings](help://users/user#settings), because the walkthrough and theme switching are shared between profiles.

## Step 12. Sign out

When the administrator finishes:

1. return to the side panel;
2. click `Sign out`;
3. confirm sign-out if required;
4. leave the system on the initial screen.

## What to review after an important change

After adding, editing, deleting, or assigning, validate:

- that the user appears correctly;
- that the installer remains visible in the catalog;
- that the user can sign in;
- that the filtered catalog matches the assignment made.

## Frequent support cases

### The user cannot see the assigned application

Review:

1. that the installer exists;
2. that the user has assignments;
3. that the executable path is still valid.

### The executable does not open

Review:

1. that the file exists in the path;
2. that it was not moved;
3. that Windows is not blocking it;
4. that the user has permissions over that location.

### User Management is not visible

Review:

1. that access was made through admin login;
2. that the administrative password was used;
3. that administrator mode is active.

## Good administration practices

- use clear names for installers;
- validate paths before saving;
- avoid duplicate accounts;
- confirm assignments after each change;
- always distinguish standard login from admin login.

## Contact and support

If a functional or technical question appears, use the internal help center of the system and the support channel defined by the organization.
