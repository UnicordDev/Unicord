# Unicord
A free, open source Discord Client for Windows 10 and Windows 10 Mobile, that tries to provide a fast, efficient, native feeling Discord experience. Built on [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/)!

![promo](https://i.imgur.com/ZGz3UIv.png)

## Getting Started
So you wanna build Unicord, well you're gonna need to have a few things handy.

### Prerequisites
 - Windows 10 build 1809+
 - Visual Studio 2017/2019 (with UWP tooling for both .NET and C++)
 - Windows 10 SDK build 17763+ (subject to change)

### Building and Installing
Firstly, as with all GitHub projects, you'll want to clone the repo, but you will also need to pull submodules, to do this, use:

```
git submodule update --recursive
```

From here, building should be as simple as double clicking `Unicord.sln`, ensuring your targets are appropriate to your testing platform (i.e. Debug x64), and hitting F5. Once built and deployed, it should show in your start menu as "Unicord Canary", data and settings are kept separate from the Store version, so they can be installed side by side.

![Canary](https://i.imgur.com/NaMdkZ4.png)

## Testing
Unicord curently lacks any kind of unit testing. This will likely change as I adopt a more sane workflow, but for now, I suggest going around the app and making sure everything you'd use regularly works, and ensuring all confugurations build. A handy way of doing this, is Visual Studio's Batch Build feature, accessible like so:

![batch build](https://i.imgur.com/8bvkRRv.png)

On one specific note, while the project technically targets a minimum of Windows 10 version 1709 (Fall Creators Update), all code should compile and run on version 170**3** (Creators Update) to maintain Windows Phone support. Please pay special attention to the minimum required Windows version when consuming UWP APIs, and be careful when consuming .NET Standard 2.0 APIs, which may require a newer Windows version.

## Contributing
Unicord accepts contributions! Want a feature that doesn't already exist? Feel free to dig right in and give it a shot. Do be mindful of other ongoing projects, make sure someone isn't already building the feature you want, etc. If you don't have the know how yourself, file an issue, someone might pick up on it.

## Licence
Unicord is licenced under the MIT License.

## Acknowledgements
 - [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) Contributors, for providing a wonderful base on which I've built much of this
 - Any member of my [Discord server](https://discord.gg/NfneAaS) who's given me any tips, feedback or guidence! <3