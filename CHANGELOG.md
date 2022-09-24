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
