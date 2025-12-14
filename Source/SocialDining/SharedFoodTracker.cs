using RimWorld;
using Verse;

namespace RimTalkExpandActions.SocialDining
{
    public class SharedFoodTracker : ThingComp
    {
        private Pawn sharer;
        private Pawn recipient;
        private int expiryTick = -1;

        public Pawn Sharer => sharer;
        public Pawn Recipient => recipient;
        public bool IsActive => sharer != null && expiryTick > Find.TickManager.TicksGame;

        public CompProperties_SharedFoodTracker Props => (CompProperties_SharedFoodTracker)props;

        public bool IsSharedWith(Pawn pawn)
        {
            return pawn != null && IsActive && recipient == pawn;
        }

        public bool IsSharedBy(Pawn pawn)
        {
            return pawn != null && IsActive && sharer == pawn;
        }

        public void MarkShared(Pawn newSharer, Pawn newRecipient)
        {
            sharer = newSharer;
            recipient = newRecipient;
            expiryTick = Find.TickManager.TicksGame + Props.shareDurationTicks;
        }

        public void Clear()
        {
            sharer = null;
            recipient = null;
            expiryTick = -1;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!IsActive && (sharer != null || recipient != null))
            {
                Clear();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref sharer, "sharer");
            Scribe_References.Look(ref recipient, "recipient");
            Scribe_Values.Look(ref expiryTick, "sharedFoodExpiryTick", -1);
        }
    }

    public class CompProperties_SharedFoodTracker : CompProperties
    {
        public int shareDurationTicks = 2500 * 10; // 10 minutes

        public CompProperties_SharedFoodTracker()
        {
            compClass = typeof(SharedFoodTracker);
        }
    }
}
