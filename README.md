[![.NET](https://github.com/ernstc/VisualStudioSolutionSecrets/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ernstc/VisualStudioSolutionSecrets/actions/workflows/dotnet.yml) ![Nuget](https://img.shields.io/nuget/dt/vs-secrets) ![Nuget](https://img.shields.io/nuget/v/vs-secrets)

# ***Visual Studio Solution Secrets***

Synchronize solution secrets across different development machines.

* [Get Started](#get-started)
* [Best Practices](#best-practices)
* [The Problem](#the-problem)
* [The Solution](#the-solution)
* [How to install](#how-to-install)
* [Configure the encryption key and authorizations](#configure-the-encryption-key-and-authorizations)
* [Repository configuration](#repository-configuration)
* [Push solution secrets](#push-solution-secrets)
* [Pull solution secrets](#pull-solution-secrets)
* [Utility commands](#utility-commands)
* [Configuration files](#configuration-files)

<br/>

# Get Started

If you already know it, here are the quick start commands.

1) Install
```shell
dotnet tool install --global vs-secrets
```
2) Configure
```shell
vs-secrets init -p <your-passphrase>
```
3) Pull secrets
```shell
vs-secrets pull
```
<br/>

# Best Practices

As a good practices in DevOps, you must not store secrets (sensitive data like passwords, connection strings, access keys, etc.) in your source code that is committed in a shared repository and secrets must not be deployed with the apps.

Fortunately Visual Studio and .Net help us in separating secrets from our code with the ***User Secrets Manager*** that let us store secrets out of the solution folder. The User Secrets Manager hides implementation details, but essentially it stores secrets in files located in the machine's user profile folder.

You can find the **User Secrets Manager** documentation [here](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows#secret-manager).

# The Problem

The User Secrets Manager is a great tool, but when you change your development machine, usually you clone your project code from a remote repository and then you would like to be up and running for coding and testing in a matter of seconds.

But if you have managed secrets with the User Secrets Manager you will not be immediatly able to test your code because you will miss something very important on your new machine: **the secret settings** that let your code work.

# The Solution

For being  ready to start coding and testing on the new development machine, you have three choices.

1) Manually copy secret files from the old machine to the new one, if you still have access to the old machine.
2) Recreate the secret settings on your new machine for each project of the solution, but this can be tedious because you have to recover passwords, keys, etc. from different resources and it can be time consuming.
3) **\*\*New\*\*** : use **Visual Studio Solution Secrets** to synchronize secret settings through the cloud in a quick and secure way.

**Visual Studio Solution Secrets** synchronizes secret settings through the cloud in a quick and secure way.

The idea is to use a **secure** repository in the cloud for storing secret settings, so that when you change the development machine, you don't have to copy any file from the old one.

Just install the tool, configure it and pull the solutions secrets on your new machine and you are ready to code. 

***It's fast!***

Visual Studio Solution Secrets support two kinds of remote repositories:
- GitHub Gists
- Azure Key Vault

## GitHub Gists

A "gist" is a snippet of code that can either be public or secret. Visual Studio Solution Secrets uses only **secret** gists.

GitHub Gists is the default repository used by Visual Studio Solution Secrets for storing solutions secrets. Secrets are collected, **encrypted** and pushed on your GitHub account in a **secret gist**, so that only you can see them. The encryption key is generated from a passphrase or a key file that you specify during the one time initialization phase of the tool.

GitHub Gists fits very well for personal use.

![Concept](https://raw.githubusercontent.com/ernstc/VisualStudioSolutionSecrets/main/media/github-flow.png)

## Azure Key Vault

Azure Key Vault is a cloud service for securely storing and accessing secrets. Secrets are encrypted at rest and can be accessed only be authorized accounts. No one else can read their contents.

Since the secrets are encrypted at rest and communication with the key vault is secure because it is enforced as HTTPS / TLS 1.2, Visual Studio Solution Secrets does not encrypt the secrets by itself before sending them to the key vault, therefore it is not necessary to use the encryption key on the local machine.

This opens the scenario in which you can share the secrets of the solution with the development team. You just need to authorize the team with read or read/write access to the Azure Key Vault secrets, so that the team can pull secrets.

Azure Key Vault fits better for enterprise use and **is the recommended way for sharing solution secrets within the team.**

You can read the Azure Key Vault documentation [here](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)

![Concept](https://raw.githubusercontent.com/ernstc/VisualStudioSolutionSecrets/main/media/azurekv-flow.png)

<br/>

# How to install

The tool is installed using the **dotnet** command line interface:

```shell
dotnet tool install --global vs-secrets
```

For upgrading to the latest version, use the command:

```shell
dotnet tool update --global vs-secrets
```

# Configure the encryption key and authorizations

In case you use GitHub Gists as secrets repository, after the tool is installed, you need to create the encryption key and then authorize the use of your GitHub Gists. 

> The local encryption key is not necessary with Azure Key Vault.

Create the encryption key from a passphrase:
```shell
vs-secrets init -p <your-passphrase>
```
Otherwise, you can create the encryption key from a key file with the command below:
```shell
vs-secrets init --keyfile <file-path>
```

In case the encryption key is compromised you can change it. 
```shell
vs-secrets change-key --passphrase <new-passphrase>
vs-secrets change-key --keyfile <file-path>
```
When you change the encryption key with one of the above commands, any secret already encrypted on GitHub Gists is re-encrypted with the new key. In this way the compromised key becomes useless.

# Repository configuration

As said, GitHub Gists is the default repository, but in case you need, any solution can use a different repository for storing its secret settings.

> For example, you can set up your own projects to use GitHub Gists and work projects to use Azure Key Vault.

For configuring the solution to use GitHub Gists:
```shell
vs-secrets configure --repo github
```
For configuring the solution to use Azure Key Vault:
```shell
vs-secrets configure --repo azurekv --name <vault-name | vault-uri>
```

The `--name` option works only for Azure Key Vault repositories and can accept the key vault URI (ex: `https://my-keyvault.vault.azure.net`) or the resource name (ex: `my-keyvault`). In case you use the key vault resource name, it is then completed in its URI (ex: `my-keyvault` => `https://my-keyvault.vault.azure.net`). 

For changing the default repository, use one of the commands below:
```shell
vs-secrets configure --default --repo github
vs-secrets configure --default --repo azurekv --name <vault-name | vault-uri>
```

Sometimes you need to check what is the default repository, or if the solution has a custom repository configuration.

The command `configure list` serves to this purpose.
```shell
vs-secrets configure list [<folder-path>] [--all]
```

# Push solution secrets

For pushing the secrets of the solution in the current folder:
```shell
vs-secrets push
```
For pushing the secrets of the solution in another folder:
```shell
vs-secrets push <solution-path>
```
For pushing the secrets of all the solutions in a folder tree:
```shell
vs-secrets push --all
vs-secrets push <path> --all
```

# Pull solution secrets

For pulling the secrets of the solution in the current folder:
```shell
vs-secrets pull
```
For pulling the secrets of the solution in another folder:
```shell
vs-secrets pull <solution-path>
```
For pulling the secrets of all the solutions in a folder tree:
```shell
vs-secrets pull --all
vs-secrets pull <path> --all
```

# Utility commands

## Search for solutions that use secrets

You can use the tool for just searching solutions and projects that use secrets
```shell
vs-secrets search [<solution-path>] [--all]
```

## Checking the status

With the `status` command you can check for the status of the tool. The command below checks if the encryption key has been defined and if the tool has been authorized to access GitHub Gists:
```shell
vs-secrets status
```
If the current folder contains a solution, the `status` command will show also the synchronization status for the secrets of the solutions.

Optionally you can check the synchronization status in another folder specifying the folder path or in an entire folder tree adding the `--all` parameter. Here are some examples:
```shell
vs-secrets status --all
vs-secrets status c:\projects\my-project-folder
vs-secrets status c:\projects --all
```

## Clear secret settings from the local machine
If there are secrets that you need to clear locally, the `clear` command erases the solution secrets from the local machine. It is equivalent to applying the command `dotnet user-secrets clear` to each project in the solution. 
```shell
vs-secrets clear
vs-secrets clear .\my-solution.sln
```

# Configuration files

Visual Studio Solution Secrets stores its configuration files in the machine's user profile folder.

| Platform | Path |
|:---|:---|
| Windows | `%APPDATA%\Visual Studio Solution Secrets` |
| macSO | `~/.config/Visual Studio Solution Secrets` |
| Linux | `~/.config/Visual Studio Solution Secrets` |

The files generated by the tool are listed below.

| File | Description |
|:---|:---|
| cipher.json | Contains the encryption key |
| github.json | Contains the access token for managing user's GitHub Gists |
| configuration.json | Contains the settings for the repository to use by default and for each solution configured with the command `configure`

