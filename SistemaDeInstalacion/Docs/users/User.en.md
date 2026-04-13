# User

This guide is intended for the end user of the system. Its purpose is to explain, in a clear and structured way, how to register, sign in, recover access, review assigned installers, and run the available applications.

## Purpose of this guide

With this guide, the user will be able to:

- register correctly in the system;
- sign in with valid credentials;
- recover access if the password is forgotten;
- understand what appears on the main screen;
- open the applications assigned to the account;
- identify what to review when something is missing or fails.

## What the application does for the user

The application centralizes in a single interface the installers and executables that each user is authorized to use. This avoids searching through multiple folders, reduces operating mistakes, and helps each person see only what is actually available for the role.

## Before you start

Before signing in, confirm the following:

- your user or email is already registered in the system;
- you have an active password;
- an administrator already assigned the applications you need to see;
- you are entering through the standard login and not through the administrative access.

## General usage flow

```text
Open the application
        |
        +--> Register for the first time
        |
        \--> Sign in
                |
                +--> Successful login -> assigned installer catalog
                |
                \--> Failed login -> specific validation message
```

### Step 1. Open the application

Open the desktop shortcut of the system or the location defined by support. When the application starts, the standard access screen for users is displayed.

![Standard login start screen](image.png)

## User registration

Standard registration allows the user to create an operational account for daily usage of the system.

### Step 1. Open the registration option

From the standard login screen, click the `Register` option.

![Register option from standard login](image-2.png)

### Step 2. Complete the registration data

In the registration form, enter:

- corporate email;
- password.

The system will internally complete the base information according to the current project rules and will validate that the email belongs to the allowed domain.

### Step 3. Save the registration

Click the registration button and wait for confirmation.

If the email already exists, the system will inform you. If registration is successful, it returns to the login screen with the data ready to sign in.

![User registration confirmation](image-1.png)

## Step by step to sign in

### Step 2. Enter user or email

In the user field you can type:

- your registered user;
- or your registered corporate email.

You must enter it exactly as it was created in the system to avoid validation errors.

### Step 3. Enter the password

Type your password in the corresponding field. If you make a typing mistake, the system will not allow access.

If your keyboard has Caps Lock enabled, verify it before continuing.

### Step 4. Use the Remember me option if needed

If you want the system to remember your data on that computer, check the `Remember me` option.

Use this option only when the computer is personal or controlled. It is not recommended on shared machines.

### Step 5. Click Sign in

Click the `Sign in` button. If the data is correct, the system opens the main view with the installers assigned to your account.

![Main screen after successful login](image-13.png)

## What happens when login fails

If the system does not allow you to enter, one of these situations may be happening:

- the user or email does not exist in the database;
- the password does not match;
- you are typing an email different from the registered one;
- the account has not yet been created.

The system now distinguishes when the incorrect data is the user or email and when the incorrect data is the password.

![Failed login validation](image-6.png)

## Password recovery

If you forgot the password, use the recovery route available on the access screen.

### Step 1. Open the recovery option

From the login screen, enter the access recovery option.

![Access to password recovery](image-7.png)

### Step 2. Enter the registered email

Enter the corporate email with which the account was created. The system validates whether that email exists.

### Step 3. Validate the recovery process

Follow the instructions shown on the screen. The system may ask for previous validations before allowing the password change.

![Recovery validation screen](image-8.png)

### Step 4. Enter the new password

When the system allows it, type the new password and confirm the change.

![New password registration](image-12.png)

## What happens when login is correct

## User main view

After signing in, the user accesses the installer view.

On this screen the user normally sees:

- the side panel with the display name;
- the `Installers` option;
- the `Help` option;
- the `Settings` option to adjust the visual theme of the application;
- the `Sign out` button;
- cards with the applications assigned in both the local plant development folder and the global development folder.

![User main view](image-10.png)

## How to read an installer card

Each card can visually show:

- the application name;
- the path or executable reference;
- the program icon;
- the `View` button;
- the `Install` or execute button, depending on the system configuration.

This allows you to quickly identify which application you are using and confirm whether it matches your process.

![Installer card reading](image-11.png)

## How to review the details of an application

If you want to see more information before opening it:

1. locate the application card;
2. click `View`;
3. review the name, description, and visible executable information.

![Application detail view](image-5.png)

## How to run an assigned application

To open an application:

1. locate the corresponding card;

![Application location inside the card](image-15.png)

2. click the `Install` button available in the card;

![Install button of the application](image-17.png)

3. wait until Windows opens the execution dialog and click `Run` to continue;

![Windows execution confirmation](image-18.png)

4. wait a few minutes while the installation or update window loads; then verify everything and continue by clicking `Install` or `Update`;

![Installation or update window](image-19.png)

5. wait while the installation completes and, at the end, the system shows a window confirming that the installation finished successfully;

![Installation finished successfully](image-20.png)

6. the application will open automatically at the end. If the file is correctly registered and the path exists, the system opens the application without additional steps.

![Automatic application launch](image-21.png)

## How to use the Help tab

The `Help` tab allows the user to review the internal documentation of the system without leaving the application. This view is useful to understand procedures, review functional flows, and find support information when needed.

Inside `Help`, the user can normally:

- review documentation folders;
- open functional guides;
- read step-by-step instructions;
- find support data visible on the screen.

### Step 1. Open the Help tab

From the side panel, click the `Help` option.

![Access to the Help tab](image-22.png)

### Step 2. Review available folders and documents

In the help center, the folders and documents are displayed for consultation.

![Folders and documents in the help center](image-24.png)

### Step 3. Open and read a document

Select the document you need and review its content in the main panel.

![Open document in Help](image-25.png)

## Settings

The `Settings` option allows the user to adjust the visual preferences of the application, especially the theme available for the work environment.

From this screen, the user can review the active theme and change the appearance when another visual mode is needed inside the application.

### Step 1. Open Settings

From the side panel, click the `Settings` option.

### Step 2. Review the current visual state

This view shows the active theme of the application and how the interface responds to the computer visual environment.

The application can start by respecting the visual preference of the PC. From `Settings`, the user can keep that behavior or manually change the available theme according to the need of the moment.

### Step 3. Switch between light and dark theme

To change the theme:

1. open `Settings`;
2. locate the appearance or theme option;
3. choose `Light` or `Dark`;
4. confirm the change if the interface asks for it.

![Theme selection in settings](image-28.png)

When the change is applied, the visual interface of the application updates and the integrated documentation presentation also changes to maintain a consistent experience.

![Theme applied in the application](image-29.png)

## How to sign out

When you finish working:

1. go to the side panel;
2. click `Sign out`;
3. the system closes the current session and returns to the access screen.

This is especially important if the computer is shared.

![User sign-out](image-26.png)

## What an end user cannot do

An end user cannot:

- register new installers;
- edit installers;
- delete installers;
- enter User Management;
- assign applications to other people;
- open the logs module;
- use administrative permissions from the standard login.

## Common issues and what to review

### I cannot see my installers

Review the following:

1. that your account is correctly created;
2. that the administrator assigned applications to you;
3. that the executable still exists in the registered path.

### The system does not let me sign in

Verify:

1. that the user or email is correctly written;
2. that the password is correct;
3. that you are not using an unregistered account;
4. that you are not trying to enter through a different route.

### The application does not open

This can happen because of:

- a missing path;
- a file moved or deleted;
- Windows permissions over the path;
- operational changes not yet updated by administration.

## Usage recommendations

- always use the correct corporate account;
- do not share your password;
- sign out when finished;
- inform support if a registered path no longer opens;
- avoid creating duplicate accounts when the real problem is the password.

## Contact

If the problem is not solved with this guide, use the support channel visible inside the help center of the system.

![Support and contact information](image-27.png)
