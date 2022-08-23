# Management resource for RAGECOOP

This resource allows you to create and manage user credential, you can kick or ban other users on your server.

## Installation
Put RageCoop.Resources.Management.respkg in [ServerRoot]\Resources\Packages

## Commands

Prefix command name with "/" to execute commands with chat messages, e.g. `/kick Asshole123`

### kick [username]
Required permissions: Kick <br/>
Kick a user from this server.

### ban [username|address]
Required permissions: Ban <br/>
Ban a user permenently from this server.

### unban [username|address]
Required permissions: Ban <br/>
Unban a user from this server.

### register [username] [password]
Required permissions: Register <br/>
register a user on this server with secified password and assigned the default role (see configurations).

### unregister [username]
Required permissions: All or None <br/>
Remove a user from the database, but the client won't be kicked immediately.
The client that executes this command must have `All` permission for the [username] argument to work. 
If [username] is not provided, the executor itself will be unregistered.

### setrole [username] [role]
Required permissions: All <br/>
Set a user to a specific role.

# Configurations
The user database and configuration file is located at [ServerRoot]\Resources\Server\data\RageCoop.Resources.Management
Some basic configuration are stored in Config.json, explained as follows:
```
{
  "AllowGuest": true,
  "DefaultRole": "User",
  "Roles": {
    "Admin": {
      "CommandFilteringMode": 1,
      "Permissions": "All",
      "WhiteListedCommands": [],
      "BlackListedCommands": []
    },
    "User": {
      "CommandFilteringMode": 1,
      "Permissions": "Register",
      "WhiteListedCommands": [],
      "BlackListedCommands": []
    },
    "Guest": {
      "CommandFilteringMode": 0,
      "Permissions": "Register",
      "WhiteListedCommands": [
        "register"
      ],
      "BlackListedCommands": []
    }
  }
}
```

## AllowGuest
This options indicates whether your server will accept incoming connections with unauthorized identity. if set to false, other players won't be able to connect unless provided a valid credentials stored in Member.db


## Roles
One user belongs to a specific role, allowing them to execute specific command and operations only.
Roles are defined in Config.json


There're some default roles defined
- Admin: Server administrator with all permissions
- User: Registered user that can execute non-blacklisted commands
- Guest: Unauthorized user will be assigned to this role, they cannot user command other than `register` by default. DO NOT delete this role!

You can define your own roles as well.

## CommandFilteringMode
Possible value are only 0 and 1.
If set to 0, user won't be able to execute commands not defined in WhiteListedCommands.
If set to 1, user will able to execute all non-managerial command not defined in BlackListedCommands.
## Permissions
All possible values listed below, you can define multiple permissions by seperating them with comma
```
None,
Mute,
Kick,
Ban,
Register,
All
```

## Server setup
You can execute commands directly in server console with all permissions, there's one default admin user:<br/>
- Name: Sausage
- Password: ilovesausage
- Remeber to change the password or unregister it before you expose the server to internet!

