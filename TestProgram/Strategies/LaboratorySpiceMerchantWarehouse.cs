﻿using Dominion;
using CardTypes = Dominion.CardTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program
{
    public static partial class Strategies
    {
        public static class LaboratorySpiceMerchantWarehouse
        {
            // big money smithy player
            public static PlayerAction Player(int playerNumber)
            {
                return new MyPlayerAction(playerNumber);                
            }

            class MyPlayerAction
                : PlayerAction
            {
                public MyPlayerAction(int playerNumber)
                    : base("LaboratorySpiceMerchantWarehouse",
                            playerNumber,
                            purchaseOrder: PurchaseOrder(),                         
                            actionOrder: ActionOrder(),
                            trashOrder: TrashOrder())
                {
                }

                public override bool ShouldPutCardOnTopOfDeck(Card card, GameState gameState)
                {
                    return true;
                }

                public override PlayerActionChoice ChooseBetween(GameState gameState, IsValidChoice acceptableChoice)
                {
                    return PlayerActionChoice.PlusCard;
                }
            }

            private static ICardPicker PurchaseOrder()
            {
                var highPriority = new CardPickByPriority(
                     CardAcceptance.For<CardTypes.Province>(gameState => CountAllOwned<CardTypes.Gold>(gameState) >=2),
                     CardAcceptance.For<CardTypes.Duchy>(gameState => CountOfPile<CardTypes.Province>(gameState) <= 4),                     
                     CardAcceptance.For<CardTypes.Gold>(),
                     CardAcceptance.For<CardTypes.Laboratory>());

                var buildOrder = new CardPickByBuildOrder(                    
                    new CardTypes.SpiceMerchant(),
                    new CardTypes.Silver(),                    
                    new CardTypes.Warehouse());

                var lowPriority = new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Silver>(),
                           CardAcceptance.For<CardTypes.Estate>(gameState => CountOfPile<CardTypes.Province>(gameState) <= 4));

                return new CardPickConcatenator(highPriority, buildOrder, lowPriority);
            }

            private static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Laboratory>(),
                           CardAcceptance.For<CardTypes.SpiceMerchant>(gameState => CountInHand<CardTypes.Copper>(gameState) > 0),
                           CardAcceptance.For<CardTypes.Warehouse>());
            }

            private static CardPickByPriority TrashOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Copper>());
            } 
        }
    }
}