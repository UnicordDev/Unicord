# Unicord
A free, open source Discord Client for Windows 10 and Windows 10 Mobile, that tries to provide a fast, efficient, native feeling Discord experience, while adding handy extras along the way. Built on [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/)!

[![Build Status](https://dev.azure.com/WanKerrCoLtd/Unicord/_apis/build/status/WamWooWam.Unicord?branchName=master)](https://dev.azure.com/WanKerrCoLtd/Unicord/_build/latest?definitionId=4&branchName=master)

![promo](https://raw.githubusercontent.com/WamWooWam/Unicord/master/Assets/promo1.png)

## Getting Started
So you wanna build Unicord, well you're gonna need to have a few things handy.

### Prerequisites
 - Windows 11 (Build 22000+)
 - Visual Studio 2019 or later, with the Universal Windows Platform workload.
 - Windows 11 SDK Build 22000

### Building and Installing
Firstly, as with all GitHub projects, you'll want to clone the repo, but you will also need to pull submodules, to do this, use:

```sh
$ git submodule update --init --recursive
```

From here, building should be as simple as double clicking `Unicord.sln`, ensuring your targets are appropriate to your testing platform (i.e. Debug x64), and hitting F5. 
Once built and deployed, it should show in your start menu as "Unicord Canary", data and settings are kept separate from the Store version, so they can be installed side by side.

![Canary](https://i.imgur.com/NaMdkZ4.png)

## Testing
Unicord currently lacks any kind of unit testing. This will likely change as I adopt a more sane workflow, but for now, I suggest going around the app and making sure everything you'd use regularly works, and ensuring all configurations build. A handy way of doing this, is Visual Studio's Batch Build feature, accessible like so:

![batch build](https://i.imgur.com/8bvkRRv.png)

On one specific note, while the project technically targets a minimum of Windows 10 version 1709 (Fall Creators Update), all code should compile and run on version 170**3** (Creators Update) to maintain Windows Phone support. Please pay special attention to the minimum required Windows version when consuming UWP APIs, and be careful when consuming .NET Standard 2.0 APIs, which may require a newer Windows version.

## Contributing
Unicord accepts contributions! Want a feature that doesn't already exist? Feel free to dig right in and give it a shot. Do be mindful of other ongoing projects, make sure someone isn't already building the feature you want, etc. If you don't have the know how yourself, file an issue, someone might pick up on it.

## Get in Touch
We have a Discord server specifically for Unicord development and testing, join here:

[![Unicord](https://discordapp.com/api/guilds/648519011130408980/widget.png?style=banner2)](https://discord.gg/64g7M5Y)

## License
Unicord is licensed under the [MIT License](LICENSE).

## Acknowledgements
 - [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) Contributors, for providing a wonderful base on which I've built much of this
 - Any member of my personal Discord server who's given me any tips, feedback or guidance! <3
