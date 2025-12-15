using System.Linq;
using RimWorld;
using Verse;
using RimTalkExpandActions.Memory.Actions;

/// <summary>
/// Quick test script for Simple "Let's Eat Together" implementation
/// 
/// HOW TO USE:
/// 1. Enable Developer Mode in RimWorld (Options ¡ú Development Mode)
/// 2. Press F12 to open developer console
/// 3. Copy and paste this entire script
/// 4. Watch the two pawns immediately start eating
/// </summary>
public static class TestSimpleEatTogether
{
    public static void RunTest()
    {
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
        Log.Message("Testing Simple 'Let's Eat Together' Implementation");
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");

        // Get the first two colonists
        var colonists = Find.Maps[0].mapPawns.FreeColonists.ToList();
        
        if (colonists.Count < 2)
        {
            Log.Error("Need at least 2 colonists on the map!");
            return;
        }

        Pawn pawn1 = colonists[0];
        Pawn pawn2 = colonists[1];

        Log.Message($"\nTest Subjects:");
        Log.Message($"  Initiator: {pawn1.Name.ToStringShort}");
        Log.Message($"  Recipient: {pawn2.Name.ToStringShort}");

        // Pre-test validation
        Log.Message($"\nPre-Test Status:");
        Log.Message($"  {pawn1.Name.ToStringShort} - Alive: {!pawn1.Dead}, Spawned: {pawn1.Spawned}, Downed: {pawn1.Downed}");
        Log.Message($"  {pawn2.Name.ToStringShort} - Alive: {!pawn2.Dead}, Spawned: {pawn2.Spawned}, Downed: {pawn2.Downed}");

        if (pawn1.needs?.food != null)
        {
            Log.Message($"  {pawn1.Name.ToStringShort} Hunger: {pawn1.needs.food.CurLevelPercentage:P0}");
        }
        if (pawn2.needs?.food != null)
        {
            Log.Message($"  {pawn2.Name.ToStringShort} Hunger: {pawn2.needs.food.CurLevelPercentage:P0}");
        }

        // Method 1: Direct interaction trigger
        Log.Message($"\n[TEST 1] Triggering via InteractionDef...");
        var interactionDef = DefDatabase<InteractionDef>.GetNamedSilentFail("LetsTalkEatTogether");
        
        if (interactionDef == null)
        {
            Log.Error("? InteractionDef 'LetsTalkEatTogether' not found!");
            Log.Error("   Make sure Defs/InteractionDefs/Interaction_LetsTalkEatTogether.xml is loaded");
            return;
        }

        Log.Message($"? Found InteractionDef: {interactionDef.defName}");

        // Trigger the interaction
        interactionDef.Worker.Interacted(
            pawn1,
            pawn2,
            null,
            out string letterText,
            out string letterLabel,
            out LetterDef letterDef,
            out LookTargets lookTargets
        );

        // Check post-test status
        Log.Message($"\nPost-Test Status:");
        
        if (pawn1.jobs?.curJob != null)
        {
            Log.Message($"  {pawn1.Name.ToStringShort} Current Job: {pawn1.jobs.curJob.def.defName}");
            if (pawn1.jobs.curJob.def == JobDefOf.Ingest)
            {
                Log.Message($"    ? Successfully started eating!");
            }
        }
        else
        {
            Log.Warning($"  {pawn1.Name.ToStringShort} has no current job");
        }

        if (pawn2.jobs?.curJob != null)
        {
            Log.Message($"  {pawn2.Name.ToStringShort} Current Job: {pawn2.jobs.curJob.def.defName}");
            if (pawn2.jobs.curJob.def == JobDefOf.Ingest)
            {
                Log.Message($"    ? Successfully started eating!");
            }
        }
        else
        {
            Log.Warning($"  {pawn2.Name.ToStringShort} has no current job");
        }

        // Method 2: Via RimTalkActions helper
        Log.Message($"\n[TEST 2] Waiting 3 seconds, then testing via RimTalkActions...");
        Log.Message($"  (This test will use: RimTalkActions.ExecuteSimpleEatTogether)");
        Log.Message($"  (Check logs in ~3 seconds)");

        // Schedule second test (optional)
        /*
        Find.TickManager.slower.TickAction delegate(
        {
            Log.Message("\n--- Running Test 2 ---");
            RimTalkActions.ExecuteSimpleEatTogether(pawn1, pawn2.Name.ToStringShort);
        }, 180); // 3 seconds at normal speed
        */

        Log.Message("\n¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
        Log.Message("Test Complete!");
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
        Log.Message("Expected Results:");
        Log.Message("  ? Both pawns should interrupt current jobs");
        Log.Message("  ? Both pawns should go to nearest food");
        Log.Message("  ? Both pawns should start eating (Ingest job)");
        Log.Message("  ? Log shows 'successfully started eating'");
        Log.Message("\nIf test fails, check:");
        Log.Message("  - Is there accessible food on the map?");
        Log.Message("  - Are pawns in mental state or downed?");
        Log.Message("  - Check Player.log for detailed error messages");
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
    }

    /// <summary>
    /// Alternative test: Force specific pawns by name
    /// </summary>
    public static void TestWithNames(string name1, string name2)
    {
        Log.Message($"Testing: {name1} invites {name2} to eat");

        var allPawns = Find.Maps[0].mapPawns.AllPawnsSpawned;
        
        Pawn pawn1 = allPawns.FirstOrDefault(p => 
            p.Name.ToStringShort.Contains(name1) || 
            (p.Name as NameTriple)?.Nick?.Contains(name1) == true
        );

        Pawn pawn2 = allPawns.FirstOrDefault(p => 
            p.Name.ToStringShort.Contains(name2) || 
            (p.Name as NameTriple)?.Nick?.Contains(name2) == true
        );

        if (pawn1 == null)
        {
            Log.Error($"Could not find pawn: {name1}");
            return;
        }

        if (pawn2 == null)
        {
            Log.Error($"Could not find pawn: {name2}");
            return;
        }

        Log.Message($"Found: {pawn1.Name.ToStringShort} and {pawn2.Name.ToStringShort}");

        var interactionDef = DefDatabase<InteractionDef>.GetNamed("LetsTalkEatTogether");
        interactionDef.Worker.Interacted(pawn1, pawn2, null, out _, out _, out _, out _);

        Log.Message("Interaction triggered - check if both pawns start eating!");
    }

    /// <summary>
    /// Test safety checks only (without triggering)
    /// </summary>
    public static void TestSafetyChecks()
    {
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
        Log.Message("Testing Safety Checks");
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");

        var colonists = Find.Maps[0].mapPawns.FreeColonists.ToList();
        if (colonists.Count < 2)
        {
            Log.Error("Need at least 2 colonists!");
            return;
        }

        Pawn pawn1 = colonists[0];
        Pawn pawn2 = colonists[1];

        // Test food finding
        Log.Message("\nTesting Food Finding:");
        Log.Message($"  Looking for food for {pawn1.Name.ToStringShort}...");
        
        Thing food1 = FoodUtility.BestFoodSourceOnMap(
            pawn1, pawn1, false, out ThingDef foodDef1,
            FoodPreferability.MealLavish, true, false, false,
            true, false, false, false, false, false, false, false
        );

        if (food1 != null)
        {
            Log.Message($"    ? Found: {food1.LabelShort} at {food1.Position}");
        }
        else
        {
            Log.Warning($"    ? No food found for {pawn1.Name.ToStringShort}");
        }

        Log.Message($"  Looking for food for {pawn2.Name.ToStringShort}...");
        Thing food2 = FoodUtility.BestFoodSourceOnMap(
            pawn2, pawn2, false, out ThingDef foodDef2,
            FoodPreferability.MealLavish, true, false, false,
            true, false, false, false, false, false, false, false
        );

        if (food2 != null)
        {
            Log.Message($"    ? Found: {food2.LabelShort} at {food2.Position}");
        }
        else
        {
            Log.Warning($"    ? No food found for {pawn2.Name.ToStringShort}");
        }

        // Test hunger levels
        Log.Message("\nTesting Hunger Levels:");
        if (pawn1.needs?.food != null)
        {
            float hunger1 = pawn1.needs.food.CurLevelPercentage;
            bool hungry1 = hunger1 < 0.9f;
            Log.Message($"  {pawn1.Name.ToStringShort}: {hunger1:P0} ({(hungry1 ? "Hungry" : "Not hungry")})");
        }

        if (pawn2.needs?.food != null)
        {
            float hunger2 = pawn2.needs.food.CurLevelPercentage;
            bool hungry2 = hunger2 < 0.9f;
            Log.Message($"  {pawn2.Name.ToStringShort}: {hunger2:P0} ({(hungry2 ? "Hungry" : "Not hungry")})");
        }

        // Test validity
        Log.Message("\nTesting Pawn Validity:");
        Log.Message($"  {pawn1.Name.ToStringShort}:");
        Log.Message($"    Alive: {!pawn1.Dead}");
        Log.Message($"    Spawned: {pawn1.Spawned}");
        Log.Message($"    Not downed: {!pawn1.Downed}");
        Log.Message($"    Not mental: {!pawn1.InMentalState}");

        Log.Message($"  {pawn2.Name.ToStringShort}:");
        Log.Message($"    Alive: {!pawn2.Dead}");
        Log.Message($"    Spawned: {pawn2.Spawned}");
        Log.Message($"    Not downed: {!pawn2.Downed}");
        Log.Message($"    Not mental: {!pawn2.InMentalState}");

        Log.Message("\n¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
        Log.Message("Safety Checks Complete");
        Log.Message("¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T");
    }
}

// ¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T
// RUN THE TEST
// ¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T
TestSimpleEatTogether.RunTest();

// Alternative tests (uncomment to use):
// TestSimpleEatTogether.TestWithNames("Val", "Cait");
// TestSimpleEatTogether.TestSafetyChecks();
