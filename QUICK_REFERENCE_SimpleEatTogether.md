# Simple "Let's Eat Together" - Quick Reference

## ?? Quick Start

### RimTalk Command
```
Player: "Let's eat together with Bob"
AI: "ºÃ°¡£¡{"action": "simple_eat_together", "target": "Bob"}"
```

### Developer Console
```csharp
var def = DefDatabase<InteractionDef>.GetNamed("LetsTalkEatTogether");
def.Worker.Interacted(pawn1, pawn2, null, out _, out _, out _, out _);
```

### Code Call
```csharp
RimTalkActions.ExecuteSimpleEatTogether(initiator, "Bob");
```

---

## ? What It Does

1. ? Validates both pawns (alive, spawned, not downed)
2. ? Checks hunger levels (optional)
3. ? Finds best food for each pawn
4. ? Creates vanilla `Ingest` jobs
5. ? Forces both pawns to start eating immediately

---

## ?? Key Features

- **Vanilla Jobs Only** - Uses `JobDefOf.Ingest`
- **No Custom Drivers** - Maximum stability
- **Independent** - Each pawn finds own food
- **Thread Safe** - Protected map access
- **Graceful Failure** - Returns false if can't execute

---

## ?? Files

| File | Purpose |
|------|---------|
| `InteractionWorker_LetsTalkEatTogether.cs` | Main logic |
| `Interaction_LetsTalkEatTogether.xml` | RimWorld def |
| `RimTalkActions.cs::ExecuteSimpleEatTogether` | Helper method |

---

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| Neither eats | Check if food is available |
| Only one eats | Second pawn failed safety check |
| Eats then stops | Normal - job can be interrupted |
| "Not found" error | Check target name spelling |

---

## ?? When To Use

**Use Simple (This):**
- ? Need maximum stability
- ? Don't need synchronized eating
- ? Quick implementation

**Use Complex (SocialDine):**
- ? Want shared meals
- ? Need table coordination
- ? Advanced roleplay

---

## ?? Status

? **Production Ready**  
????? Stability  
? Complexity  
?? Features

---

## ?? Full Docs

- `Docs/Simple_EatTogether_Implementation.md`
- `Docs/TestScript_SimpleEatTogether.cs`
- `Docs/SUMMARY_SimpleEatTogether.md`
