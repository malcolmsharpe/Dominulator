﻿using Dominion;
using CardTypes = Dominion.CardTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Author: Sparafucile + SheCantSayNo 

namespace Program
{
    public static partial class Strategies
    {
        public static class HermitMarketSquare
        {
            public static PlayerAction Player(int playerNumber)
            {
                return new MyPlayerAction(playerNumber);
            }

            class MyPlayerAction
                : PlayerAction
            {
                public MyPlayerAction(int playerNumber)
                    : base("HermitMarketSquare",
                        playerNumber,
                        purchaseOrder: PurchaseOrder(),
                        actionOrder: ActionOrder(TrashOrder()),
                        trashOrder: TrashOrder())
                {
                }

                private static CardPickByPriority PurchaseOrder()
                {
                    return new CardPickByPriority(
                               CardAcceptance.For<CardTypes.Province>(),
                               CardAcceptance.For<CardTypes.Duchy>(gameState => CountAllOwned<CardTypes.Province>(gameState) > 0),
                               CardAcceptance.For<CardTypes.Estate>(gameState => CountAllOwned<CardTypes.Province>(gameState) > 0),
                               CardAcceptance.For<CardTypes.Hermit>(ShouldGainHermit),
                               CardAcceptance.For<CardTypes.MarketSquare>(ShouldGainMarketSquare));
                }

                private static CardPickByPriority ActionOrder(ICardPicker trashOrder)
                {
                    return new CardPickByPriority(
                               CardAcceptance.For<CardTypes.Madman>(ShouldPlayMadman(trashOrder)),
                               CardAcceptance.For<CardTypes.Hermit>(IsDoingMegaTurn),
                               CardAcceptance.For<CardTypes.MarketSquare>(ShouldPlayMarketSquare(trashOrder)),
                               CardAcceptance.For<CardTypes.Hermit>(gameState => CountAllOwned<CardTypes.Madman>(gameState) - CountAllOwned<CardTypes.Hermit>(gameState) < 3),
                               CardAcceptance.For<CardTypes.Hermit>(gameState => CountAllOwned<CardTypes.Province>(gameState) > 0));
                }

                private static CardPickByPriority TrashOrder()
                {
                    return new CardPickByPriority(
                               CardAcceptance.For<CardTypes.Estate>(gameState => IsDoingMegaTurn(gameState) && CountAllOwned<CardTypes.Province>(gameState) == 0),
                               CardAcceptance.For<CardTypes.Hermit>(IsDoingMegaTurn));
                }
            }

            private static bool ShouldGainHermit(GameState gameState)
            {
                if (PlayBigHermit(gameState))
                    return CountHermitsEverGained(gameState) < 9 && CountAllOwned<CardTypes.MarketSquare>(gameState) == 0;

                return CountHermitsEverGained(gameState) < 7 && CountAllOwned<CardTypes.MarketSquare>(gameState) == 0;
            }

            private static bool ShouldGainMarketSquare(GameState gameState)
            {
                PlayerState self = gameState.Self;

                //Prioritize gaining Madmen over buying Market Squares once you have three squares
                if (CountAllOwned<CardTypes.Hermit>(gameState) > CountInDeckAndDiscard<CardTypes.Hermit>(gameState) + CountInHand<CardTypes.Hermit>(gameState) &&
                    self.Hand.Count < 4 &&
                    CountAllOwned<CardTypes.MarketSquare>(gameState) > 2)
                {
                    return false;
                }

                return true;
            }

            private static bool PlayBigHermit(GameState gameState)
            {
                //Play the 9-Hermit version if possible
                return CountOfPile<CardTypes.Hermit>(gameState) + CountHermitsEverGained(gameState) >= 9;
            }

            private static GameStatePredicate ShouldPlayMadman(ICardPicker trashOrder)
            {
                return delegate(GameState gameState)
                {
                    PlayerState self = gameState.Self;

                    if (!self.Hand.HasCard(CardTypes.Madman.card))
                        return false;

                    if (CountAllOwned<CardTypes.Province>(gameState) > 0)
                        return true;

                    if (ShouldStartMegaTurn(gameState))
                        return true;

                    if (IsDoingMegaTurn(gameState))
                    {
                        if (self.CardsInDeckAndDiscard.Count() > 5)
                            return true;
                    }

                    return false;
                };
            }

            private static GameStatePredicate ShouldPlayMarketSquare(ICardPicker trashOrder)
            {
                return delegate(GameState gameState)
                {
                    var self = gameState.Self;

                    if (!IsDoingMegaTurn(gameState))
                    {
                        return !CanTrashForGold(gameState, trashOrder);
                    }
                    else
                    {

                        if (ShouldPlayMadman(trashOrder)(gameState))
                            return false;

                        int numberOfProvincesCanAfford = self.ExpectedCoinValueAtEndOfTurn / CardTypes.Province.card.CurrentCoinCost(self);
                        if (self.AvailableBuys < numberOfProvincesCanAfford)
                            return true;

                        return !CanTrashForGold(gameState, trashOrder);
                    }
                };
            }

            private static bool CanTrashForGold(GameState gameState, ICardPicker trashOrder)
            {
                PlayerState self = gameState.Self;

                return trashOrder.GetPreferredCard(gameState, c => (self.Hand.HasCard(c) || self.Discard.HasCard(c)) && CardTypes.Hermit.CanTrashCard(c)) != null &&
                       self.Hand.HasCard(CardTypes.Hermit.card) &&
                       self.Hand.HasCard(CardTypes.MarketSquare.card);
            }

            private static bool ShouldStartMegaTurn(GameState gameState)
            {
                PlayerState self = gameState.Self;

                int CountMSNotInPlay = CountInDeckAndDiscard<CardTypes.MarketSquare>(gameState) +
                  CountInHand<CardTypes.MarketSquare>(gameState);

                if (self.Hand.Count < 5 ||
                    self.Hand.CountOf(CardTypes.Madman.card) < 2 ||
                    CountMSNotInPlay < 3)
                    return false;

                if (PlayBigHermit(gameState))
                {
                    return self.AllOwnedCards.CountOf(CardTypes.Madman.card) >= 6 &&                           
                           CountHermitsEverGained(gameState) >= 9;
                }
                else
                {
                    return self.AllOwnedCards.CountOf(CardTypes.Madman.card) >= 4 &&
                           CountHermitsEverGained(gameState) >= 7;
                }
            }

            private static bool IsDoingMegaTurn(GameState gameState)
            {
                PlayerState self = gameState.Self;

                return self.Hand.Count > 6;
            }

            private static int CountHermitsEverGained(GameState gameState)
            {
                PlayerState self = gameState.Self;
                return CountAllOwned<CardTypes.Hermit>(gameState) + CountAllOwned<CardTypes.Madman>(gameState);
            }
        }
    }
}