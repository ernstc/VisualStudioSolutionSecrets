## October 2022 Release (version 2.0.0)

This release enables the use of different kinds of repositories and enables to configure each solution to use a different kind of repository. 

**Azure Key Vault** is the new supported repository. Azure Key Vault is the recommended repository to use for scenarios where the solution secrets can be shared within the development team.

### New Features

* Added **Azure Key Vault** as an alternative repository for storing solution secrets.
* New command: `configure`

  The "configure" command let to specify the default repository for your secrets (GitHub Gists or Azure Key Vault). Alternatively, with this command you can configure single solutions to use the preferred repository. Now you can have some solutions using GitHub Gists and some others using Azure Key Vault. It is possible to use different Azure Key Vault resources for different solutions, so, if you work with different development teams, each team can share their own secrets.

* New command: `configure list`

  This command display the custom repository configuration for the solutions.

* New command: `clear`

  The "clear" command erase the solution secrets from the local machine. It is equivalent to applying the command "dotnet user-secerts clear" for each project in the solution. 

### Changes

* Changed `status` command. It has been enhanced for displaying the default repository and the custom repository configured for each solution. In addition it display the synchronization status between the local and remote secret settings.
* The command `changekey` has been renamed to `change-key`.

---

## September 2022 Release (version 1.2.0)

Added new features and improvements.

### Features

* Added the new command "changekey". This command changes the encryption key locally e re-encrypts the remote secrects with the new key. Use this command if you loose the key or the key is compromised.
* Extended the "status" command for displaying also the synchronization status of the solutions. The status command now has two new parameters: --path and --all.

### Fixes

* Fixed the management of relative paths passed to the --path parameter.
* Fixed issues in the Windows/WSL Linux environment.
* Minor fixes.

---

## April 2022 Release (version 1.1.2)

### Features

* Extended secrets management to ASP.NET MVC 5 projects.
* Added support to projects that share the same secret files.
* Added "status" verb for checking the tool configuration.
* Added check for upgrade to a new version.
* Improved output presentation.

---

## February 2022 Release (version 1.0.2)

Initial release.
Tool for syncing Visual Studio solution secrets across different development machines.

### Features

* Searching for projects that use secrets.
* Pushing encrypted secrets to a remote repository.
* Pulling ecrypted secrets from a remote repository.
* Implemented GitHub Gist as remote secrets repository.color
