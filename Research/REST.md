# REST APIs

## `GET /users/@me/mentions`
Gets list of recent mentions for the current user 

 Query Parameter  | Type      | Default | Details 
 ---------------- | --------- | ------- | ------- 
 `limit`          | number    | `25`    | Limit of mentions to get
 `guild_id`       | snowflake | `none`  | The guild to get mentions in
 `roles`          | bool      | `true`  | Include @role mentions?
 `everyone`       | bool      | `true`  | Include @everyone mentions?

Returns a list of message objects.

## `POST /channels/:channel_id/call/ring` 
Starts a voice call in a DM channel.

 JSON Parameter | Type      | Default | Details 
 -------------- | --------- | ------- | ------- 
 `recipients`   | ??        | null    | Unknown (maybe group related?)

```json
{"recipients":null}
```
