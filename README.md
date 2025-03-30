# P03AI (WIP)

![Screenshot](screenshot.png)

AI plays Inscryption thingy.

## Running

Inject [injection.cs](injection.cs) into the game (as Harmony patch for `DiskCardGame.OilPaintingPuzzle.ManagedUpdate()`, or just call `Update()` yourself via any other hook you like), tweak prompt in [index.ts](index.ts#L5), put OpenAI API token into `OPENAI_API_KEY` env var, and run `index.ts` with [Bun](https://bun.sh).
Press `[` to send summary, press `]` to generate AI response.

## TODO

- [ ] Card battle
  - [x] Base stuff
  - [ ] Leshy's surrender offers
  - [x] Normal battle
  - [x] Totem battle
  - [x] Drawing
  - [ ] Boss battle
    - [ ] Angler's hook
    - [ ] Trapper/Trader second phase explanation
    - [ ] Prospector board wipe explanation
    - [ ] Final boss fight shenanigans
    - [x] Boss rares
  - [x] THE MOON
- [x] Map
  - [x] Node descriptions
  - [x] Info on following nodes
- [ ] Events
  - [x] Boulder gambling
  - [x] Woodcarver
    - [x] Base thing
    - [x] Amalgam
  - [x] Trapper
  - [ ] Card choices
    - [x] Normal
    - [x] Cost
    - [x] Tribe
    - [ ] Deathcard??
  - [x] Mysterious stones
  - [x] Bone Lord
  - [x] Campfire
  - [x] Goobert
  - [x] Deck trial
  - [x] Mycologists
    - [x] Base thing
    - [x] No duplicates
  - [x] Pack
    - [x] Base thing
    - [x] Pack Rat
  - [x] Trader
