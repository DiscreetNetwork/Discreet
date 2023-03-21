<div align="center">
  <h1Discreet Network</h1>
  <img src="https://files.discreet.net/discreet_logo.png" width="100"/>
</div>

## Discreet Master Branch
[![Chat on Telegram][ico-telegram]][link-telegram]
![Discreet Main Commits][ico-activity-commit]
#### WIP REPOSITORY – CODE IS PRESENTED “AS IS”. Please refer to the [roadmap](https://discreet.net/roadmap) for progress.

For more information about this project go to https://discreet.net or send us an e-mail, also found on the website.

This is the main repository for the Discreet Network. This project interfaces with the [DiscreetCore library](https://github.com/DiscreetNetwork/DiscreetCore-Win) to perform the necessary cryptographic operations required for the Discreet protocol.

For a cross-platform graphical user interface you can review the [discreet-gui](https://github.com/DiscreetNetwork/discreet-gui) repository.

Documentation on building this repository for multiple platforms will soon be added.

Interacting with the daemon through the JSON-RPC API is documented [on the Discreet docs website](https://developers.discreet.net/docs).

### Compilation guide
If you wish to build Discreet yourself, rather than use one of the prepackaged executables, you can do so. A guide is coming soon.

Dependencies:

Daemon: `.NET Runtime 6.0`

DiscreetCore: `Microsoft Visual C++ Redistributable Package (MSVC v142)`

To setup a local environment, use a loopback address on port `9875` to have a single-peer network. 

### Specifications (Discreet Daemon)
#### Please note that this is for the daemon, not for mining/staking.
#### Minimum Specifications

| CPU (*) | RAM | Storage | Network Connection |
| :--- | :--- | :--- | :--- |
| x64 1 core; 1 GHz | 2 GB | 60 GB | 1 Mbps |

#### Optimal Specifications

| CPU (*) | RAM | Storage | Network Connection |
| :--- | :--- | :--- | :--- |
| x64 4 cores; 3 GHz | 8 GB | 250 GB | > 10 Mbps |

<sup>(*) Processors supporting AVX2 instruction sets (produced after Q2 2013) recommended for optimal speed and on-disk chain size. </sup>

<sup><b>Important:</b> Please ensure that you are running Discreet on a 64-bit environment. RocksDB does not support x86 systems.</sup>

#### Copyright © 2022 - Juche, SIA
<sup>Discreet® is a <a href="https://euipo.europa.eu/eSearch/#details/trademarks/018562628">registered trademark</a> of Juche, SIA</sup>

[ico-activity-commit]: https://img.shields.io/github/commit-activity/m/DiscreetNetwork/Discreet
[ico-telegram]: https://img.shields.io/badge/@DiscreetNetwork-2CA5E0.svg?style=flat-square&logo=telegram&label=Telegram
[link-telegram]: https://t.me/DiscreetNetwork
