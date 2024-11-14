# StardewExportAudio

A SDV mod that exports data about the game's audio to JSON. The format of the output JSON is 
```typescript
{ [trackHexId: string]: [category: string, name: string] }
```

This is a slightly modified version of [Pathoschild's audio export code](https://stardewvalleywiki.com/Modding_talk:Audio).
