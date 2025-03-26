# P03AI (WIP)

AI plays Inscryption thingy.
Inject [injection.cs](injection.cs) into the game, tweak prompt in [index.ts](index.ts#L5), put OpenAI API token into `OPENAI_API_KEY` env var, and run `index.ts` with [Bun](https://bun.sh).
Press `[` to send summary, press `]` to generate AI response.

## Roadmap

- [ ] Card battle
  - [x] Base stuff
  - [x] Normal battle
  - [x] Totem battle
  - [x] Drawing
  - [ ] Boss battle
    - [x] Boss rares
  - [ ] THE MOON
- [x] Map
  - [x] Node descriptions
  - [x] Info on following nodes
- [ ] Events
  - [ ] Boulder gambling
  - [ ] Woodcarver
    - [x] Base thing
    - [ ] Amalgam
  - [x] Trapper
  - [ ] Card choices
    - [x] Normal
    - [ ] Cost
    - [x] Tribe
    - [ ] Deathcard??
  - [x] Mysterious stones
  - [x] Bone Lord
  - [x] Campfire
  - [ ] Goobert
  - [x] Deck trial
  - [ ] Mycologists
    - [x] Base thing
    - [ ] No duplicates
  - [x] Pack
    - [x] Base thing
    - [x] Pack Rat
  - [x] Trader
