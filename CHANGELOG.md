# [0.6.0](https://github.com/resq-software/dotnet-sdk/compare/v0.5.1...v0.6.0) (2026-04-01)


### Features

* add SimDemo example — standalone simulation, mesh, and dialect demo ([badeced](https://github.com/resq-software/dotnet-sdk/commit/badecedc375c092db3dd3232804c96b43b4ee5e8))

## [0.5.1](https://github.com/resq-software/dotnet-sdk/compare/v0.5.0...v0.5.1) (2026-03-31)


### Bug Fixes

* **ci:** remove custom codeql.yml — conflicts with GitHub default CodeQL setup ([dfc1e5e](https://github.com/resq-software/dotnet-sdk/commit/dfc1e5ef342fc7d1fa322e8bf33b02a82b89474a))

# [0.5.0](https://github.com/resq-software/dotnet-sdk/compare/v0.4.3...v0.5.0) (2026-03-31)


### Bug Fixes

* address CI format and critical/high PR review findings ([c4e26de](https://github.com/resq-software/dotnet-sdk/commit/c4e26de9964e55a26e612b2c1e15394d10ab46e0))
* address CI format and medium PR review findings ([0daea00](https://github.com/resq-software/dotnet-sdk/commit/0daea009c818dc704a090a78b5b2e90014f36d40))
* address CI format and PR review findings ([e4394e7](https://github.com/resq-software/dotnet-sdk/commit/e4394e78176edbaafc13ff2fa3fd1fdd19c1b106))
* address CI format and PR review findings ([ae54501](https://github.com/resq-software/dotnet-sdk/commit/ae545016a8bd0aa4a7aa7e821536baa965ee278b))
* address critical and high-priority PR review findings ([6596f32](https://github.com/resq-software/dotnet-sdk/commit/6596f32a3fc7fdfc77c52079be0ac6d4f7ad63af)), closes [hi#priority](https://github.com/hi/issues/priority)
* address medium-priority PR review findings — enums, docs, config ([3682569](https://github.com/resq-software/dotnet-sdk/commit/368256992877765abeb610791528f4befefe1c9e))
* **ci:** drop netstandard2.1 target from ResQ.Mavlink ([121f9ae](https://github.com/resq-software/dotnet-sdk/commit/121f9aec16242d1e515b0906f1a159a69933b207))
* **ci:** remove global.json in CI to use setup-dotnet SDK directly ([b696938](https://github.com/resq-software/dotnet-sdk/commit/b6969385425eeb81ec6ae691ae18787581da5cc4))
* **ci:** resolve CodeQL and Release workflow failures ([159a4bb](https://github.com/resq-software/dotnet-sdk/commit/159a4bbc3c5ae5f6a3c4650341e8c46c60424b71)), closes [C#-only](https://github.com/C/issues/-only)
* **ci:** use camelCase for DOTNET_ROLL_FORWARD env var ([a71c81f](https://github.com/resq-software/dotnet-sdk/commit/a71c81f22cbc1e9689510f20339a30e5d3a979e9))
* resolve dotnet format whitespace violations across all projects ([9856483](https://github.com/resq-software/dotnet-sdk/commit/9856483e2276548e0acf82815437ca44dd1a47a5))
* revert global.json to SDK 9.0.100 with latestMajor rollForward ([2486dd1](https://github.com/resq-software/dotnet-sdk/commit/2486dd1794699172e98a4c485b119c5fe1bb4e7f))
* **sim-engine:** use configured FlightModelType in SimulationWorld.AddDrone ([30d01e5](https://github.com/resq-software/dotnet-sdk/commit/30d01e598c4e13e8ed833ceba3577f5000901955))


### Features

* **gateway:** add GatewayRoutingOptions + GatewayRouter (Task 5) ([6525bf4](https://github.com/resq-software/dotnet-sdk/commit/6525bf4edd75d2c3e6bee6926b1a8fd8fd468c59))
* **gateway:** add VehicleState + VehicleStateTracker (Task 4) ([b689418](https://github.com/resq-software/dotnet-sdk/commit/b68941831ac51c2df7364d9fdad4d8def8d52029))
* implement ResQ.Mavlink.Sitl SITL bridge (Tasks 11-17) ([1f4ae1c](https://github.com/resq-software/dotnet-sdk/commit/1f4ae1c82a24bc3abfa9314cf4cbbffdb2a1b544))
* **mavlink-phase2:** implement GcsPassthrough, MavlinkGateway, and E2E integration tests ([17cb109](https://github.com/resq-software/dotnet-sdk/commit/17cb109e7e02fdf6f5e993e3620680f4153d7745))
* **mavlink:** add TcpTransport, SerialTransport, and MavlinkFrameParser ([d845470](https://github.com/resq-software/dotnet-sdk/commit/d845470de8182693eb7f3bea26bda260f886c095))
* **mavlink:** expand message set to ~80 messages (Phase 3) ([4969a35](https://github.com/resq-software/dotnet-sdk/commit/4969a3543ed19ac80534fa7052d647c12a27aa9c))
* **mavlink:** expand message set to ~80 messages (Phase 3) ([eca0903](https://github.com/resq-software/dotnet-sdk/commit/eca0903163a51963259c26187e8dcc38229f4f0b))
* **mavlink:** implement Chunk 2 — enums, messages, and message registry ([9b654c6](https://github.com/resq-software/dotnet-sdk/commit/9b654c6d2174915457570d5455d9141bf5c17463))
* **mavlink:** implement CRC-16/MCRF4XX and protocol constants ([4a6192b](https://github.com/resq-software/dotnet-sdk/commit/4a6192b410ac295124ef89aa31c5f070fd00109c))
* **mavlink:** implement MAVLink v2 codec — serialize and parse with CRC validation ([2e47040](https://github.com/resq-software/dotnet-sdk/commit/2e470409661b5cf36eac56e724378ad60d5708b2))
* **mavlink:** implement MavlinkPacket record ([c2e77e7](https://github.com/resq-software/dotnet-sdk/commit/c2e77e7e37d5afdb1f5402992730ca9733499bc7))
* **mavlink:** implement MissionProtocol upload/download ([a7482d7](https://github.com/resq-software/dotnet-sdk/commit/a7482d73e5451464c4cfdad03eb49962fc321c22))
* **mavlink:** implement Phase 4 custom ResQ MAVLink dialect (IDs 60000-60007) ([e1dd558](https://github.com/resq-software/dotnet-sdk/commit/e1dd5583eb9ad39d0a511a3acb326ea21f6e0344))
* **mavlink:** implement Phase 5 mesh transport and firmware hooks ([1c823b9](https://github.com/resq-software/dotnet-sdk/commit/1c823b9dcb6063e0bc375151f2f3f3405764e342))
* **mavlink:** implement transport layer and connection management ([99cb42b](https://github.com/resq-software/dotnet-sdk/commit/99cb42ba81226adc1ffc9edccc70b41fb4ee29c3))
* **mavlink:** scaffold ResQ.Mavlink project and test project ([f84c662](https://github.com/resq-software/dotnet-sdk/commit/f84c662cf62509b6cb7f522f3d0750bc2d6a792c))
* scaffold ResQ.Mavlink.Gateway with MavStateMapper and MessageTranslator (Phase 2 Tasks 1-3) ([e22c81c](https://github.com/resq-software/dotnet-sdk/commit/e22c81c5ff38192584945568e1ce19b3a655c94c))
* **sim-engine:** add DronePhysicsState, FlightCommand, and IFlightModel interface ([bc95817](https://github.com/resq-software/dotnet-sdk/commit/bc9581781f37c740a789f9ccffcddbfe1f9710fc))
* **sim-engine:** add SimulationConfig with IOptions<T> pattern ([babcff6](https://github.com/resq-software/dotnet-sdk/commit/babcff6d42a399b87dc108ca9ee1498150ad160c))
* **sim-engine:** add Structure entity with damage states ([f112fd6](https://github.com/resq-software/dotnet-sdk/commit/f112fd65af4bdfe8c8a50c7ccca2aac3ec77c7b9))
* **sim-engine:** implement ISimulationClock with stepped/realtime/accelerated modes ([e9c49c3](https://github.com/resq-software/dotnet-sdk/commit/e9c49c39456afac1c4254f21fcc4377629eb12ae))
* **sim-engine:** implement ITerrain with FlatTerrain and HeightmapTerrain ([2ff1894](https://github.com/resq-software/dotnet-sdk/commit/2ff1894cbad629ef6c2db94a93bb27aa580a8ece))
* **sim-engine:** implement IWeatherSystem with calm/steady/turbulent wind modes ([569c986](https://github.com/resq-software/dotnet-sdk/commit/569c9862dea0117e9425139b67b0c1cbe9ceffa7))
* **sim-engine:** implement KinematicFlightModel with waypoint navigation and landing ([8112916](https://github.com/resq-software/dotnet-sdk/commit/811291667bcc0e8c9beeea1f7a2b6308933ab203))
* **sim-engine:** implement QuadrotorFlightModel with 6DOF dynamics and PD control ([acd3743](https://github.com/resq-software/dotnet-sdk/commit/acd37433fbbc0357caea4d5269fcb89618082594))
* **sim-engine:** implement SimulatedDrone entity with flight model and detection ([d232e5b](https://github.com/resq-software/dotnet-sdk/commit/d232e5b54eede43511875974c7b85c42558ad643))
* **sim-engine:** implement SimulationWorld with fixed-timestep sim loop ([3fff00e](https://github.com/resq-software/dotnet-sdk/commit/3fff00ee5888e656e452dd56904c6ba7e71e3d4d))
* **sim-engine:** scaffold ResQ.Simulation.Engine project and test project ([dc980ae](https://github.com/resq-software/dotnet-sdk/commit/dc980aef67677455272a08f8d201a714b569b57e))

## [0.4.3](https://github.com/resq-software/dotnet-sdk/compare/v0.4.2...v0.4.3) (2026-03-22)


### Bug Fixes

* **ci:** address PR review suggestions for proto sync and CI hardening ([#15](https://github.com/resq-software/dotnet-sdk/issues/15)) ([8d64e7b](https://github.com/resq-software/dotnet-sdk/commit/8d64e7bcea655872ccc969b90b62365993f0499a))

## [0.4.2](https://github.com/resq-software/dotnet-sdk/compare/v0.4.1...v0.4.2) (2026-03-21)


### Performance Improvements

* **ci:** sync directly to monorepo main instead of creating PRs ([3f94725](https://github.com/resq-software/dotnet-sdk/commit/3f94725dba71db53f5e00dfaeac23a8e0977fbad))

## [0.4.1](https://github.com/resq-software/dotnet-sdk/compare/v0.4.0...v0.4.1) (2026-03-21)


### Bug Fixes

* **ci:** pin actions to SHA hashes and add permissions ([e7de772](https://github.com/resq-software/dotnet-sdk/commit/e7de772303d38e8289c8217c9a0a7a179ec2f263))

# [0.4.0](https://github.com/resq-software/dotnet-sdk/compare/v0.3.2...v0.4.0) (2026-03-19)


### Features

* **clients:** 🛡️ implement resilience pipelines and cancellation support ([12e99c9](https://github.com/resq-software/dotnet-sdk/commit/12e99c95223bcdde1755c2889f591f21a6da3b95))

## [0.3.2](https://github.com/resq-software/dotnet-sdk/compare/v0.3.1...v0.3.2) (2026-03-18)


### Bug Fixes

* **test:** 🔒 ensure thread safety in MockHttpMessageHandler ([5854441](https://github.com/resq-software/dotnet-sdk/commit/5854441f0f68fea512b0c08a8afc4548f4cdb042))

## [0.3.1](https://github.com/resq-software/dotnet-sdk/compare/v0.3.0...v0.3.1) (2026-03-17)


### Bug Fixes

* **packaging:** add shared NuGet package readme ([#8](https://github.com/resq-software/dotnet-sdk/issues/8)) ([47f51de](https://github.com/resq-software/dotnet-sdk/commit/47f51de0ff660d71d3366105d7552bffa94f9ae8))

# [0.3.0](https://github.com/resq-software/dotnet-sdk/compare/v0.2.0...v0.3.0) (2026-03-17)


### Features

* **proto:** consume private schemas from resq-proto ([502bd1a](https://github.com/resq-software/dotnet-sdk/commit/502bd1a62ad99f28f6788d34c0f2e90b50fc7f4f))

# [0.2.0](https://github.com/resq-software/dotnet-sdk/compare/v0.1.0...v0.2.0) (2026-03-13)


### Features

* **ci:** inbound sync — create PR in resQ monorepo on push to main ([73ba44b](https://github.com/resq-software/dotnet-sdk/commit/73ba44b13f34e48d185c9f8826f64a0257ca461d))
