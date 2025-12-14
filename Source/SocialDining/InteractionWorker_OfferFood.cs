using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkExpandActions.SocialDining
{
    public class InteractionWorker_OfferFood : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null)
            {
                return 0f;
            }

            if (initiator.Dead || recipient.Dead)
            {
                return 0f;
            }

            if (!initiator.Spawned || !recipient.Spawned)
            {
                return 0f;
            }

            if (initiator.Map != recipient.Map)
            {
                return 0f;
            }

            if (!CanOfferFood(initiator, recipient))
            {
                return 0f;
            }

            return 0.02f;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            if (initiator?.needs?.mood != null)
            {
                initiator.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.OfferedFood, recipient);
            }

            if (recipient?.needs?.mood != null)
            {
                recipient.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.ReceivedFoodOffer, initiator);
            }

            if (initiator != null && recipient != null)
            {
                TryStartSocialDining(initiator, recipient);
            }
        }

        private bool CanOfferFood(Pawn initiator, Pawn recipient)
        {
            if (initiator?.needs?.food == null || recipient?.needs?.food == null)
            {
                return false;
            }

            if (initiator.needs.food.CurLevelPercentage > 0.9f && recipient.needs.food.CurLevelPercentage > 0.9f)
            {
                return false;
            }

            Thing food = FoodSharingUtility.FindFoodForSharing(initiator, recipient);
            return food != null;
        }

        private void TryStartSocialDining(Pawn initiator, Pawn recipient)
        {
            Thing food = FoodSharingUtility.FindFoodForSharing(initiator, recipient);
            if (food == null)
            {
                return;
            }

            // Use the ported core method
            FoodSharingUtility.TryTriggerShareFood(initiator, recipient, food);
        }
    }
}
