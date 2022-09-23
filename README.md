[![.NET](https://github.com/ernstc/VisualStudioSolutionSecrets/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ernstc/VisualStudioSolutionSecrets/actions/workflows/dotnet.yml) ![Nuget](https://img.shields.io/nuget/dt/vs-secrets) ![Nuget](https://img.shields.io/nuget/v/vs-secrets)

# ***Visual Studio Solution Secrets***

Synchronize Visual Studio solution secrets across different development machines.

* [Get Started](#get-started)
* [Best Practices](#best-practices)
* [The Problem](#the-problem)
* [The Solution](#the-solution)
* [How to install](#how-to-install)
* [Configure the encryption key and authorizations](#configure-the-encryption-key-and-authorizations)
* [Push solution secrets](#push-solution-secrets)
* [Pull solution secrets](#pull-solution-secrets)
* [Utility commands](#utility-commands)
* [Configuration files](#configuration-files)

# Get Started

If you already know it, here are the quick start commands.

```
dotnet tool install --global vs-secrets
```
```
vs-secrets init -p <your-passphrase>
```
```
vs-secrets pull
```

# Best Practices

As a good practices in DevOps, you must not store secrets (sensitive data like passwords, connection strings, access keys, etc.) in your source code that is committed in a shared repository and secrets must not be deployed with the apps.

Fortunately Visual Studio and .Net help us in separating secrets from our code with the ***User Secrets Manager*** that let us store secrets out of the solution folder. The User Secrets Manager hides implementation details, but essentially it stores secrets in files located in the machine's user profile folder.

You can find the **User Secrets Manager** documentation [here](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows#secret-manager).

# The Problem

The User Secrets Manager is a great tool, but when you change your development machine usually you clone your project code from a remote repository and then you would like to be up and running for coding and testing in a matter of seconds.

But if you have managed secrets with the User Secrets Manager you will not be immediatly able to test your code because you will miss something very important on your new machine: **the secret settings** that let your code work.

# The Solution

For being  ready to start coding and testing on the new development machine you have three choices.

1) Manually copy secret files from the old machine to the new one, if you still have access to the old machine.
2) Recreate the secret settings on your new machine for each project of the solution, but this can be tedious because you have to recover passwords, keys, etc. from different resources and it can be time consuming.
3) **\*\*New\*\*** : use **Visual Studio Solution Secrets** to synchronize secret settings through the cloud in a quick and secure way.

The idea is to use GitHub Gists as the repository for your secrets. Visual Studio Solution Secrets collects all the secret settings used in the solution, encrypts and pushes them on your GitHub account in a secret Gist, so that only you can see them. The encryption key is generated from a passphrase or a key file that you specify during the one time initialization phase of the tool.

Once you change the development machine, you don't have to copy any file from the old one.

Just install the tool, recreate the encryption key with your passphrase or your key file, authorize the tool on GitHub, pull the solutions secrets on your new machine and you are ready to code. It's fast!

![Concept](https://raw.githubusercontent.com/ernstc/VisualStudioSolutionSecrets/main/Concept.png)

# How to install

The tool is installed using the **dotnet** command line interface:

```
dotnet tool install --global vs-secrets
```

If you already have it, but you want to update to the latest version, use the command:

```
dotnet tool update --global vs-secrets
```

# Configure the encryption key and authorizations

After the tool is installed, you need to create the encryption key and then authorize the use of your GitHub Gists. 

Create the encryption key from a passphrase:
```
vs-secrets init -p <your-passphrase>
```
Otherwise, you can create the encryption key from a key file with the command below:
```
vs-secrets init --keyfile <file-path>
```

In case the encryption key is compromised you can change it. 
```
vs-secrets changekey --passphrase <new-passphrase>
vs-secrets changekey --keyfile <file-path>
```
When you change the encryption key with one of the above commands, any secret already encrypted on GitHub is re-encrypted with the new key. In this way the compromised key becomes useless.


# Push solution secrets

For pushing the secrets of the solution in current folder:
```
vs-secrets push
```
For pushing the secrets of the solution in another folder:
```
vs-secrets push --path <solution-path>
```
For pushing the secrets of all the solutions in a folder tree:
```
vs-secrets push --all
```
or
```
vs-secrets push --path <path> --all
```

# Pull solution secrets

For pulling the secrets of the solution in current folder:
```
vs-secrets pull
```
For pulling the secrets of the solution in another folder:
```
vs-secrets pull --path <solution-path>
```
For pulling the secrets of all the solutions in a folder tree:
```
vs-secrets pull --all
```
or
```
vs-secrets pull --path <path> --all
```
# Utility commands
## Search for solutions that use secrets

You can also use the tool for just searching solutions and projects that use secrets
```
vs-secrets search [--path <solution-path>] [--all]
```


## Checking the status

The "status" command let you check for the status of the tool. The command below checks if the encryption key has been defined and if the tool has been authorized to access GitHub Gists:
```
vs-secrets status
```
If needed, you can check also the synchronization status for a specific solution or for all the solutions in a folder tree:

```
vs-secrets status --path <solution-path> [--all]
```

# Configuration files

Visual Studio Solution Secrets stores its configuration files in the machine's user profile folder.

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\Visual Studio Solution Secrets` |
| macSO | `~/.config/Visual Studio Solution Secrets` |
| Linux | `~/.config/Visual Studio Solution Secrets` |

The files generated by the tool are listed below.

| File | Description |
|------|-------------|
| cipher.json | Contains the encryption key |
| github.json | Contains the access token for managing user's GitHub Gists |

