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

## 14: Fetch User Presences
Sorta equivelent of `OP 12`, but much more optimised, used for the user list.

 JSON Parameter | Type                           | Default | Details 
 -------------- | ------------------------------ | ------- | ------- 
 `guild_id`     | `snowflake`                    | n/a     | The guild to sync
 `typing`       | `bool`                         | n/a     | Recieve typing events?
 `activities`   | `bool`                         | n/a     | Recieve status update events?
 `lfg`          | `bool`                         | n/a     | Recieve LFG update events? (presumably)
 `channels`     | `map<snowflake, list<range>>?` | n/a     | Map of Channel ID to range of user presences to fetch


```json
{"op":14,"d":{"guild_id":"guild_id","typing":true,"activities":true,"lfg":true}}

{"op":14,"d":{"guild_id":"guild_id","typing":true,"activities":true,"lfg":true,"channels":{"channel_id":[[0,99]]}}}

{"op":14,"d":{"guild_id":"guild_id","channels":{"channel_id":[[0,99]],"channel_id":[[0,99]]}}}
```

```json
{"t":"GUILD_MEMBERS_CHUNK","s":46,"op":0,"d":{"not_found":[],"members":[{"user": { }}],"guild_id":"guild_id"}}
````