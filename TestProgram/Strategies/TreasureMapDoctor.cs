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
        public static class TreasureMapDoctor
        {            
            public static PlayerAction Player(int playerNumber)
            {
                return new MyPlayerAction(playerNumber);
            }

            class MyPlayerAction
                : PlayerAction
            {
                public MyPlayerAction(int playerNumber)
                    : base("TreasureMapDoctor",
                        playerNumber,
                        purchaseOrder: PurchaseOrder(),                        
                        actionOrder: ActionOrder(),
                        trashOrder: TrashAndDiscardOrder(),
                        discardOrder: TrashAndDiscardOrder())
                {
                }
               
                override public int GetCoinAmountToOverpayForCard(GameState gameState, Card card)
                {
                    return gameState.Self.AvailableCoins;
                }

                public override Card NameACard(GameState gameState)
                {
                    return GetCardTypeToTrash(gameState);
                }

                public override PlayerActionChoice ChooseBetween(GameState gameState, IsValidChoice acceptableChoice)
                {
                    if (gameState.Self.CardsBeingRevealed.HasCard(CardTypes.Silver.card))
                        return PlayerActionChoice.Discard;
                    return PlayerActionChoice.Trash;
                }

                public override Card GetCardFromRevealedCardsToPutOnDeck(GameState gameState, PlayerState player)
                {
                    return player.CardsBeingRevealed.FirstOrDefault();
                }

                static Card GetCardTypeToTrash(GameState gameState)
                {
                    PlayerState self = gameState.Self;

                    if (self.CardsInDeck.Count() <= 3 &&
                        CountInDeck<CardTypes.Estate>(gameState) > 0)
                    {
                        return CardTypes.Estate.card;
                    }

                    int countCopper = CountMightDraw<CardTypes.Copper>(gameState, 3);
                    int countEstate = CountMightDraw<CardTypes.Estate>(gameState, 3);

                    if (Default.ShouldBuyProvinces(gameState))
                        countEstate = 0;

                    if (countCopper + countEstate == 0)
                        return CardTypes.Estate.card;

                    return countCopper > countEstate ? CardTypes.Copper.card : CardTypes.Estate.card;
                }
            }

            private static CardPickByPriority PurchaseOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Doctor>(gameState => CountAllOwned<CardTypes.Doctor>(gameState) < 1 && gameState.Self.AvailableCoins >= 5),
                           CardAcceptance.For<CardTypes.Province>(),
                           CardAcceptance.For<CardTypes.Duchy>(gameState => CountOfPile<CardTypes.Province>(gameState) <= 5),
                           CardAcceptance.For<CardTypes.Estate>(gameState => CountOfPile<CardTypes.Province>(gameState) <= 2),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.TreasureMap>(gameState => CountAllOwned<CardTypes.Gold>(gameState) == 0),
                           //CardAcceptance.For<CardTypes.Doctor>(gameState => CountAllOwned<CardTypes.Doctor>(gameState) == 0),
                           CardAcceptance.For<CardTypes.Estate>(gameState => CountOfPile<CardTypes.Province>(gameState) < 4),
                           CardAcceptance.For<CardTypes.Silver>());                           
            }

            private static CardPickByPriority ActionOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.TreasureMap>(gameState => CountInHand<CardTypes.TreasureMap>(gameState) == 2 || CountAllOwned<CardTypes.Gold>(gameState) > 0),
                           CardAcceptance.For<CardTypes.Doctor>());
            }

            private static CardPickByPriority TrashAndDiscardOrder()
            {
                return new CardPickByPriority(
                           CardAcceptance.For<CardTypes.Estate>(),
                           CardAcceptance.For<CardTypes.Copper>(),
                           CardAcceptance.For<CardTypes.Silver>(),
                           CardAcceptance.For<CardTypes.Gold>(),
                           CardAcceptance.For<CardTypes.Province>(),
                           CardAcceptance.For<CardTypes.Duchy>(),
                           CardAcceptance.For<CardTypes.Estate>(),
                           CardAcceptance.For<CardTypes.TreasureMap>());
            }
        }
    }
}
