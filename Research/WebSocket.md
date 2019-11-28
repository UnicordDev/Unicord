# WebSocket Events

## 3: Status Update
See https://discordapp.com/developers/docs/topics/gateway#update-status

 JSON Parameter | Type      | Default | Details 
 -------------- | --------- | ------- | ------- 
 `afk`          | `bool`    | n/a     | If `true`, push notifications are delivered to mobile devices.

```json
{"op":3,"d":{"status":"online","since":0,"activities":[],"afk":true}}
```

## 8: Guild Member Request
Requests a chunk of guild members, quite advanced, can be used for searching as well as optimising number of presences. Generally used after messages have been fetched in large guilds.

 JSON Parameter | Type          | Default | Details 
 -------------- | ------------- | ------- | ------- 
 `guild_id`     | `snowflake[]` | n/a     | List of guilds to request user presences for
 `user_ids`     | `snowflake[]` | n/a     | List of users to request user presences for


```json
{"op":8,"d":{"guild_id":["guild_id"],"user_ids":["user_id"]}}
```

## 14: FUCKING EVERYTHING
So Discord seem to use this opcode for like 50 different things, it's used for requesting presences, statuses, and so on, but also activating typing and activity events in guilds?! Like jesus what the fuck is with the focus on 14

 JSON Parameter | Type                           | Default | Details 
 -------------- | ------------------------------ | ------- | ------- 
 `guild_id`     | `snowflake`                    | n/a     | The guild to sync
 `typing`       | `bool`                         | n/a     | Recieve typing events?
 `activities`   | `bool`                         | n/a     | Recieve status update events?
 `lfg`          | `bool`                         | n/a     | Recieve LFG update events? (presumably)
 `channels`     | `map<snowflake, list<range>>?` | n/a     | Map of Channel ID to range of user presences to fetch


Requests Discord send typing events for the specified `guild_id`:
```json
{"op":14,"d":{"guild_id":"guild_id","typing":true,"activities":true}}
```

Requests members for the last 99 messages? in the specified channels. Could also be top 99 in the user list. Not 100% sure.
```json
{"op":14,"d":{"guild_id":"guild_id","typing":true,"activities":true,"lfg":true,"channels":{"channel_id":[[0,99]]}}}

{"op":14,"d":{"guild_id":"guild_id","channels":{"channel_id":[[0,99]],"channel_id":[[0,99]]}}}
```

```json
{"t":"GUILD_MEMBERS_CHUNK","s":46,"op":0,"d":{"not_found":[],"members":[{"user": { }}],"guild_id":"guild_id"}}
````